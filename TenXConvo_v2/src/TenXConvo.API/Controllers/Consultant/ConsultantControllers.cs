using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using TenXConvo.Application.Interfaces;
using System.Security.Claims;

namespace TenXConvo.API.Controllers.Consultant;

[ApiController][Route("api/consultant/profile")][Authorize(Policy="ConsultantOnly")][EnableCors("ConsultantPortal")]
public class ConsultantProfileController : ControllerBase
{
    private readonly IConsultantService _svc;
    public ConsultantProfileController(IConsultantService svc) => _svc = svc;
private Guid UserId
{
    get
    {
        var id =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("nameid")?.Value;

        if (string.IsNullOrEmpty(id))
            throw new UnauthorizedAccessException("UserId claim missing from token");

        return Guid.Parse(id);
    }
}
    [HttpGet]   public async Task<IActionResult> Get() => Ok(new{success=true,data=await _svc.GetMyProfileAsync(UserId)});
    [HttpPut]   public async Task<IActionResult> Update([FromBody] ConsultantProfileInput req) => Ok(new{success=true,data=await _svc.UpdateProfileAsync(UserId,req)});
    [HttpPost("avatar")] public async Task<IActionResult> Avatar([FromForm] IFormFile file) { var url=await _svc.UploadAvatarAsync(UserId,file.OpenReadStream(),file.FileName); return Ok(new{success=true,data=new{url}}); }
    [HttpPut("online")] public async Task<IActionResult> Online([FromBody] OnlineRequest req) { await _svc.SetOnlineStatusAsync(UserId,req.IsOnline); return Ok(new{success=true}); }
}
public record OnlineRequest(bool IsOnline);

[ApiController][Route("api/consultant/clients")][Authorize(Policy="ConsultantOnly")][EnableCors("ConsultantPortal")]
public class ConsultantClientsController : ControllerBase
{
    private readonly IConsultantService _svc;
    public ConsultantClientsController(IConsultantService svc) => _svc = svc;
private Guid UserId
{
    get
    {
        var id =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("nameid")?.Value;

        if (string.IsNullOrEmpty(id))
            throw new UnauthorizedAccessException("UserId claim missing from token");

        return Guid.Parse(id);
    }
}
    [HttpGet]         public async Task<IActionResult> GetAll([FromQuery] int page=1,[FromQuery] int pageSize=10,[FromQuery] string? search=null) => Ok(new{success=true,data=await _svc.GetMyClientsAsync(UserId,page,pageSize,search)});
    [HttpGet("requests")] public async Task<IActionResult> Requests() => Ok(new{success=true,data=await _svc.GetPendingRequestsAsync(UserId)});
    [HttpPut("requests/{id:guid}/accept")] public async Task<IActionResult> Accept(Guid id) { try{await _svc.AcceptRequestAsync(id,UserId);return Ok(new{success=true,message="Accepted."});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
    [HttpPut("requests/{id:guid}/reject")] public async Task<IActionResult> Reject(Guid id) { try{await _svc.RejectRequestAsync(id,UserId);return Ok(new{success=true,message="Rejected."});}catch(KeyNotFoundException ex){return NotFound(new{success=false,message=ex.Message});} }
}

[ApiController][Route("api/consultant/messages")][Authorize(Policy="ConsultantOnly")][EnableCors("ConsultantPortal")]
public class ConsultantMessagingController : ControllerBase
{
    private readonly IConsultantService _svc;
    public ConsultantMessagingController(IConsultantService svc) => _svc = svc;
private Guid UserId
{
    get
    {
        var id =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("nameid")?.Value;

        if (string.IsNullOrEmpty(id))
            throw new UnauthorizedAccessException("UserId claim missing from token");

        return Guid.Parse(id);
    }
}
    [HttpGet]                   public async Task<IActionResult> GetConversations([FromQuery] int page=1,[FromQuery] int pageSize=20) => Ok(new{success=true,data=await _svc.GetConversationsAsync(UserId,page,pageSize)});
    [HttpGet("{id:guid}")]      public async Task<IActionResult> GetMessages(Guid id,[FromQuery] int page=1,[FromQuery] int pageSize=50) => Ok(new{success=true,data=await _svc.GetMessagesAsync(id,UserId,page,pageSize)});
    [HttpPost("{id:guid}")]     public async Task<IActionResult> Send(Guid id,[FromBody] SendMessageRequest req) => Ok(new{success=true,data=await _svc.SendMessageAsync(id,UserId,req.Body,req.MessageType??"text")});
    [HttpPut("{id:guid}/read")] public async Task<IActionResult> Read(Guid id) { await _svc.MarkConversationReadAsync(id,UserId); return Ok(new{success=true}); }
}
public record SendMessageRequest(string Body, string? MessageType);
