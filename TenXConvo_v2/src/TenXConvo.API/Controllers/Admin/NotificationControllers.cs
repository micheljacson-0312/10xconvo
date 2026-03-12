using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using SendGrid;
using SendGrid.Helpers.Mail;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

namespace TenXConvo.API.Controllers.Admin;

// ═══════════════════════════════════════════════════════════════════════════
//  NOTIFICATION CONTROLLERS
//  WA:    POST /api/admin/notifications/wa/send
//  SMS:   POST /api/admin/notifications/sms/send
//  Email: POST /api/admin/notifications/email/send
//  All:   GET  /api/admin/notifications/{channel}    (history)
// ═══════════════════════════════════════════════════════════════════════════

// ── SHARED NOTIFICATION SERVICE (injected into controllers) ───────────────────

public class NotificationDispatcher
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<NotificationDispatcher> _log;
    private readonly AppDbContext _db;

    private readonly bool _twilioReady;
    private readonly bool _sendgridReady;

    public NotificationDispatcher(IConfiguration cfg, ILogger<NotificationDispatcher> log, AppDbContext db)
    {
        _cfg = cfg; _log = log; _db = db;

        var sid   = cfg["Twilio:AccountSid"]   ?? "";
        var token = cfg["Twilio:AuthToken"]    ?? "";
        _twilioReady = !string.IsNullOrEmpty(sid) && sid.StartsWith("AC") && !sid.Contains("xxx");
        if (_twilioReady) TwilioClient.Init(sid, token);

        var sgKey = cfg["SendGrid:ApiKey"] ?? "";
        _sendgridReady = sgKey.StartsWith("SG.") && !sgKey.Contains("xxx");
    }

    // ── WA ────────────────────────────────────────────────────────────────────

    public async Task<(bool ok, string status)> SendWaAsync(string toNumber, string message)
    {
        var to = NormalisePhone(toNumber);
        var entry = new WaNotification { Id = Guid.NewGuid(), SendNo = to, Message = message, Status = "pending", SubmitOn = DateTime.UtcNow };
        _db.WaNotifications.Add(entry);

        if (!_twilioReady)
        {
            _log.LogInformation("[WA-MOCK] → {To}: {Msg}", to, message);
            entry.Status = "mock-sent";
            await _db.SaveChangesAsync();
            return (true, "mock-sent");
        }
        try
        {
            var from = _cfg["Twilio:WhatsAppFrom"] ?? "whatsapp:+14155238886";
            var msg  = await MessageResource.CreateAsync(
                new PhoneNumber($"whatsapp:{to}"),
                from: new PhoneNumber(from),
                body: message);
            entry.Status = msg.Status.ToString();
            await _db.SaveChangesAsync();
            return (true, msg.Status.ToString());
        }
        catch (Exception ex)
        {
            entry.Status = "failed";
            await _db.SaveChangesAsync();
            _log.LogError(ex, "WA failed → {To}", to);
            return (false, ex.Message);
        }
    }

    // ── SMS ───────────────────────────────────────────────────────────────────

    public async Task<(bool ok, string status)> SendSmsAsync(string toNumber, string message)
    {
        var to = NormalisePhone(toNumber);
        var entry = new SmsNotification { Id = Guid.NewGuid(), PhoneNumber = to, Message = message, Status = "pending", SubmitDate = DateTime.UtcNow };
        _db.SmsNotifications.Add(entry);

        if (!_twilioReady)
        {
            _log.LogInformation("[SMS-MOCK] → {To}: {Msg}", to, message);
            entry.Status = "mock-sent";
            await _db.SaveChangesAsync();
            return (true, "mock-sent");
        }
        try
        {
            var from = _cfg["Twilio:FromNumber"] ?? throw new Exception("Twilio:FromNumber missing");
            var msg  = await MessageResource.CreateAsync(
                new PhoneNumber(to),
                from: new PhoneNumber(from),
                body: message);
            entry.Status = msg.Status.ToString();
            await _db.SaveChangesAsync();
            return (true, msg.Status.ToString());
        }
        catch (Exception ex)
        {
            entry.Status = "failed";
            await _db.SaveChangesAsync();
            _log.LogError(ex, "SMS failed → {To}", to);
            return (false, ex.Message);
        }
    }

    // ── EMAIL ─────────────────────────────────────────────────────────────────

    public async Task<(bool ok, string status)> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var entry = new EmailNotification { Id = Guid.NewGuid(), ToEmail = toEmail, Subject = subject, Status = "pending", SubmitDate = DateTime.UtcNow };
        _db.EmailNotifications.Add(entry);

        if (!_sendgridReady)
        {
            _log.LogInformation("[EMAIL-MOCK] → {To} | {Sub}", toEmail, subject);
            entry.Status = "mock-sent";
            await _db.SaveChangesAsync();
            return (true, "mock-sent");
        }
        try
        {
            var client   = new SendGridClient(_cfg["SendGrid:ApiKey"]!);
            var fromAddr = _cfg["SendGrid:FromEmail"] ?? "noreply@10xdigitalventures.com";
            var fromName = _cfg["SendGrid:FromName"]  ?? "10X Convo";
            var plain    = System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]+>", "");
            var msg      = MailHelper.CreateSingleEmail(
                new EmailAddress(fromAddr, fromName),
                new EmailAddress(toEmail),
                subject, plain, htmlBody);
            var resp = await client.SendEmailAsync(msg);
            var ok   = (int)resp.StatusCode is >= 200 and < 300;
            entry.Status = ok ? "sent" : $"failed:{(int)resp.StatusCode}";
            await _db.SaveChangesAsync();
            return (ok, entry.Status);
        }
        catch (Exception ex)
        {
            entry.Status = "failed";
            await _db.SaveChangesAsync();
            _log.LogError(ex, "Email failed → {To}", toEmail);
            return (false, ex.Message);
        }
    }

    // ── HELPER ────────────────────────────────────────────────────────────────

    private static string NormalisePhone(string n)
    {
        n = n.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        return n.StartsWith("+") ? n : $"+{n}";
    }

    public bool TwilioConfigured  => _twilioReady;
    public bool SendGridConfigured => _sendgridReady;
}

