using TenXConvo.API.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TenXConvo.API.Helpers;

// ═══════════════════════════════════════════════════════════════════════════
//  PART A — 7 MISSING CONTROLLERS
//  1. WaTemplate           api/admin/notifications/templates/wa
//  2. SmsTemplate          api/admin/notifications/templates/sms
//  3. EmailTemplate        api/admin/notifications/templates/email
//  4. WebTemplate          api/admin/notifications/templates/web
//  5. WebNotification      api/user/notifications  + api/consultant/notifications
//  6. AppNotification      api/admin/notifications/app
//  7. ConsultantAvailability api/consultant/availability
//  8. ConsultantReview     api/user/consultants/{id}/reviews
//  9. FiscalYear           api/admin/setup/fiscal-years
// ═══════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
// 1. WA TEMPLATES
// ─────────────────────────────────────────────────────────────────────────────

namespace TenXConvo.API.Controllers.Admin
{

[ApiController]
[Route("api/admin/notifications/templates/wa")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class WaTemplateController : ControllerBase
{
    private readonly AppDbContext _db;
    public WaTemplateController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.WaTemplates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(t => t.TemplateName.Contains(search) || t.TemplateTitle.Contains(search));
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(t => t.Status == status);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.CreatedOn)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new { t.Id, t.TemplateName, t.TemplateTitle, t.Activity, t.Status, t.CreatedOn })
            .ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var t = await _db.WaTemplates.FindAsync(id);
        if (t == null) return NotFound(new { success = false, message = "Template not found." });
        return Ok(new { success = true, data = t });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WaTemplateRequest req)
    {
        var duplicate = await _db.WaTemplates.AnyAsync(t => t.TemplateName == req.TemplateName);
        if (duplicate) return BadRequest(new { success = false, message = "Template name already exists." });

        var item = new WaTemplate
        {
            Id            = Guid.NewGuid(),
            TemplateName  = req.TemplateName,
            TemplateTitle = req.TemplateTitle,
            Activity      = req.Activity,
            Body          = req.Body,
            Variables     = req.Variables,
            Status        = "active",
            CreatedOn     = DateTime.UtcNow,
        };
        _db.WaTemplates.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WaTemplateRequest req)
    {
        var item = await _db.WaTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });

        item.TemplateName  = req.TemplateName;
        item.TemplateTitle = req.TemplateTitle;
        item.Activity      = req.Activity;
        item.Body          = req.Body;
        item.Variables     = req.Variables;
        item.UpdatedAt     = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest req)
    {
        var item = await _db.WaTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.Status    = req.Status;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"Status set to {req.Status}." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.WaTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.WaTemplates.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }

    // ── Preview: replace {{variables}} with sample values ────────────────────
    [HttpPost("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id, [FromBody] Dictionary<string, string> vars)
    {
        var item = await _db.WaTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        var preview = item.Body;
        foreach (var (k, v) in vars)
            preview = preview.Replace($"{{{{{k}}}}}", v);
        return Ok(new { success = true, data = new { preview, original = item.Body } });
    }
}
public record WaTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Body, string? Variables);

// ─────────────────────────────────────────────────────────────────────────────
// 2. SMS TEMPLATES
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/admin/notifications/templates/sms")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class SmsTemplateController : ControllerBase
{
    private readonly AppDbContext _db;
    public SmsTemplateController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.SmsTemplates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(t => t.TemplateName.Contains(search));
        if (!string.IsNullOrWhiteSpace(status))  q = q.Where(t => t.Status == status);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.CreatedOn)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new { t.Id, t.TemplateName, t.TemplateTitle, t.Activity, t.Status, t.CreatedOn })
            .ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var t = await _db.SmsTemplates.FindAsync(id);
        if (t == null) return NotFound(new { success = false, message = "Not found." });
        return Ok(new { success = true, data = t });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SmsTemplateRequest req)
    {
        var item = new SmsTemplate
        {
            Id = Guid.NewGuid(), TemplateName = req.TemplateName, TemplateTitle = req.TemplateTitle,
            Activity = req.Activity, Body = req.Body, Status = "active", CreatedOn = DateTime.UtcNow,
        };
        _db.SmsTemplates.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SmsTemplateRequest req)
    {
        var item = await _db.SmsTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.TemplateName = req.TemplateName; item.TemplateTitle = req.TemplateTitle;
        item.Activity = req.Activity; item.Body = req.Body; item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest req)
    {
        var item = await _db.SmsTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.Status = req.Status; item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"Status set to {req.Status}." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.SmsTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.SmsTemplates.Remove(item); await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }

    [HttpPost("char-count")]
    public IActionResult CharCount([FromBody] SmsCharCountRequest req)
    {
        var len     = req.Message.Length;
        var smsCount = (int)Math.Ceiling(len / 160.0);
        return Ok(new { success = true, data = new { length = len, smsCount, remaining = (smsCount * 160) - len } });
    }
}
public record SmsTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Body);
public record SmsCharCountRequest(string Message);

