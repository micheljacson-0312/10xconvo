using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TenXConvo.Domain.Entities;

namespace TenXConvo.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════
//  JWT TOKEN SERVICE
//  Generates Access Token + Refresh Token
//
//  Access Token Claims:
//    sub          = userId (Guid)
//    email        = user email
//    name         = user display name
//    role         = "Admin Role" | "Consultant Role" | "Client Role" | "Web Role"
//    loginId      = name@htag.mhm
//    location     = locationId (selected at login step 2)
//    locationName = "Head Office" | "Multan Office"
//    connection   = "QA" | "Production"
//    fiscalYear   = fiscalYearId
//    fiscalYearName = "Financial Year 2026-2027"
//
//  Token lifetime  : 60 minutes (configurable in appsettings.json)
//  Refresh lifetime: 30 days    (configurable in appsettings.json)
// ═══════════════════════════════════════════════════════════════════════════
public class TokenService
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration config)
    {
        _config = config;
        _key    = new SymmetricSecurityKey(
                      Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
    }

    // ── Generate JWT Access Token ────────────────────────────────────────────
    public string GenerateAccessToken(
        AppUser    user,
        AppRole?   role,
        Location?  location,
        string     connection,
        FiscalYear? fiscalYear)
    {
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");
        var expiry        = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
        // Standard + compatibility claims
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),

        new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
        new(ClaimTypes.Email, user.Email ?? ""),

    new(JwtRegisteredClaimNames.Name, user.UserName ?? ""),
    new(ClaimTypes.Name, user.UserName ?? ""),

    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

    // optional but good for compatibility/debugging
    new("userid", user.Id.ToString()),

    // Role claim
    new(ClaimTypes.Role, role?.RoleName ?? ""),

    // App-specific claims
    new("loginId",       user.LoginId ?? ""),
    new("roleName",      role?.RoleName      ?? ""),
    new("roleId",        role?.Id.ToString() ?? ""),

    new("location",      location?.Id.ToString()   ?? ""),
    new("locationName",  location?.LocationName    ?? ""),

    new("connection",    connection ?? ""),

    new("fiscalYear",     fiscalYear?.Id.ToString() ?? ""),
    new("fiscalYearName", fiscalYear?.Name          ?? ""),
};
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Generate Refresh Token (cryptographically secure random) ────────────
    public static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    // ── Get expiry datetime for access token ─────────────────────────────────
    public DateTime GetAccessTokenExpiry()
    {
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");
        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }

    // ── Get expiry datetime for refresh token ────────────────────────────────
    public DateTime GetRefreshTokenExpiry()
    {
        var expiryDays = int.Parse(_config["Jwt:RefreshExpiryDays"] ?? "30");
        return DateTime.UtcNow.AddDays(expiryDays);
    }

    // ── Validate a token (for refresh) ──────────────────────────────────────
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler    = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = false,   // allow expired for refresh
                ValidateIssuerSigningKey = true,
                ValidIssuer              = _config["Jwt:Issuer"],
                ValidAudience            = _config["Jwt:Audience"],
                IssuerSigningKey         = _key
            };
            return handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }
}
