using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using SendGrid;
using SendGrid.Helpers.Mail;
using TenXConvo.Application.Interfaces;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

namespace TenXConvo.Infrastructure.Services
{

// ═══════════════════════════════════════════════════════════════════════════
//  NOTIFICATION SERVICES
//  All 3 channels: WhatsApp, SMS (Twilio) + Email (SendGrid)
//  Falls back to console logging in dev when credentials not configured
// ═══════════════════════════════════════════════════════════════════════════

// ── WHATSAPP + SMS via TWILIO ─────────────────────────────────────────────────

public class TwilioNotificationService : IWaNotificationService, ISmsNotificationService
{
    private readonly IConfiguration  _config;
    private readonly ILogger<TwilioNotificationService> _log;
    private readonly AppDbContext     _db;
    private readonly bool             _isConfigured;

    public TwilioNotificationService(IConfiguration config, ILogger<TwilioNotificationService> log, AppDbContext db)
    {
        _config = config; _log = log; _db = db;

        var sid   = config["Twilio:AccountSid"];
        var token = config["Twilio:AuthToken"];
        _isConfigured = !string.IsNullOrEmpty(sid) && !sid.StartsWith("AC") == false
                        && !sid.Contains("xxx");

        if (_isConfigured)
            TwilioClient.Init(sid, token);
        else
            _log.LogWarning("⚠️  Twilio not configured — notifications will be logged only.");
    }

    // ── WHATSAPP ──────────────────────────────────────────────────────────────

    public async Task<(bool Success, string? MessageSid, string? Error)> SendWhatsAppAsync(string toNumber, string message)
    {
        // Normalise number → E.164 e.g. +923001234567
        var to = NormaliseNumber(toNumber);

        var log = new WaNotification
        {
            Id       = Guid.NewGuid(),
            SendNo   = to,
            Message  = message,
            Status   = "pending",
            SubmitOn = DateTime.UtcNow
        };
        _db.WaNotifications.Add(log);

        if (!_isConfigured)
        {
            _log.LogInformation("📱 [WA-MOCK] To: {To} | {Msg}", to, message);
            log.Status = "mock-sent";
            await _db.SaveChangesAsync();
            return (true, "mock-sid", null);
        }

        try
        {
            var from = _config["Twilio:WhatsAppFrom"] ?? "whatsapp:+14155238886";
            var msg  = await MessageResource.CreateAsync(
                to:   new PhoneNumber($"whatsapp:{to}"),
                from: new PhoneNumber(from),
                body: message
            );

            log.Status = msg.Status.ToString();
            await _db.SaveChangesAsync();
            return (true, msg.Sid, null);
        }
        catch (Exception ex)
        {
            log.Status = "failed";
            await _db.SaveChangesAsync();
            _log.LogError(ex, "WhatsApp send failed to {To}", to);
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? MessageSid, string? Error)> SendWhatsAppTemplateAsync(string toNumber, string templateSid, Dictionary<string, string>? variables = null)
    {
        // For template messages, build body from variables then send
        var body = variables != null
            ? string.Join(", ", variables.Select(kv => $"{kv.Key}: {kv.Value}"))
            : "Template notification";
        return await SendWhatsAppAsync(toNumber, body);
    }

    // ── SMS ───────────────────────────────────────────────────────────────────

    public async Task<(bool Success, string? MessageSid, string? Error)> SendSmsAsync(string toNumber, string message)
    {
        var to  = NormaliseNumber(toNumber);
        var log = new SmsNotification
        {
            Id          = Guid.NewGuid(),
            PhoneNumber = to,
            Message     = message,
            Status      = "pending",
            SubmitDate  = DateTime.UtcNow
        };
        _db.SmsNotifications.Add(log);

        if (!_isConfigured)
        {
            _log.LogInformation("📟 [SMS-MOCK] To: {To} | {Msg}", to, message);
            log.Status = "mock-sent";
            await _db.SaveChangesAsync();
            return (true, "mock-sid", null);
        }

        try
        {
            var from = _config["Twilio:FromNumber"] ?? throw new Exception("Twilio:FromNumber not set");
            var msg  = await MessageResource.CreateAsync(
                to:   new PhoneNumber(to),
                from: new PhoneNumber(from),
                body: message
            );

            log.Status = msg.Status.ToString();
            await _db.SaveChangesAsync();
            return (true, msg.Sid, null);
        }
        catch (Exception ex)
        {
            log.Status = "failed";
            await _db.SaveChangesAsync();
            _log.LogError(ex, "SMS send failed to {To}", to);
            return (false, null, ex.Message);
        }
    }

    // ── HELPER ────────────────────────────────────────────────────────────────

    private static string NormaliseNumber(string number)
    {
        // Remove spaces/dashes, ensure + prefix
        var n = number.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        return n.StartsWith("+") ? n : $"+{n}";
    }
}

// ── EMAIL via SENDGRID ────────────────────────────────────────────────────────

public class SendGridEmailService : IEmailNotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailService> _log;
    private readonly AppDbContext   _db;
    private readonly bool           _isConfigured;