// ─────────────────────────────────────────────────────────────────────────────
// 3. EMAIL TEMPLATES
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/admin/notifications/templates/email")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class EmailTemplateController : ControllerBase
{
    private readonly AppDbContext _db;
    public EmailTemplateController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.EmailTemplates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(t => t.TemplateName.Contains(search) || t.Subject.Contains(search));
        if (!string.IsNullOrWhiteSpace(status))  q = q.Where(t => t.Status == status);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.CreatedOn)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new { t.Id, t.TemplateName, t.TemplateTitle, t.Subject, t.Activity, t.Status, t.CreatedOn })
            .ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var t = await _db.EmailTemplates.FindAsync(id);
        if (t == null) return NotFound(new { success = false, message = "Not found." });
        return Ok(new { success = true, data = t });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmailTemplateRequest req)
    {
        var item = new EmailTemplate
        {
            Id = Guid.NewGuid(), TemplateName = req.TemplateName, TemplateTitle = req.TemplateTitle,
            Activity = req.Activity, Subject = req.Subject, Body = req.Body,
            Status = "active", CreatedOn = DateTime.UtcNow,
        };
        _db.EmailTemplates.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] EmailTemplateRequest req)
    {
        var item = await _db.EmailTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.TemplateName = req.TemplateName; item.TemplateTitle = req.TemplateTitle;
        item.Activity = req.Activity; item.Subject = req.Subject;
        item.Body = req.Body; item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest req)
    {
        var item = await _db.EmailTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.Status = req.Status; item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"Status set to {req.Status}." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.EmailTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.EmailTemplates.Remove(item); await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }

    // ── Preview with variable substitution ───────────────────────────────────
    [HttpPost("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id, [FromBody] Dictionary<string, string> vars)
    {
        var item = await _db.EmailTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        var subject = item.Subject;
        var body    = item.Body;
        foreach (var (k, v) in vars)
        {
            subject = subject.Replace($"{{{{{k}}}}}", v);
            body    = body.Replace($"{{{{{k}}}}}", v);
        }
        return Ok(new { success = true, data = new { subject, body } });
    }

    // ── Send test email using this template ───────────────────────────────────
    [HttpPost("{id:guid}/send-test")]
    public async Task<IActionResult> SendTest(Guid id, [FromBody] SendTestEmailRequest req,
        [FromServices] NotificationDispatcher notify)
    {
        var item = await _db.EmailTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        var body = item.Body;
        foreach (var (k, v) in req.Variables ?? new())
            body = body.Replace($"{{{{{k}}}}}", v);
        var (ok, status) = await notify.SendEmailAsync(req.TestEmail, item.Subject, body);
        return Ok(new { success = ok, message = ok ? "Test email sent." : $"Failed: {status}" });
    }
}
public record EmailTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Subject, string Body);
public record SendTestEmailRequest(string TestEmail, Dictionary<string, string>? Variables);

