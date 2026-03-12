using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using TenXConvo.Application.Interfaces;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

namespace TenXConvo.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly string _uploadsPath;
    private readonly string _baseUrl;

    public AdminService(AppDbContext db, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _db = db;
        _uploadsPath = config["FileStorage:LocalPath"] ?? "wwwroot/uploads";
        _baseUrl = config["FileStorage:BaseUrl"] ?? "http://localhost:5000/uploads";
    }

    // ── SETTINGS ─────────────────────────────────────────────────────────────

    public async Task<ClientSettingsResult> GetSettingsAsync()
    {
        var s = await _db.ClientSettings
            .Include(x => x.ConsultantDefaultRole)
            .Include(x => x.ClientDefaultRole)
            .FirstOrDefaultAsync();

        if (s == null) return new ClientSettingsResult(false, null, null, null, null, null, null, null, null, null, null, null);

        return new ClientSettingsResult(
            s.IsWebsiteOnline, s.FooterDescription,
            s.BusinessName, s.BusinessNature, s.BusinessProvince, s.FbrToken, s.ValidationToken,
            s.ConsultantDefaultRoleId, s.ConsultantDefaultRole?.RoleName,
            s.ClientDefaultRoleId, s.ClientDefaultRole?.RoleName,
            s.ChatLinkUrl
        );
    }

    public async Task UpdateWebsiteSettingsAsync(bool isOnline, string? footer)
    {
        var s = await GetOrCreateSettingsAsync();
        s.IsWebsiteOnline = isOnline;
        s.FooterDescription = footer;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateBusinessSettingsAsync(BusinessSettingsInput input)
    {
        var s = await GetOrCreateSettingsAsync();
        s.BusinessName = input.BusinessName;
        s.BusinessNature = input.BusinessNature;
        s.BusinessProvince = input.BusinessProvince;
        s.FbrToken = input.FbrToken;
        s.ValidationToken = input.ValidationToken;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateRolesSettingsAsync(Guid? consultantRoleId, Guid? clientRoleId, string? chatUrl)
    {
        var s = await GetOrCreateSettingsAsync();
        s.ConsultantDefaultRoleId = consultantRoleId;
        s.ClientDefaultRoleId = clientRoleId;
        s.ChatLinkUrl = chatUrl;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task<ClientSettings> GetOrCreateSettingsAsync()
    {
        var s = await _db.ClientSettings.FirstOrDefaultAsync();
        if (s != null) return s;
        s = new ClientSettings();
        _db.ClientSettings.Add(s);
        return s;
    }

    // ── ORGANIZATION ──────────────────────────────────────────────────────────

    public async Task<OrganizationResult> GetOrganizationAsync()
    {
        var o = await _db.Organizations.FirstOrDefaultAsync()
            ?? new Organization { ClientName = "" };

        return MapOrg(o);
    }

    public async Task UpdateOrganizationAsync(OrganizationInput input)
    {
        var o = await _db.Organizations.FirstOrDefaultAsync();
        if (o == null) { o = new Organization(); _db.Organizations.Add(o); }

        o.ClientName = input.ClientName;
        o.ClientArea = input.ClientArea;
        o.ClientGroup = input.ClientGroup;
        o.Currency = input.Currency;
        o.CurrencySymbol = input.CurrencySymbol;
        o.Email = input.Email;
        o.ContactPerson = input.ContactPerson;
        o.CellNo = input.CellNo;
        o.Website = input.Website;
        o.NTN = input.NTN;
        o.STRN = input.STRN;
        o.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<string> UploadLogoAsync(Stream fileStream, string fileName)
    {
        var url = await SaveFileAsync(fileStream, fileName, "logos");
        var o = await _db.Organizations.FirstOrDefaultAsync();
        if (o != null) { o.LogoUrl = url; await _db.SaveChangesAsync(); }
        return url;
    }

    private static OrganizationResult MapOrg(Organization o) =>
        new(o.Id, o.ClientName, o.ClientArea, o.ClientGroup, o.Currency, o.CurrencySymbol,
            o.Email, o.ContactPerson, o.CellNo, o.Website, o.NTN, o.STRN, o.LogoUrl);

    // ── LOCATIONS ─────────────────────────────────────────────────────────────

    public async Task<PagedResult<LocationResult>> GetLocationsAsync(int page, int pageSize, string? search)
    {
        var q = _db.Locations.Include(l => l.LocationType).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(l => l.LocationName.Contains(search) || l.LocationAddress!.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderBy(l => l.LocationName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new LocationResult(l.Id, l.LocationName, l.LocationType.LocationTypeName, l.LocationAddress, l.IsActive, l.CreatedOn))
            .ToListAsync();

        return new PagedResult<LocationResult>(items, total, page, pageSize);
    }

    public async Task<LocationResult> CreateLocationAsync(LocationInput input)
    {
        var locType = await _db.LocationTypes.FindAsync(input.LocationTypeId)
            ?? throw new KeyNotFoundException("Location type not found.");

        var l = new Location
        {
            LocationName = input.LocationName,
            LocationTypeId = input.LocationTypeId,
            LocationAddress = input.LocationAddress,
            IsActive = input.IsActive
        };
        _db.Locations.Add(l);
        await _db.SaveChangesAsync();
        return new LocationResult(l.Id, l.LocationName, locType.LocationTypeName, l.LocationAddress, l.IsActive, l.CreatedOn);
    }

    public async Task<LocationResult> UpdateLocationAsync(Guid id, LocationInput input)
    {
        var l = await _db.Locations.FindAsync(id) ?? throw new KeyNotFoundException("Location not found.");
        var locType = await _db.LocationTypes.FindAsync(input.LocationTypeId) ?? throw new KeyNotFoundException("Location type not found.");
        l.LocationName = input.LocationName;
        l.LocationTypeId = input.LocationTypeId;
        l.LocationAddress = input.LocationAddress;
        l.IsActive = input.IsActive;
        l.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new LocationResult(l.Id, l.LocationName, locType.LocationTypeName, l.LocationAddress, l.IsActive, l.CreatedOn);
    }

    public async Task DeleteLocationAsync(Guid id)
    {
        var l = await _db.Locations.FindAsync(id) ?? throw new KeyNotFoundException("Location not found.");
        _db.Locations.Remove(l);
        await _db.SaveChangesAsync();
    }

    // ── ROLES ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<RoleResult>> GetRolesAsync(int page, int pageSize, string? search)
    {
        var q = _db.Roles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.RoleName.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderBy(r => r.RoleName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new RoleResult(r.Id, r.RoleName, r.CreatedOn,
                _db.RoleModules.Count(rm => rm.RoleId == r.Id)))
            .ToListAsync();

        return new PagedResult<RoleResult>(items, total, page, pageSize);
    }

    public async Task<RoleResult> CreateRoleAsync(string roleName)
    {
        var r = new AppRole { RoleName = roleName };
        _db.Roles.Add(r);
        await _db.SaveChangesAsync();
        return new RoleResult(r.Id, r.RoleName, r.CreatedOn, 0);
    }

    public async Task<RoleResult> UpdateRoleAsync(Guid id, string roleName)
    {
        var r = await _db.Roles.FindAsync(id) ?? throw new KeyNotFoundException("Role not found.");
        r.RoleName = roleName;
        await _db.SaveChangesAsync();
        var totalLocations = await _db.RoleModules.CountAsync(rm => rm.RoleId == id);
        return new RoleResult(r.Id, r.RoleName, r.CreatedOn, totalLocations);
    }

    public async Task DeleteRoleAsync(Guid id)
    {
        var r = await _db.Roles.FindAsync(id) ?? throw new KeyNotFoundException("Role not found.");
        _db.Roles.Remove(r);
        await _db.SaveChangesAsync();
    }

    // ── USERS ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<UserResult>> GetUsersAsync(int page, int pageSize, string? search)
    {
        var q = _db.Users.Include(u => u.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(u => u.UserName.Contains(search) || u.Email.Contains(search) || u.LoginId.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserResult(u.Id, u.ImageUrl, u.UserName, u.LoginId, u.Email, u.CellNo, u.Role!.RoleName, u.IsActive, u.CreatedAt))
            .ToListAsync();

        return new PagedResult<UserResult>(items, total, page, pageSize);
    }

    public async Task<UserResult> GetUserByIdAsync(Guid id)
    {
        var u = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException("User not found.");
        return new UserResult(u.Id, u.ImageUrl, u.UserName, u.LoginId, u.Email, u.CellNo, u.Role?.RoleName, u.IsActive, u.CreatedAt);
    }

    public async Task<UserResult> CreateUserAsync(CreateUserInput input)
    {
        if (await _db.Users.AnyAsync(u => u.Email == input.Email))
            throw new InvalidOperationException("Email already exists.");
        if (await _db.Users.AnyAsync(u => u.LoginId == input.LoginId))
            throw new InvalidOperationException("Login ID already exists.");

        var u = new AppUser
        {
            UserName = input.UserName,
            LoginId = input.LoginId,
            Email = input.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
            CellNo = input.CellNo,
            RoleId = input.RoleId
        };
        _db.Users.Add(u);
        await _db.SaveChangesAsync();

        var role = input.RoleId.HasValue ? await _db.Roles.FindAsync(input.RoleId) : null;
        return new UserResult(u.Id, null, u.UserName, u.LoginId, u.Email, u.CellNo, role?.RoleName, u.IsActive, u.CreatedAt);
    }

    public async Task<UserResult> UpdateUserAsync(Guid id, UpdateUserInput input)
    {
        var u = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException("User not found.");
        u.UserName = input.UserName;
        u.Email = input.Email;
        u.CellNo = input.CellNo;
        u.RoleId = input.RoleId;
        u.IsActive = input.IsActive;
        u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        var role = input.RoleId.HasValue ? await _db.Roles.FindAsync(input.RoleId) : null;
        return new UserResult(u.Id, u.ImageUrl, u.UserName, u.LoginId, u.Email, u.CellNo, role?.RoleName, u.IsActive, u.CreatedAt);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var u = await _db.Users.FindAsync(id) ?? throw new KeyNotFoundException("User not found.");
        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword)
    {
        var u = await _db.Users.FindAsync(id) ?? throw new KeyNotFoundException("User not found.");
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<string> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName)
    {
        var url = await SaveFileAsync(fileStream, fileName, "avatars");
        var u = await _db.Users.FindAsync(userId);
        if (u != null) { u.ImageUrl = url; u.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
        return url;
    }

    // ── ERROR LOGS ────────────────────────────────────────────────────────────

    public async Task<PagedResult<ErrorLogResult>> GetErrorLogsAsync(int page, int pageSize, string? search)
    {
        var q = _db.ErrorLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e => e.ErrorMessage.Contains(search) || e.ActionName.Contains(search) || e.ControllerName.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(e => e.CreatedOn)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new ErrorLogResult(e.Id, e.ActionName, e.ControllerName, e.Code, e.ErrorMessage, e.StackTrace, e.RequestPath, e.CreatedOn))
            .ToListAsync();

        return new PagedResult<ErrorLogResult>(items, total, page, pageSize);
    }

    public async Task<ErrorLogResult> GetErrorLogByIdAsync(Guid id)
    {
        var e = await _db.ErrorLogs.FindAsync(id) ?? throw new KeyNotFoundException("Error log not found.");
        return new ErrorLogResult(e.Id, e.ActionName, e.ControllerName, e.Code, e.ErrorMessage, e.StackTrace, e.RequestPath, e.CreatedOn);
    }

    public async Task DeleteErrorLogAsync(Guid id)
    {
        var e = await _db.ErrorLogs.FindAsync(id) ?? throw new KeyNotFoundException("Error log not found.");
        _db.ErrorLogs.Remove(e);
        await _db.SaveChangesAsync();
    }

    // ── FILE UPLOAD HELPER ────────────────────────────────────────────────────

    private async Task<string> SaveFileAsync(Stream fileStream, string fileName, string subfolder)
    {
        var ext = Path.GetExtension(fileName);
        var newName = $"{Guid.NewGuid()}{ext}";
        var folder = Path.Combine(_uploadsPath, subfolder);
        Directory.CreateDirectory(folder);
        var fullPath = Path.Combine(folder, newName);

        using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs);

        return $"{_baseUrl}/{subfolder}/{newName}";
    }
}
