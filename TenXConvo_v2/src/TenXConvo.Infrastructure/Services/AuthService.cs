using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TenXConvo.Application.Interfaces;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

namespace TenXConvo.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext  _db;
    private readonly IConfiguration _config;
    private readonly TokenService  _tokens;

    public AuthService(AppDbContext db, IConfiguration config, TokenService tokens)
    {
        _db     = db;
        _config = config;
        _tokens = tokens;
    }

    // ── STEP 1: email → locations + connections + fiscal years ───────────────
    public async Task<LoginStep1Result> LoginStep1Async(string email)
    {
        var locations = await _db.Locations
            .Include(l => l.LocationType)
            .Where(l => l.IsActive)
            .OrderBy(l => l.LocationName)
            .Select(l => new LocationItem(l.Id, l.LocationName, l.LocationType.LocationTypeName))
            .ToListAsync();

        var fiscalYears = await _db.FiscalYears
            .Where(f => f.IsActive)
            .OrderByDescending(f => f.IsCurrent)
            .Select(f => new FiscalYearItem(f.Id, f.Name, f.IsCurrent))
            .ToListAsync();

        var emailParts = email.Split('@');
        var defaultUserName = emailParts.Length > 0 ? emailParts[0] : email;

        return new LoginStep1Result(
            Found:       true,
            UserName:    defaultUserName,
            LoginId:     email,
            Locations:   locations,
            Connections: new List<string> { "QA", "Production" },
            FiscalYears: fiscalYears
        );
    }

    // ── STEP 2: password + selections → JWT ─────────────────────────────────
    public async Task<LoginStep2Result> LoginStep2Async(LoginStep2Input input)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == input.Email.ToLower() && u.IsActive);

        bool isValidPassword = false;
        if (user != null)
        {
            isValidPassword = BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash);
        }
        else
        {
            // Dummy hash verify to mitigate timing attacks
            BCrypt.Net.BCrypt.Verify(input.Password, "$2a$11$0oYQQWFJqWl7qXazxwgc7OWNo5qWqkpKg0WZ2WQeaPWqjlr/.u9fm");
        }

        if (user == null || !isValidPassword)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var location   = await _db.Locations.Include(l => l.LocationType).FirstOrDefaultAsync(l => l.Id == input.LocationId)
                         ?? throw new InvalidOperationException("Location not found.");
        var fiscalYear = await _db.FiscalYears.FindAsync(input.FiscalYearId)
                         ?? throw new InvalidOperationException("Fiscal year not found.");

        // Generate JWT access token with all claims
        var accessToken  = _tokens.GenerateAccessToken(user, user.Role, location, input.Connection, fiscalYear);
        var refreshToken = TokenService.GenerateRefreshToken();

        // Revoke old refresh tokens for this user (single device policy)
        var oldTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync();
        oldTokens.ForEach(t => t.IsRevoked = true);

        // Save new refresh token
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = _tokens.GetRefreshTokenExpiry()
        });

        // Save login preference (pre-fills dropdowns next login)
        var pref = await _db.LoginPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (pref == null)
            _db.LoginPreferences.Add(new UserLoginPreference { UserId = user.Id, LocationId = input.LocationId, FiscalYearId = input.FiscalYearId, Connection = input.Connection, RememberMe = input.RememberMe });
        else
        { pref.LocationId = input.LocationId; pref.FiscalYearId = input.FiscalYearId; pref.Connection = input.Connection; pref.RememberMe = input.RememberMe; pref.UpdatedAt = DateTime.UtcNow; }

        await _db.SaveChangesAsync();

        return new LoginStep2Result(
            AccessToken:  accessToken,
            RefreshToken: refreshToken,
            ExpiresAt:    _tokens.GetAccessTokenExpiry(),
            User: new UserProfileResult(user.Id, user.UserName, user.LoginId, user.Email, user.CellNo, user.ImageUrl, user.Role?.RoleName ?? "")
        );
    }

    // ── REFRESH TOKEN ────────────────────────────────────────────────────────
    public async Task<TokenResult> RefreshTokenAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired. Please login again.");

        var pref       = await _db.LoginPreferences.FirstOrDefaultAsync(p => p.UserId == token.UserId);
        var location   = pref?.LocationId   != null ? await _db.Locations.Include(l => l.LocationType).FirstOrDefaultAsync(l => l.Id == pref.LocationId) : null;
        var fiscalYear = pref?.FiscalYearId != null ? await _db.FiscalYears.FindAsync(pref.FiscalYearId) : null;

        token.IsRevoked = true;

        var newAccess  = _tokens.GenerateAccessToken(token.User, token.User.Role, location, pref?.Connection ?? "Production", fiscalYear);
        var newRefresh = TokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken { UserId = token.UserId, Token = newRefresh, ExpiresAt = _tokens.GetRefreshTokenExpiry() });
        await _db.SaveChangesAsync();

        return new TokenResult(newAccess, newRefresh, _tokens.GetAccessTokenExpiry());
    }

    // ── LOGOUT ───────────────────────────────────────────────────────────────
    public async Task LogoutAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token != null) { token.IsRevoked = true; await _db.SaveChangesAsync(); }
    }

    // ── GET ME ───────────────────────────────────────────────────────────────
    public async Task<UserProfileResult> GetMeAsync(Guid userId)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");
        return new UserProfileResult(user.Id, user.UserName, user.LoginId, user.Email, user.CellNo, user.ImageUrl, user.Role?.RoleName ?? "");
    }

    // ── CHANGE PASSWORD ──────────────────────────────────────────────────────
    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId) ?? throw new KeyNotFoundException("User not found.");
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt    = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  OAUTH / EXTERNAL LOGIN
    // ══════════════════════════════════════════════════════════════════════════

    // ── External Login (Google / Microsoft) → JWT ────────────────────────────
    // Frontend sends the OAuth id_token's claims (provider, providerKey, email).
    // We find-or-create the AppUser + ExternalLogin, then issue our JWT.
    public async Task<LoginStep2Result> ExternalLoginAsync(ExternalLoginInput input)
    {
        // 1. Look for existing external login link
        var extLogin = await _db.Set<ExternalLogin>()
            .Include(e => e.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(e => e.Provider == input.Provider && e.ProviderKey == input.ProviderKey);

        AppUser user;

        if (extLogin != null)
        {
            // Known external login → use linked user
            user = extLogin.User;
            if (!user.IsActive) throw new UnauthorizedAccessException("Account is disabled.");
        }
        else
        {
            // New external login — try to match by email first
            user = await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == input.Email.ToLower() && u.IsActive)!;

            if (user == null)
            {
                // Auto-provision new user with default "Web Role"
                var webRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Web Role");
                user = new AppUser
                {
                    UserName     = input.DisplayName ?? input.Email.Split('@')[0],
                    LoginId      = input.Email,
                    Email        = input.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // random (unusable) password
                    ImageUrl     = input.AvatarUrl,
                    RoleId       = webRole?.Id,
                    Role         = webRole,
                };
                _db.Users.Add(user);
            }

            // Link the external login to this user
            _db.Set<ExternalLogin>().Add(new ExternalLogin
            {
                UserId      = user.Id,
                Provider    = input.Provider,
                ProviderKey = input.ProviderKey,
                Email       = input.Email,
                DisplayName = input.DisplayName,
                AvatarUrl   = input.AvatarUrl,
            });
        }

        // 2. Resolve location + fiscal year (use defaults if not supplied)
        var location = input.LocationId.HasValue
            ? await _db.Locations.Include(l => l.LocationType).FirstOrDefaultAsync(l => l.Id == input.LocationId)
            : await _db.Locations.Include(l => l.LocationType).FirstOrDefaultAsync(l => l.IsActive);
        var fiscalYear = input.FiscalYearId.HasValue
            ? await _db.FiscalYears.FindAsync(input.FiscalYearId)
            : await _db.FiscalYears.FirstOrDefaultAsync(f => f.IsCurrent && f.IsActive);
        var connection = input.Connection ?? "Production";

        // 3. Issue tokens
        var accessToken  = _tokens.GenerateAccessToken(user, user.Role, location, connection, fiscalYear);
        var refreshToken = TokenService.GenerateRefreshToken();

        // Revoke old refresh tokens
        var oldTokens = await _db.RefreshTokens.Where(t => t.UserId == user.Id && !t.IsRevoked).ToListAsync();
        oldTokens.ForEach(t => t.IsRevoked = true);

        _db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, Token = refreshToken, ExpiresAt = _tokens.GetRefreshTokenExpiry() });
        await _db.SaveChangesAsync();

        return new LoginStep2Result(
            AccessToken:  accessToken,
            RefreshToken: refreshToken,
            ExpiresAt:    _tokens.GetAccessTokenExpiry(),
            User: new UserProfileResult(user.Id, user.UserName, user.LoginId, user.Email, user.CellNo, user.ImageUrl, user.Role?.RoleName ?? "")
        );
    }

    // ── List linked providers ────────────────────────────────────────────────
    public async Task<List<ExternalLoginInfo>> GetLinkedProvidersAsync(Guid userId)
    {
        return await _db.Set<ExternalLogin>()
            .Where(e => e.UserId == userId)
            .Select(e => new ExternalLoginInfo(e.Provider, e.Email ?? "", e.LinkedAt))
            .ToListAsync();
    }

    // ── Link a new external provider to existing account ─────────────────────
    public async Task LinkExternalLoginAsync(Guid userId, ExternalLoginInput input)
    {
        var exists = await _db.Set<ExternalLogin>()
            .AnyAsync(e => e.Provider == input.Provider && e.ProviderKey == input.ProviderKey);
        if (exists) throw new InvalidOperationException($"This {input.Provider} account is already linked.");

        _db.Set<ExternalLogin>().Add(new ExternalLogin
        {
            UserId      = userId,
            Provider    = input.Provider,
            ProviderKey = input.ProviderKey,
            Email       = input.Email,
            DisplayName = input.DisplayName,
            AvatarUrl   = input.AvatarUrl,
        });
        await _db.SaveChangesAsync();
    }

    // ── Unlink an external provider ──────────────────────────────────────────
    public async Task UnlinkExternalLoginAsync(Guid userId, string provider)
    {
        var link = await _db.Set<ExternalLogin>()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider)
            ?? throw new KeyNotFoundException($"No {provider} account linked.");
        _db.Set<ExternalLogin>().Remove(link);
        await _db.SaveChangesAsync();
    }
}