// ─────────────────────────────────────────────────────────────────────────────
// 4. WEB TEMPLATES (in-app push body templates)
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/admin/notifications/templates/web")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class WebTemplateController : ControllerBase
{
    private readonly AppDbContext _db;
    public WebTemplateController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.WebTemplates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(t => t.TemplateName.Contains(search));
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.CreatedOn).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var t = await _db.WebTemplates.FindAsync(id);
        if (t == null) return NotFound(new { success = false, message = "Not found." });
        return Ok(new { success = true, data = t });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WebTemplateRequest req)
    {
        var item = new WebTemplate
        {
            Id = Guid.NewGuid(), TemplateName = req.TemplateName, TemplateTitle = req.TemplateTitle,
            Activity = req.Activity, Body = req.Body, Status = "active", CreatedOn = DateTime.UtcNow,
        };
        _db.WebTemplates.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WebTemplateRequest req)
    {
        var item = await _db.WebTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.TemplateName = req.TemplateName; item.TemplateTitle = req.TemplateTitle;
        item.Activity = req.Activity; item.Body = req.Body; item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest req)
    {
        var item = await _db.WebTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.Status = req.Status; item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"Status set to {req.Status}." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.WebTemplates.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.WebTemplates.Remove(item); await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record WebTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Body);
public record SetStatusRequest(string Status);  // shared by all 4 template types

// ─────────────────────────────────────────────────────────────────────────────
// 5. WEB NOTIFICATIONS (bell icon — per-user inbox)
//    Admin sends → users receive in their bell icon
//    GET  /user/notifications         → my unread notifications
//    PUT  /user/notifications/{id}/read
//    PUT  /user/notifications/read-all
//    --- Admin side ---
//    POST /api/admin/notifications/web/send
//    GET  /api/admin/notifications/web
// ─────────────────────────────────────────────────────────────────────────────
}


namespace TenXConvo.API.Controllers.User
{

[ApiController]
[Route("api/user/notifications")]
[Authorize(Policy = "ClientOnly")]
[EnableCors("UserPortal")]
public class UserWebNotificationController : ControllerBase
{
    private readonly AppDbContext _db;
    public UserWebNotificationController(AppDbContext db) => _db = db;

private Guid MyUserId => AuthHelper.GetUserId(User);
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.WebNotifications.Where(n => n.UserId == MyUserId);
        if (unreadOnly) q = q.Where(n => !n.IsRead);
        var total   = await q.CountAsync();
        var unreadCount = await _db.WebNotifications.CountAsync(n => n.UserId == MyUserId && !n.IsRead);
        var items   = await q.OrderByDescending(n => n.CreatedOn)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new { n.Id, n.Title, n.Body, n.Url, n.IsRead, n.CreatedOn, n.ReadAt })
            .ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, unreadCount, page, pageSize } });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var n = await _db.WebNotifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == MyUserId);
        if (n == null) return NotFound(new { success = false, message = "Not found." });
        if (!n.IsRead) { n.IsRead = true; n.ReadAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
        return Ok(new { success = true });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var unread = await _db.WebNotifications
            .Where(n => n.UserId == MyUserId && !n.IsRead).ToListAsync();
        unread.ForEach(n => { n.IsRead = true; n.ReadAt = DateTime.UtcNow; });
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"{unread.Count} notifications marked as read." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var n = await _db.WebNotifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == MyUserId);
        if (n == null) return NotFound(new { success = false, message = "Not found." });
        _db.WebNotifications.Remove(n); await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

