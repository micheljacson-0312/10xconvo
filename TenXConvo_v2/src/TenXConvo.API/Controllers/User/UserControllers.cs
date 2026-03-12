using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using TenXConvo.Application.Interfaces;

namespace TenXConvo.API.Controllers.User;

[ApiController][Route("api/user/consultants")][EnableCors("UserPortal")]
public class UserConsultantsController : ControllerBase
{
    private readonly IUserService _svc;
    public UserConsultantsController(IUserService svc) => _svc = svc;
    private Guid UserId => Guid.Parse(User.FindFirst("sub")?.Value!);

    [HttpGet][AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int page=1,[FromQuery] int pageSize=12,[FromQuery] string? search=null)
        => Ok(new{success=true,data=await _svc.GetConsultantsAsync(page,pageSize,search)});

    [HttpGet("{id:guid}")][AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    { try{return Ok(new{success=true,data=await _svc.GetConsultantByIdAsync(id)});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }

    /// <summary>Public profile by slug — e.g. GET /api/user/consultants/by-slug/ali-khan</summary>
    [HttpGet("by-slug/{slug}")][AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug)
    { try{return Ok(new{success=true,data=await _svc.GetConsultantBySlugAsync(slug)});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }

    [HttpPost("{id:guid}/connect")][Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> Connect(Guid id)
    { try{return Ok(new{success=true,data=await _svc.ConnectAsync(UserId,id)});}catch(Exception ex){return BadRequest(new{success=false,message=ex.Message});} }
}

[ApiController][Route("api/user/profile")][Authorize(Policy="ClientOnly")][EnableCors("UserPortal")]
public class CustomerProfileController : ControllerBase
{
    private readonly IUserService _svc;
    public CustomerProfileController(IUserService svc) => _svc = svc;
    private Guid UserId => Guid.Parse(User.FindFirst("sub")?.Value!);

    [HttpGet] public async Task<IActionResult> Get() => Ok(new{success=true,data=await _svc.GetMyProfileAsync(UserId)});
    [HttpPut] public async Task<IActionResult> Update([FromBody] CustomerProfileInput req) => Ok(new{success=true,data=await _svc.UpdateProfileAsync(UserId,req)});
}

[ApiController][Route("api/user/messages")][Authorize(Policy="ClientOnly")][EnableCors("UserPortal")]
public class UserMessagingController : ControllerBase
{
    private readonly IUserService _svc;
    public UserMessagingController(IUserService svc) => _svc = svc;
    private Guid UserId => Guid.Parse(User.FindFirst("sub")?.Value!);

    [HttpGet]               public async Task<IActionResult> GetConversations([FromQuery] int page=1,[FromQuery] int pageSize=20) => Ok(new{success=true,data=await _svc.GetConversationsAsync(UserId,page,pageSize)});
    [HttpGet("{id:guid}")]  public async Task<IActionResult> GetMessages(Guid id,[FromQuery] int page=1,[FromQuery] int pageSize=50) => Ok(new{success=true,data=await _svc.GetMessagesAsync(id,UserId,page,pageSize)});
    [HttpPost("{id:guid}")] public async Task<IActionResult> Send(Guid id,[FromBody] UserSendMessageRequest req) => Ok(new{success=true,data=await _svc.SendMessageAsync(id,UserId,req.Body)});
}
public record UserSendMessageRequest(string Body);