// ── WA CONTROLLER ─────────────────────────────────────────────────────────────

[ApiController][Route("api/admin/notifications/wa")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class WaNotificationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationDispatcher _svc;
    public WaNotificationController(AppDbContext db, NotificationDispatcher svc) { _db = db; _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var q = _db.WaNotifications.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.SendNo.Contains(search) || x.Message.Contains(search));
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.SubmitOn)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] WaSendRequest req)
    {
        var (ok, status) = await _svc.SendWaAsync(req.PhoneNumber, req.Message);
        return Ok(new
        {
            success = ok,
            message = ok ? $"WhatsApp sent ({status})" : $"Failed: {status}",
            data    = new { status, isSimulated = !_svc.TwilioConfigured }
        });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkSend([FromBody] WaBulkRequest req)
    {
        if (req.Numbers.Count > 100)
            return BadRequest(new { success = false, message = "Max 100 numbers per bulk send." });

        var results = new List<object>();
        foreach (var num in req.Numbers)
        {
            var (ok, status) = await _svc.SendWaAsync(num, req.Message);
            results.Add(new { number = num, ok, status });
        }
        return Ok(new { success = true, data = results });
    }
}
public record WaSendRequest(string PhoneNumber, string Message);
public record WaBulkRequest(List<string> Numbers, string Message);

// ── SMS CONTROLLER ────────────────────────────────────────────────────────────

[ApiController][Route("api/admin/notifications/sms")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class SmsNotificationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationDispatcher _svc;
    public SmsNotificationController(AppDbContext db, NotificationDispatcher svc) { _db = db; _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var q = _db.SmsNotifications.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.PhoneNumber.Contains(search) || x.Message.Contains(search));
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.SubmitDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SmsSendRequest req)
    {
        var (ok, status) = await _svc.SendSmsAsync(req.PhoneNumber, req.Message);
        return Ok(new
        {
            success = ok,
            message = ok ? $"SMS sent ({status})" : $"Failed: {status}",
            data    = new { status, isSimulated = !_svc.TwilioConfigured }
        });
    }
}
public record SmsSendRequest(string PhoneNumber, string Message);

// ── EMAIL CONTROLLER ──────────────────────────────────────────────────────────

[ApiController][Route("api/admin/notifications/email")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class EmailNotificationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationDispatcher _svc;
    public EmailNotificationController(AppDbContext db, NotificationDispatcher svc) { _db = db; _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var q = _db.EmailNotifications.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.ToEmail.Contains(search) || x.Subject.Contains(search));
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.SubmitDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] EmailSendRequest req)
    {
        var (ok, status) = await _svc.SendEmailAsync(req.ToEmail, req.Subject, req.HtmlBody);
        return Ok(new
        {
            success = ok,
            message = ok ? $"Email sent ({status})" : $"Failed: {status}",
            data    = new { status, isSimulated = !_svc.SendGridConfigured }
        });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkSend([FromBody] EmailBulkRequest req)
    {
        if (req.Emails.Count > 50)
            return BadRequest(new { success = false, message = "Max 50 emails per bulk send." });

        var results = new List<object>();
        foreach (var email in req.Emails)
        {
            var (ok, status) = await _svc.SendEmailAsync(email, req.Subject, req.HtmlBody);
            results.Add(new { email, ok, status });
        }
        return Ok(new { success = true, data = results });
    }
}
public record EmailSendRequest(string ToEmail, string Subject, string HtmlBody);
public record EmailBulkRequest(List<string> Emails, string Subject, string HtmlBody);