// Consultant also has bell icon
[ApiController]
[Route("api/consultant/notifications")]
[Authorize(Policy = "ConsultantOnly")]
[EnableCors("ConsultantPortal")]
public class ConsultantWebNotificationController : ControllerBase
{
    private readonly AppDbContext _db;
    public ConsultantWebNotificationController(AppDbContext db) => _db = db;
private Guid MyUserId => AuthHelper.GetUserId(User);
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.WebNotifications.Where(n => n.UserId == MyUserId);
        if (unreadOnly) q = q.Where(n => !n.IsRead);
        var total       = await q.CountAsync();
        var unreadCount = await _db.WebNotifications.CountAsync(n => n.UserId == MyUserId && !n.IsRead);
        var items       = await q.OrderByDescending(n => n.CreatedOn)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, unreadCount } });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var n = await _db.WebNotifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == MyUserId);
        if (n == null) return NotFound(new { success = false, message = "Not found." });
        n.IsRead = true; n.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var unread = await _db.WebNotifications.Where(n => n.UserId == MyUserId && !n.IsRead).ToListAsync();
        unread.ForEach(n => { n.IsRead = true; n.ReadAt = DateTime.UtcNow; });
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"{unread.Count} marked as read." });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 6. APP NOTIFICATIONS (admin broadcasts: Holiday, General)
//    Admin: POST api/admin/notifications/app           → create + send to targets
//    Admin: GET  api/admin/notifications/app           → list all broadcasts
//    Admin: GET  api/admin/notifications/app/{id}      → detail + read receipts
//    User/Consultant: delivered via WebNotification + SignalR
// ─────────────────────────────────────────────────────────────────────────────
}


