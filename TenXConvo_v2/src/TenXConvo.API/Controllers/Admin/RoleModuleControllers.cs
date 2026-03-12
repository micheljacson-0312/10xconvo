using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

namespace TenXConvo.API.Controllers.Admin;

// ═══════════════════════════════════════════════════════════════════════════
//  ROLE MODULES — which modules a role has access to, per location
//  GET  /api/admin/users/roles/{roleId}/modules
//  POST /api/admin/users/roles/{roleId}/modules
//  DEL  /api/admin/users/roles/{roleId}/modules/{id}
// ═══════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/admin/users/roles/{roleId:guid}/modules")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class RoleModuleController : ControllerBase
{
    private readonly AppDbContext _db;
    public RoleModuleController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid roleId)
    {
        var role = await _db.Roles.FindAsync(roleId);
        if (role == null) return NotFound(new { success = false, message = "Role not found." });

        var modules = await _db.RoleModules
            .Include(m => m.Location)
            .Where(m => m.RoleId == roleId)
            .OrderBy(m => m.ModuleKey)
            .Select(m => new
            {
                m.Id, m.ModuleKey, m.ModuleName,
                m.CanView, m.CanCreate, m.CanEdit, m.CanDelete, m.CanExport,
                LocationName = m.Location != null ? m.Location.LocationName : "All Locations",
                m.LocationId
            })
            .ToListAsync();

        return Ok(new { success = true, data = modules });
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(Guid roleId, [FromBody] UpsertRoleModuleRequest req)
    {
        var role = await _db.Roles.FindAsync(roleId);
        if (role == null) return NotFound(new { success = false, message = "Role not found." });

        // Upsert — update if exists, insert if not
        var existing = await _db.RoleModules
            .FirstOrDefaultAsync(m => m.RoleId == roleId && m.ModuleKey == req.ModuleKey
                                   && m.LocationId == req.LocationId);

        if (existing != null)
        {
            existing.ModuleName = req.ModuleName;
            existing.CanView    = req.CanView;
            existing.CanCreate  = req.CanCreate;
            existing.CanEdit    = req.CanEdit;
            existing.CanDelete  = req.CanDelete;
            existing.CanExport  = req.CanExport;
        }
        else
        {
            _db.RoleModules.Add(new RoleModule
            {
                Id         = Guid.NewGuid(),
                RoleId     = roleId,
                LocationId = req.LocationId,
                ModuleKey  = req.ModuleKey,
                ModuleName = req.ModuleName,
                CanView    = req.CanView,
                CanCreate  = req.CanCreate,
                CanEdit    = req.CanEdit,
                CanDelete  = req.CanDelete,
                CanExport  = req.CanExport,
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Module permissions saved." });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpsert(Guid roleId, [FromBody] List<UpsertRoleModuleRequest> requests)
    {
        var role = await _db.Roles.FindAsync(roleId);
        if (role == null) return NotFound(new { success = false, message = "Role not found." });

        foreach (var req in requests)
        {
            var existing = await _db.RoleModules
                .FirstOrDefaultAsync(m => m.RoleId == roleId && m.ModuleKey == req.ModuleKey
                                       && m.LocationId == req.LocationId);
            if (existing != null)
            {
                existing.CanView = req.CanView; existing.CanCreate = req.CanCreate;
                existing.CanEdit = req.CanEdit; existing.CanDelete = req.CanDelete;
                existing.CanExport = req.CanExport;
            }
            else
            {
                _db.RoleModules.Add(new RoleModule
                {
                    Id = Guid.NewGuid(), RoleId = roleId, LocationId = req.LocationId,
                    ModuleKey = req.ModuleKey, ModuleName = req.ModuleName,
                    CanView = req.CanView, CanCreate = req.CanCreate, CanEdit = req.CanEdit,
                    CanDelete = req.CanDelete, CanExport = req.CanExport,
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"{requests.Count} module(s) saved." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid roleId, Guid id)
    {
        var item = await _db.RoleModules.FirstOrDefaultAsync(m => m.Id == id && m.RoleId == roleId);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.RoleModules.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}

public record UpsertRoleModuleRequest(
    string  ModuleKey,
    string  ModuleName,
    Guid?   LocationId,
    bool    CanView,
    bool    CanCreate,
    bool    CanEdit,
    bool    CanDelete,
    bool    CanExport
);

// ═══════════════════════════════════════════════════════════════════════════
//  ROLE MENU ENTRIES — sidebar menu items visible to a role
//  GET  /api/admin/users/roles/{roleId}/menus
//  POST /api/admin/users/roles/{roleId}/menus/bulk
//  DEL  /api/admin/users/roles/{roleId}/menus/{id}
// ═══════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/admin/users/roles/{roleId:guid}/menus")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class RoleMenuController : ControllerBase
{
    private readonly AppDbContext _db;
    public RoleMenuController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid roleId)
    {
        var menus = await _db.RoleMenus
            .Include(m => m.Location)
            .Where(m => m.RoleId == roleId)
            .OrderBy(m => m.MenuOrder).ThenBy(m => m.MenuKey)
            .Select(m => new
            {
                m.Id, m.MenuKey, m.MenuLabel, m.MenuIcon, m.MenuPath,
                m.MenuOrder, m.ParentKey, m.IsVisible,
                LocationName = m.Location != null ? m.Location.LocationName : "All",
                m.LocationId
            })
            .ToListAsync();

        return Ok(new { success = true, data = menus });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkSave(Guid roleId, [FromBody] List<RoleMenuRequest> requests)
    {
        var role = await _db.Roles.FindAsync(roleId);
        if (role == null) return NotFound(new { success = false, message = "Role not found." });

        // Replace all menus for this role (delete + re-insert per location)
        var locationIds = requests.Select(r => r.LocationId).Distinct();
        foreach (var locId in locationIds)
        {
            var existing = _db.RoleMenus.Where(m => m.RoleId == roleId && m.LocationId == locId);
            _db.RoleMenus.RemoveRange(existing);
        }

        foreach (var req in requests)
        {
            _db.RoleMenus.Add(new RoleMenuEntry
            {
                Id         = Guid.NewGuid(),
                RoleId     = roleId,
                LocationId = req.LocationId,
                MenuKey    = req.MenuKey,
                MenuLabel  = req.MenuLabel,
                MenuIcon   = req.MenuIcon,
                MenuPath   = req.MenuPath,
                MenuOrder  = req.MenuOrder,
                ParentKey  = req.ParentKey,
                IsVisible  = req.IsVisible,
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Menu saved." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid roleId, Guid id)
    {
        var item = await _db.RoleMenus.FirstOrDefaultAsync(m => m.Id == id && m.RoleId == roleId);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.RoleMenus.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}

public record RoleMenuRequest(
    string  MenuKey,
    string  MenuLabel,
    string? MenuIcon,
    string  MenuPath,
    int     MenuOrder,
    string? ParentKey,
    Guid?   LocationId,
    bool    IsVisible
);

// ═══════════════════════════════════════════════════════════════════════════
//  USER PERMISSIONS — per-user override of role permissions
//  GET    /api/admin/users/{userId}/permissions
//  PUT    /api/admin/users/{userId}/permissions          (bulk upsert)
//  DELETE /api/admin/users/{userId}/permissions/{id}
//
//  Also:
//  GET    /api/admin/users/{userId}/menu                 (effective menu = role menus + overrides)
// ═══════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/admin/users/{userId:guid}/permissions")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class UserPermissionController : ControllerBase
{
    private readonly AppDbContext _db;
    public UserPermissionController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid userId)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { success = false, message = "User not found." });

        var perms = await _db.UserPermissions
            .Include(p => p.Location)
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.MenuKey)
            .Select(p => new
            {
                p.Id, p.MenuKey,
                LocationName = p.Location != null ? p.Location.LocationName : "All",
                p.LocationId, p.CanView, p.CanCreate, p.CanEdit, p.CanDelete
            })
            .ToListAsync();

        // Also return effective role modules for reference
        var roleModules = user.RoleId.HasValue
            ? await _db.RoleModules
                .Where(m => m.RoleId == user.RoleId)
                .Select(m => new { m.ModuleKey, m.ModuleName, m.CanView, m.CanCreate, m.CanEdit, m.CanDelete })
                .ToListAsync()
            : new();

        return Ok(new
        {
            success = true,
            data = new
            {
                userId,
                userName        = user.UserName,
                roleName        = user.Role?.RoleName,
                userPermissions = perms,
                roleModules
            }
        });
    }

    [HttpPut]
    public async Task<IActionResult> BulkUpsert(Guid userId, [FromBody] List<UserPermissionRequest> requests)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound(new { success = false, message = "User not found." });

        foreach (var req in requests)
        {
            var existing = await _db.UserPermissions
                .FirstOrDefaultAsync(p => p.UserId == userId && p.MenuKey == req.MenuKey
                                       && p.LocationId == req.LocationId);
            if (existing != null)
            {
                existing.CanView   = req.CanView;
                existing.CanCreate = req.CanCreate;
                existing.CanEdit   = req.CanEdit;
                existing.CanDelete = req.CanDelete;
            }
            else
            {
                _db.UserPermissions.Add(new UserPermission
                {
                    Id         = Guid.NewGuid(),
                    UserId     = userId,
                    LocationId = req.LocationId,
                    MenuKey    = req.MenuKey,
                    CanView    = req.CanView,
                    CanCreate  = req.CanCreate,
                    CanEdit    = req.CanEdit,
                    CanDelete  = req.CanDelete,
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"{requests.Count} permission(s) saved." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid id)
    {
        var item = await _db.UserPermissions.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.UserPermissions.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Permission removed." });
    }
}

[ApiController]
[Route("api/admin/users/{userId:guid}/menu")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class UserEffectiveMenuController : ControllerBase
{
    private readonly AppDbContext _db;
    public UserEffectiveMenuController(AppDbContext db) => _db = db;

    /// <summary>
    /// Returns the effective menu for a user:
    /// Role menus + any user-level permission overrides merged together.
    /// Frontend uses this to build the sidebar.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEffectiveMenu(Guid userId, [FromQuery] Guid? locationId)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { success = false, message = "User not found." });

        // 1. Get role menus
        var roleMenus = user.RoleId.HasValue
            ? await _db.RoleMenus
                .Where(m => m.RoleId == user.RoleId
                         && m.IsVisible
                         && (m.LocationId == null || m.LocationId == locationId))
                .OrderBy(m => m.MenuOrder)
                .ToListAsync()
            : new();

        // 2. Get user-level module permissions
        var userPerms = await _db.UserPermissions
            .Where(p => p.UserId == userId && (p.LocationId == null || p.LocationId == locationId))
            .ToListAsync();

        // 3. Get role module permissions
        var roleModules = user.RoleId.HasValue
            ? await _db.RoleModules
                .Where(m => m.RoleId == user.RoleId
                         && (m.LocationId == null || m.LocationId == locationId))
                .ToListAsync()
            : new();

        // 4. Merge — user override wins over role default
        var effectiveMenu = roleMenus.Select(menu =>
        {
            var roleModule = roleModules.FirstOrDefault(rm => rm.ModuleKey == menu.MenuKey);
            var userPerm   = userPerms.FirstOrDefault(up => up.MenuKey == menu.MenuKey);

            return new
            {
                menu.MenuKey,
                menu.MenuLabel,
                menu.MenuIcon,
                menu.MenuPath,
                menu.MenuOrder,
                menu.ParentKey,
                // User override wins, then role module, then default true
                CanView   = userPerm?.CanView   ?? roleModule?.CanView   ?? true,
                CanCreate = userPerm?.CanCreate  ?? roleModule?.CanCreate ?? false,
                CanEdit   = userPerm?.CanEdit    ?? roleModule?.CanEdit   ?? false,
                CanDelete = userPerm?.CanDelete  ?? roleModule?.CanDelete ?? false,
                CanExport = roleModule?.CanExport ?? false,
                IsOverridden = userPerm != null,
            };
        }).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                userId,
                userName = user.UserName,
                roleName = user.Role?.RoleName,
                menu     = effectiveMenu,
            }
        });
    }
}

public record UserPermissionRequest(
    string MenuKey,
    Guid?  LocationId,
    bool   CanView,
    bool   CanCreate,
    bool   CanEdit,
    bool   CanDelete
);