    public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> log, AppDbContext db)
    {
        _config = config; _log = log; _db = db;
        var key = config["SendGrid:ApiKey"];
        _isConfigured = !string.IsNullOrEmpty(key) && !key.Contains("xxx") && key.StartsWith("SG.");
        if (!_isConfigured)
            _log.LogWarning("⚠️  SendGrid not configured — emails will be logged only.");
    }

    public async Task<(bool Success, string? MessageId, string? Error)> SendEmailAsync(
        string toEmail, string toName, string subject, string htmlBody, string? plainBody = null)
    {
        var log = new EmailNotification
        {
            Id         = Guid.NewGuid(),
            ToEmail    = toEmail,
            Subject    = subject,
            Status     = "pending",
            SubmitDate = DateTime.UtcNow
        };
        _db.EmailNotifications.Add(log);

        if (!_isConfigured)
        {
            _log.LogInformation("📧 [EMAIL-MOCK] To: {To} | Subject: {Sub}", toEmail, subject);
            log.Status = "mock-sent";
            await _db.SaveChangesAsync();
            return (true, "mock-id", null);
        }

        try
        {
            var apiKey  = _config["SendGrid:ApiKey"]!;
            var from    = _config["SendGrid:FromEmail"] ?? "noreply@10xdigitalventures.com";
            var fromName = _config["SendGrid:FromName"] ?? "10X Convo";

            var client  = new SendGridClient(apiKey);
            var msg     = MailHelper.CreateSingleEmail(
                from:    new EmailAddress(from, fromName),
                to:      new EmailAddress(toEmail, toName),
                subject: subject,
                plainTextContent: plainBody ?? System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]+>", ""),
                htmlContent: htmlBody
            );

            var response = await client.SendEmailAsync(msg);
            var success  = (int)response.StatusCode is >= 200 and < 300;

            log.Status = success ? "sent" : $"failed:{(int)response.StatusCode}";
            await _db.SaveChangesAsync();
            return (success, null, success ? null : $"SendGrid status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            log.Status = "failed";
            await _db.SaveChangesAsync();
            _log.LogError(ex, "Email send failed to {To}", toEmail);
            return (false, null, ex.Message);
        }
    }

    // ── Convenience wrappers for common emails ────────────────────────────────

    public async Task SendOtpEmailAsync(string toEmail, string toName, string otp)
    {
        var html = $$"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
              <h2 style="color:#6c63ff">10X Convo — Login OTP</h2>
              <p>Hi {toName},</p>
              <p>Your one-time password is:</p>
              <div style="font-size:36px;font-weight:800;letter-spacing:8px;color:#6c63ff;margin:20px 0">{otp}</div>
              <p style="color:#888">Valid for 10 minutes. Do not share this code.</p>
            </div>
            """;
        await SendEmailAsync(toEmail, toName, "Your Login OTP — 10X Convo", html);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string toName)
    {
        var portalUrl = _config["PortalUrls:User"] ?? "http://localhost:3004";
        var html = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
              <h2 style="color:#6c63ff">Welcome to 10X Convo 🎉</h2>
              <p>Hi {toName},</p>
              <p>Your account has been created. Connect with expert consultants and grow your business.</p>
              <a href="{portalUrl}" style="display:inline-block;background:#6c63ff;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;margin-top:16px">Get Started ↗</a>
            </div>
            """;
        await SendEmailAsync(toEmail, toName, "Welcome to 10X Convo!", html);
    }

    public async Task SendConnectionAcceptedEmailAsync(string toEmail, string toName, string consultantName)
    {
        var portalUrl = _config["PortalUrls:User"] ?? "http://localhost:3004";
        var html = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
              <h2 style="color:#00d4aa">Connection Accepted ✅</h2>
              <p>Hi {toName},</p>
              <p><strong>{consultantName}</strong> accepted your connection request. You can now start chatting!</p>
              <a href="{portalUrl}/messages" style="display:inline-block;background:#00d4aa;color:#000;padding:12px 24px;border-radius:8px;text-decoration:none;margin-top:16px">Open Chat ↗</a>
            </div>
            """;
        await SendEmailAsync(toEmail, toName, $"{consultantName} accepted your request!", html);
    }
}

// ── INTERFACES ────────────────────────────────────────────────────────────────
// (defined in Application/Interfaces so services can use them)
}

namespace TenXConvo.Application.Interfaces
{
    public interface IWaNotificationService
    {
        Task<(bool Success, string? MessageSid, string? Error)> SendWhatsAppAsync(string toNumber, string message);
        Task<(bool Success, string? MessageSid, string? Error)> SendWhatsAppTemplateAsync(string toNumber, string templateSid, Dictionary<string, string>? variables = null);
    }

    public interface ISmsNotificationService
    {
        Task<(bool Success, string? MessageSid, string? Error)> SendSmsAsync(string toNumber, string message);
    }

    public interface IEmailNotificationService
    {
        Task<(bool Success, string? MessageId, string? Error)> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string? plainBody = null);
        Task SendWelcomeEmailAsync(string toEmail, string toName);
        Task SendConnectionAcceptedEmailAsync(string toEmail, string toName, string consultantName);
    }
}