namespace TenXConvo.API.Controllers.Admin
{

[ApiController]
[Route("api/admin/notifications/app")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class AppNotificationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public AppNotificationController(AppDbContext db,
        IHubContext<ChatHub> hub)
    { _db = db; _hub = hub; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.AppNotifications.AsQueryable();
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(n => n.Type == type);
        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(n => n.CreatedOn)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new
            {
                n.Id, n.Title, n.Message, n.Type, n.Sender, n.CreatedOn,
                TargetCount = n.Targets.Count,
                ReadCount   = n.Targets.Count(t => t.IsRead),
            })
            .ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var n = await _db.AppNotifications
            .Include(n => n.Targets).ThenInclude(t => t.User)
            .FirstOrDefaultAsync(n => n.Id == id);
        if (n == null) return NotFound(new { success = false, message = "Not found." });
        return Ok(new
        {
            success = true,
            data = new
            {
                n.Id, n.Title, n.Message, n.Type, n.Sender, n.CreatedOn,
                targets = n.Targets.Select(t => new
                {
                    t.UserId, t.User.UserName, t.IsRead, t.ReadAt,
                    ImageUrl = t.User.ImageUrl,
                })
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppNotificationRequest req)
    {
        var senderName = User.FindFirst("name")?.Value
                      ?? User.FindFirst("sub")?.Value
                      ?? "Admin";

        // Resolve target users
        List<AppUser> targets;
        if (req.SendToAll)
        {
            targets = await _db.Users.Where(u => u.IsActive).ToListAsync();
        }
        else if (req.TargetUserIds?.Any() == true)
        {
            targets = await _db.Users.Where(u => req.TargetUserIds.Contains(u.Id) && u.IsActive).ToListAsync();
        }
        else if (!string.IsNullOrEmpty(req.TargetRoleName))
        {
            targets = await _db.Users
                .Include(u => u.Role)
                .Where(u => u.IsActive && u.Role != null && u.Role.RoleName == req.TargetRoleName)
                .ToListAsync();
        }
        else
        {
            return BadRequest(new { success = false, message = "Specify sendToAll, targetUserIds, or targetRoleName." });
        }

        if (targets.Count == 0)
            return BadRequest(new { success = false, message = "No active users matched the target criteria." });

        // Create AppNotification
        var notif = new AppNotification
        {
            Id        = Guid.NewGuid(),
            Title     = req.Title,
            Message   = req.Message,
            Type      = req.Type ?? "General",
            Sender    = senderName,
            SentBy    = User.FindFirst("sub")?.Value,
            CreatedOn = DateTime.UtcNow,
        };

        // Create WebNotification for each target + AppNotificationTarget entry
        var webNotifs = new List<WebNotification>();
        foreach (var user in targets)
        {
            notif.Targets.Add(new AppNotificationTarget
            {
                Id             = Guid.NewGuid(),
                NotificationId = notif.Id,
                UserId         = user.Id,
                IsRead         = false,
            });

            webNotifs.Add(new WebNotification
            {
                Id        = Guid.NewGuid(),
                UserId    = user.Id,
                Title     = req.Title,
                Body      = req.Message,
                Url       = req.Url,
                IsRead    = false,
                CreatedOn = DateTime.UtcNow,
            });
        }

        _db.AppNotifications.Add(notif);
        _db.WebNotifications.AddRange(webNotifs);
        await _db.SaveChangesAsync();

        // Broadcast via SignalR to all connected users
        await _hub.Clients.All.SendAsync("NewAppNotification", new
        {
            notif.Id, notif.Title, notif.Message, notif.Type, notif.CreatedOn,
        });

        return Ok(new
        {
            success = true,
            message = $"Notification sent to {targets.Count} user(s).",
            data    = new { notif.Id, targetCount = targets.Count }
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var n = await _db.AppNotifications.Include(n => n.Targets).FirstOrDefaultAsync(n => n.Id == id);
        if (n == null) return NotFound(new { success = false, message = "Not found." });
        _db.AppNotificationTargets.RemoveRange(n.Targets);
        _db.AppNotifications.Remove(n);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Notification deleted." });
    }
}

public record CreateAppNotificationRequest(
    string Title,
    string Message,
    string? Type,             // "General" | "Holiday" | "Urgent" | "Maintenance"
    string? Url,              // optional click-through URL
    bool    SendToAll = false,
    List<Guid>? TargetUserIds = null,
    string? TargetRoleName = null
);

// ─────────────────────────────────────────────────────────────────────────────
// 7. CONSULTANT AVAILABILITY (weekly schedule)
//    GET    api/consultant/availability           → my schedule
//    PUT    api/consultant/availability           → bulk save full week
//    GET    api/user/consultants/{id}/availability → public: view slots
// ─────────────────────────────────────────────────────────────────────────────
}


namespace TenXConvo.API.Controllers.Consultant
{

[ApiController]
[Route("api/consultant/availability")]
[Authorize(Policy = "ConsultantOnly")]
[EnableCors("ConsultantPortal")]
public class ConsultantAvailabilityController : ControllerBase
{
    private readonly AppDbContext _db;
    public ConsultantAvailabilityController(AppDbContext db) => _db = db;

private Guid MyUserId => AuthHelper.GetUserId(User);
    [HttpGet]
    public async Task<IActionResult> GetMySchedule()
    {
        var profile = await _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.UserId == MyUserId);
        if (profile == null) return NotFound(new { success = false, message = "Profile not found." });

        var slots = await _db.ConsultantAvailabilities
            .Where(a => a.ConsultantId == profile.Id)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .Select(a => new
            {
                a.Id,
                DayOfWeek   = a.DayOfWeek.ToString(),
                DayNumber   = (int)a.DayOfWeek,
                StartTime   = a.StartTime.ToString("HH:mm"),
                EndTime     = a.EndTime.ToString("HH:mm"),
                a.IsAvailable,
            })
            .ToListAsync();

        // Build 7-day view with defaults for missing days
        var fullWeek = Enum.GetValues<DayOfWeek>().Select(day =>
        {
            var existing = slots.Where(s => s.DayNumber == (int)day).ToList();
            return new
            {
                Day       = day.ToString(),
                DayNumber = (int)day,
                Slots     = existing,
                IsWorkDay = existing.Any(s => s.IsAvailable),
            };
        });

        return Ok(new { success = true, data = fullWeek });
    }

    [HttpPut]
    public async Task<IActionResult> SaveSchedule([FromBody] List<AvailabilitySlotRequest> slots)
    {
        var profile = await _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.UserId == MyUserId);
        if (profile == null) return NotFound(new { success = false, message = "Profile not found." });

        // Validate: no overlapping slots on same day
        var grouped = slots.GroupBy(s => s.DayOfWeek);
        foreach (var dayGroup in grouped)
        {
            var ordered = dayGroup.OrderBy(s => s.StartTime).ToList();
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                if (TimeOnly.Parse(ordered[i].EndTime) > TimeOnly.Parse(ordered[i + 1].StartTime))
                    return BadRequest(new { success = false, message = $"Overlapping slots on {(DayOfWeek)dayGroup.Key}." });
            }
        }

        // Replace all slots for this consultant
        var existing = _db.ConsultantAvailabilities.Where(a => a.ConsultantId == profile.Id);
        _db.ConsultantAvailabilities.RemoveRange(existing);

        foreach (var slot in slots)
        {
            _db.ConsultantAvailabilities.Add(new ConsultantAvailability
            {
                Id           = Guid.NewGuid(),
                ConsultantId = profile.Id,
                DayOfWeek    = (DayOfWeek)slot.DayOfWeek,
                StartTime    = TimeOnly.Parse(slot.StartTime),
                EndTime      = TimeOnly.Parse(slot.EndTime),
                IsAvailable  = slot.IsAvailable,
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"Schedule saved ({slots.Count} slots)." });
    }

    [HttpDelete]
    public async Task<IActionResult> ClearSchedule()
    {
        var profile = await _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.UserId == MyUserId);
        if (profile == null) return NotFound(new { success = false, message = "Profile not found." });
        var slots = _db.ConsultantAvailabilities.Where(a => a.ConsultantId == profile.Id);
        _db.ConsultantAvailabilities.RemoveRange(slots);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Schedule cleared." });
    }
}

public record AvailabilitySlotRequest(int DayOfWeek, string StartTime, string EndTime, bool IsAvailable = true);

// Public endpoint — User portal views consultant schedule
}


namespace TenXConvo.API.Controllers.User
{

[ApiController]
[Route("api/user/consultants/{consultantUserId:guid}/availability")]
[EnableCors("UserPortal")]
public class PublicAvailabilityController : ControllerBase
{
    private readonly AppDbContext _db;
    public PublicAvailabilityController(AppDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicSchedule(Guid consultantUserId)
    {
        var profile = await _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.UserId == consultantUserId && p.IsPublic);
        if (profile == null) return NotFound(new { success = false, message = "Consultant not found." });

        var slots = await _db.ConsultantAvailabilities
            .Where(a => a.ConsultantId == profile.Id && a.IsAvailable)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .Select(a => new
            {
                DayOfWeek = a.DayOfWeek.ToString(),
                DayNumber = (int)a.DayOfWeek,
                StartTime = a.StartTime.ToString("HH:mm"),
                EndTime   = a.EndTime.ToString("HH:mm"),
            })
            .ToListAsync();

        return Ok(new { success = true, data = slots });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 8. CONSULTANT REVIEWS (1-5 star rating system)
//    POST /user/consultants/{id}/reviews     → submit review (after connection)
//    GET  /user/consultants/{id}/reviews     → public reviews
//    PUT  /user/consultants/{id}/reviews/{reviewId}  → edit my review
//    DEL  /user/consultants/{id}/reviews/{reviewId}  → delete my review
//    GET  /admin/data/reviews                → admin view all
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/user/consultants/{consultantUserId:guid}/reviews")]
[EnableCors("UserPortal")]
public class ConsultantReviewController : ControllerBase
{
    private readonly AppDbContext _db;
    public ConsultantReviewController(AppDbContext db) => _db = db;

    private Guid? MyUserId => User.FindFirst("sub") == null ? null : Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetReviews(Guid consultantUserId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var profile = await _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.UserId == consultantUserId);
        if (profile == null) return NotFound(new { success = false, message = "Consultant not found." });

        var q     = _db.ConsultantReviews.Include(r => r.Customer).ThenInclude(c => c.User)
                       .Where(r => r.ConsultantId == profile.Id);
        var total = await q.CountAsync();
        var avg   = total > 0 ? await q.AverageAsync(r => (double)r.Rating) : 0.0;

        var items = await q.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new
            {
                r.Id, r.Rating, r.Comment, r.CreatedAt,
                ReviewerName = r.Customer.User.UserName,
                ReviewerAvatar = r.Customer.AvatarUrl,
                IsMyReview = r.Customer.UserId == (MyUserId ?? Guid.Empty)
            })
            .ToListAsync();

        var ratingBreakdown = await _db.ConsultantReviews
            .Where(r => r.ConsultantId == profile.Id)
            .GroupBy(r => r.Rating)
            .Select(g => new { Stars = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                items, totalRecords = total, page, pageSize,
                averageRating = Math.Round(avg, 1),
                ratingBreakdown,
            }
        });
    }

