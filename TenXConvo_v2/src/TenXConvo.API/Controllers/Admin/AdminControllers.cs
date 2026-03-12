using TenXConvo.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using TenXConvo.Application.Interfaces;

namespace TenXConvo.API.Controllers.Admin;

[ApiController][Route("api/admin/settings")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class SettingsController : ControllerBase
{
    private readonly IAdminService _svc;
    public SettingsController(IAdminService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> Get() => Ok(new { success=true, data = await _svc.GetSettingsAsync() });
    [HttpPut("website")] public async Task<IActionResult> UpdateWebsite([FromBody] WebsiteSettingsRequest req) { await _svc.UpdateWebsiteSettingsAsync(req.IsWebsiteOnline, req.FooterDescription); return Ok(new { success=true, message="Saved." }); }
    [HttpPut("business")] public async Task<IActionResult> UpdateBusiness([FromBody] BusinessSettingsInput req) { await _svc.UpdateBusinessSettingsAsync(req); return Ok(new { success=true, message="Saved." }); }
    [HttpPut("roles")] public async Task<IActionResult> UpdateRoles([FromBody] RolesSettingsRequest req) { await _svc.UpdateRolesSettingsAsync(req.ConsultantDefaultRoleId, req.ClientDefaultRoleId, req.ChatLinkUrl); return Ok(new { success=true, message="Saved." }); }
}
public record WebsiteSettingsRequest(bool IsWebsiteOnline, string? FooterDescription);
public record RolesSettingsRequest(Guid? ConsultantDefaultRoleId, Guid? ClientDefaultRoleId, string? ChatLinkUrl);

[ApiController][Route("api/admin/setup/organization")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class OrganizationController : ControllerBase
{
    private readonly IAdminService _svc;
    public OrganizationController(IAdminService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> Get() => Ok(new { success=true, data = await _svc.GetOrganizationAsync() });
    [HttpPut] public async Task<IActionResult> Update([FromBody] OrganizationInput req) { await _svc.UpdateOrganizationAsync(req); return Ok(new { success=true, message="Updated." }); }
    [HttpPost("logo")][ValidateFile("logo", maxSizeMb: 2)] public async Task<IActionResult> UploadLogo([FromForm] IFormFile file) { var url = await _svc.UploadLogoAsync(file.OpenReadStream(), file.FileName); return Ok(new { success=true, data=new{url} }); }
}

[ApiController][Route("api/admin/setup/locations")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class LocationController : ControllerBase
{
    private readonly IAdminService _svc;
    public LocationController(IAdminService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int page=1,[FromQuery] int pageSize=10,[FromQuery] string? search=null) => Ok(new { success=true, data = await _svc.GetLocationsAsync(page, pageSize, search) });
    [HttpPost] public async Task<IActionResult> Create([FromBody] LocationInput req) => Ok(new { success=true, data = await _svc.CreateLocationAsync(req) });
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id,[FromBody] LocationInput req) { try { return Ok(new{success=true,data=await _svc.UpdateLocationAsync(id,req)}); } catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id) { try { await _svc.DeleteLocationAsync(id); return Ok(new{success=true,message="Deleted."}); } catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
}

[ApiController][Route("api/admin/users/roles")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class RoleController : ControllerBase
{
    private readonly IAdminService _svc;
    public RoleController(IAdminService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int page=1,[FromQuery] int pageSize=10,[FromQuery] string? search=null) => Ok(new { success=true, data = await _svc.GetRolesAsync(page, pageSize, search) });
    [HttpPost] public async Task<IActionResult> Create([FromBody] RoleRequest req) => Ok(new { success=true, data = await _svc.CreateRoleAsync(req.RoleName) });
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id,[FromBody] RoleRequest req) { try{return Ok(new{success=true,data=await _svc.UpdateRoleAsync(id,req.RoleName)});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id) { try{await _svc.DeleteRoleAsync(id);return Ok(new{success=true,message="Deleted."});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
}
public record RoleRequest(string RoleName);

[ApiController][Route("api/admin/users/registrations")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class UserRegistrationController : ControllerBase
{
    private readonly IAdminService _svc;
    public UserRegistrationController(IAdminService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int page=1,[FromQuery] int pageSize=10,[FromQuery] string? search=null) => Ok(new { success=true, data = await _svc.GetUsersAsync(page, pageSize, search) });
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { try{return Ok(new{success=true,data=await _svc.GetUserByIdAsync(id)});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateUserInput req) { try{return Ok(new{success=true,data=await _svc.CreateUserAsync(req)});}catch(InvalidOperationException ex){return BadRequest(new{success=false,message=ex.Message});} }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id,[FromBody] UpdateUserInput req) { try{return Ok(new{success=true,data=await _svc.UpdateUserAsync(id,req)});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id) { try{await _svc.DeleteUserAsync(id);return Ok(new{success=true,message="Deleted."});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
    [HttpPost("{id:guid}/avatar")][ValidateFile("avatar", maxSizeMb: 2)] public async Task<IActionResult> UploadAvatar(Guid id,[FromForm] IFormFile file) { var url=await _svc.UploadAvatarAsync(id,file.OpenReadStream(),file.FileName); return Ok(new{success=true,data=new{url}}); }
    [HttpPut("{id:guid}/reset-password")] public async Task<IActionResult> ResetPassword(Guid id,[FromBody] ResetPasswordRequest req) { try{await _svc.ResetPasswordAsync(id,req.NewPassword);return Ok(new{success=true,message="Reset."});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
}
public record ResetPasswordRequest(string NewPassword);

[ApiController][Route("api/admin/system/error-logs")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class ErrorLogController : ControllerBase
{
    private readonly IAdminService _svc;
    public ErrorLogController(IAdminService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int page=1,[FromQuery] int pageSize=10,[FromQuery] string? search=null) => Ok(new { success=true, data = await _svc.GetErrorLogsAsync(page, pageSize, search) });
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { try{return Ok(new{success=true,data=await _svc.GetErrorLogByIdAsync(id)});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id) { try{await _svc.DeleteErrorLogAsync(id);return Ok(new{success=true,message="Deleted."});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
}