    [HttpPost]
    [Authorize(Policy = "ClientOnly")]
    public async Task<IActionResult> Create(Guid consultantUserId, [FromBody] CreateReviewRequest req)
    {
        if (req.Rating is < 1 or > 5)
            return BadRequest(new { success = false, message = "Rating must be 1–5." });

        var consultant = await _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.UserId == consultantUserId);
        if (consultant == null) return NotFound(new { success = false, message = "Consultant not found." });

        var myProfile = await _db.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == MyUserId!.Value);
        if (myProfile == null) return BadRequest(new { success = false, message = "Customer profile not found." });

        // Must have an accepted connection to leave a review
        var hasConnection = await _db.ClientConnections.AnyAsync(c =>
            c.ConsultantId == consultant.Id &&
            c.CustomerId   == myProfile.Id  &&
            c.Status       == "accepted");

        if (!hasConnection)
            return BadRequest(new { success = false, message = "You can only review consultants you are connected with." });

        // One review per customer per consultant
        var alreadyReviewed = await _db.ConsultantReviews.AnyAsync(r =>
            r.ConsultantId == consultant.Id && r.CustomerId == myProfile.Id);
        if (alreadyReviewed)
            return BadRequest(new { success = false, message = "You have already reviewed this consultant. Use edit to update." });

        var review = new ConsultantReview
        {
            Id           = Guid.NewGuid(),
            ConsultantId = consultant.Id,
            CustomerId   = myProfile.Id,
            Rating       = req.Rating,
            Comment      = req.Comment,
            CreatedAt    = DateTime.UtcNow,
        };
        _db.ConsultantReviews.Add(review);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Review submitted.", data = new { review.Id } });
    }

    [HttpPut("{reviewId:guid}")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<IActionResult> Update(Guid consultantUserId, Guid reviewId, [FromBody] CreateReviewRequest req)
    {
        if (req.Rating is < 1 or > 5)
            return BadRequest(new { success = false, message = "Rating must be 1–5." });

        var myProfile = await _db.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == MyUserId!.Value);
        if (myProfile == null) return BadRequest(new { success = false, message = "Profile not found." });

        var review = await _db.ConsultantReviews.FirstOrDefaultAsync(r =>
            r.Id == reviewId && r.CustomerId == myProfile.Id);
        if (review == null) return NotFound(new { success = false, message = "Review not found or not yours." });

        review.Rating    = req.Rating;
        review.Comment   = req.Comment;
        review.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Review updated." });
    }

    [HttpDelete("{reviewId:guid}")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<IActionResult> Delete(Guid consultantUserId, Guid reviewId)
    {
        var myProfile = await _db.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == MyUserId!.Value);
        if (myProfile == null) return BadRequest(new { success = false, message = "Profile not found." });

        var review = await _db.ConsultantReviews.FirstOrDefaultAsync(r =>
            r.Id == reviewId && r.CustomerId == myProfile.Id);
        if (review == null) return NotFound(new { success = false, message = "Review not found or not yours." });

        _db.ConsultantReviews.Remove(review);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Review deleted." });
    }
}

public record CreateReviewRequest(int Rating, string? Comment);

// Admin view
}


namespace TenXConvo.API.Controllers.Admin
{

[ApiController]
[Route("api/admin/data/reviews")]
[Authorize]
[EnableCors("AdminPortal")]
public class AdminReviewController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminReviewController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? minRating, [FromQuery] int? maxRating,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.ConsultantReviews
            .Include(r => r.Consultant).ThenInclude(c => c.User)
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .AsQueryable();

        if (minRating.HasValue) q = q.Where(r => r.Rating >= minRating.Value);
        if (maxRating.HasValue) q = q.Where(r => r.Rating <= maxRating.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new
            {
                r.Id, r.Rating, r.Comment, r.CreatedAt,
                ConsultantName = r.Consultant.User.UserName,
                ReviewerName   = r.Customer.User.UserName,
            })
            .ToListAsync();

        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var r = await _db.ConsultantReviews.FindAsync(id);
        if (r == null) return NotFound(new { success = false, message = "Not found." });
        _db.ConsultantReviews.Remove(r);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Review removed." });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 9. FISCAL YEAR MANAGEMENT
//    GET  /admin/setup/fiscal-years
//    POST /admin/setup/fiscal-years
//    PUT  /admin/setup/fiscal-years/{id}
//    DEL  /admin/setup/fiscal-years/{id}
//    PUT  /admin/setup/fiscal-years/{id}/set-current
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/admin/setup/fiscal-years")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class FiscalYearController : ControllerBase
{
    private readonly AppDbContext _db;
    public FiscalYearController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.FiscalYears
            .OrderByDescending(f => f.StartDate)
            .Select(f => new
            {
                f.Id, f.Name, f.StartDate, f.EndDate,
                f.IsActive, f.IsCurrent, f.CreatedAt
            })
            .ToListAsync();
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FiscalYearRequest req)
    {
        if (req.EndDate <= req.StartDate)
            return BadRequest(new { success = false, message = "End date must be after start date." });

        // Check overlap with existing FYs
        var overlap = await _db.FiscalYears.AnyAsync(f =>
            f.StartDate < req.EndDate && f.EndDate > req.StartDate);
        if (overlap)
            return BadRequest(new { success = false, message = "Date range overlaps with an existing fiscal year." });

        var fy = new FiscalYear
        {
            Id        = Guid.NewGuid(),
            Name      = req.Name,
            StartDate = req.StartDate,
            EndDate   = req.EndDate,
            IsActive  = req.IsActive,
            IsCurrent = false,
            CreatedAt = DateTime.UtcNow,
        };
        _db.FiscalYears.Add(fy);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = fy });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FiscalYearRequest req)
    {
        var fy = await _db.FiscalYears.FindAsync(id);
        if (fy == null) return NotFound(new { success = false, message = "Not found." });

        if (req.EndDate <= req.StartDate)
            return BadRequest(new { success = false, message = "End date must be after start date." });

        // Check overlap (exclude self)
        var overlap = await _db.FiscalYears.AnyAsync(f =>
            f.Id != id && f.StartDate < req.EndDate && f.EndDate > req.StartDate);
        if (overlap)
            return BadRequest(new { success = false, message = "Date range overlaps with another fiscal year." });

        fy.Name      = req.Name;
        fy.StartDate = req.StartDate;
        fy.EndDate   = req.EndDate;
        fy.IsActive  = req.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = fy });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var fy = await _db.FiscalYears.FindAsync(id);
        if (fy == null) return NotFound(new { success = false, message = "Not found." });
        if (fy.IsCurrent)
            return BadRequest(new { success = false, message = "Cannot delete the current fiscal year." });
        _db.FiscalYears.Remove(fy);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }

    // ── Set as current FY ─────────────────────────────────────────────────────
    [HttpPut("{id:guid}/set-current")]
    public async Task<IActionResult> SetCurrent(Guid id)
    {
        var all = await _db.FiscalYears.ToListAsync();
        var target = all.FirstOrDefault(f => f.Id == id);
        if (target == null) return NotFound(new { success = false, message = "Not found." });

        all.ForEach(f => f.IsCurrent = false);
        target.IsCurrent = true;
        target.IsActive  = true;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"'{target.Name}' is now the current fiscal year." });
    }

    // ── Toggle active/inactive ────────────────────────────────────────────────
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var fy = await _db.FiscalYears.FindAsync(id);
        if (fy == null) return NotFound(new { success = false, message = "Not found." });
        if (fy.IsCurrent && fy.IsActive)
            return BadRequest(new { success = false, message = "Cannot deactivate the current fiscal year." });
        fy.IsActive = !fy.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"Fiscal year {(fy.IsActive ? "activated" : "deactivated")}." });
    }
}

public record FiscalYearRequest(string Name, DateTime StartDate, DateTime EndDate, bool IsActive = true);
}
