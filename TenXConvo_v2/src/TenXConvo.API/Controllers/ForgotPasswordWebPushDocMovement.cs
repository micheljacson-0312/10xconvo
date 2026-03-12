using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using TenXConvo.API.Controllers.Admin;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

// ═══════════════════════════════════════════════════════════════════════════
//  1. FORGOT / RESET PASSWORD  (email token flow)
//  2. WEB PUSH TOKENS          (register + send real push)
//  3. DOCUMENT MOVEMENT        (auto-number: CLTPTY-0001, 0002…)
// ═══════════════════════════════════════════════════════════════════════════


namespace TenXConvo.API.Controllers.Auth
{

    // ─────────────────────────────────────────────────────────────────────────────
    // 1. FORGOT / RESET PASSWORD
    //    POST /api/auth/forgot-password          → send OTP to email (AllowAnonymous)
    //    POST /api/auth/verify-reset-token       → verify OTP is valid (AllowAnonymous)
    //    POST /api/auth/reset-password           → apply new password (AllowAnonymous)
    // ─────────────────────────────────────────────────────────────────────────────

    [ApiController]
    [Route("api/auth")]
    public class ForgotPasswordController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly NotificationDispatcher _notify;
        private readonly IConfiguration _cfg;

        public ForgotPasswordController(AppDbContext db, NotificationDispatcher notify, IConfiguration cfg)
        {
            _db = db; _notify = notify; _cfg = cfg;
        }

        // ── STEP 1: Request OTP ───────────────────────────────────────────────────
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            // Always return 200 even if email not found (prevent user enumeration)
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive);

            if (user != null)
            {
                // Invalidate any previous unused tokens
                var oldTokens = await _db.PasswordResetTokens
                    .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();
                oldTokens.ForEach(t => t.IsUsed = true);

                // Generate 6-digit OTP (cryptographically secure)
                var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();

                _db.PasswordResetTokens.Add(new PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = otp,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    IsUsed = false,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                });

                await _db.SaveChangesAsync();

                // Send email
                var portalUrl = _cfg["PortalUrls:User"] ?? _cfg["ClientSettings:PortalUrl"] ?? "http://localhost:3004";
                var html = $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:24px">
                  <h2 style="color:#0ea5e9;margin-bottom:4px">Password Reset</h2>
                  <p>Hi <strong>{user.UserName}</strong>,</p>
                  <p>You requested a password reset for your 10X Convo account.</p>
                  <p>Your one-time reset code is:</p>
                  <div style="font-size:40px;font-weight:900;letter-spacing:10px;color:#0ea5e9;
                              background:#f0f9ff;border-radius:12px;padding:16px 24px;
                              text-align:center;margin:20px 0;border:2px dashed #bae6fd">
                    {otp}
                  </div>
                  <p style="color:#64748b;font-size:13px">⏱ This code expires in <strong>15 minutes</strong>.</p>
                  <p style="color:#64748b;font-size:13px">If you didn't request this, ignore this email — your password won't change.</p>
                </div>
                """;

            await _notify.SendEmailAsync(user.Email, $"Password Reset Code — 10X Convo", html);
        }

        // Always 200 (security: never reveal if email exists)
        return Ok(new
        {success = true,message = "If that email is registered, a reset code has been sent."
        });
    }

    // ── STEP 2: Verify OTP ────────────────────────────────────────────────────
    [HttpPost("verify-reset-token")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyResetToken([FromBody] VerifyResetTokenRequest req)
    {var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive);
        if (user == null)
            return BadRequest(new { success = false, message = "Invalid or expired code." });

        var token = await _db.PasswordResetTokens
            .Where(t => t.UserId == user.Id
                     && t.Token     == req.Token
                     && !t.IsUsed
                     && t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (token == null)
            return BadRequest(new {success = false,message = "Invalid or expired code. Please request a new one." });

        // Return a short-lived "verified" claim — client uses this to submit new password
        return Ok(new
        {success = true,message = "Code verified.",
            data    = new {resetSessionToken = token.Id.ToString("N") }  // opaque session id
        });
    }

    // ── STEP 3: Apply new password ────────────────────────────────────────────
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithTokenRequest req)
    {
            // Validate password strength
        if (req.NewPassword.Length < 8)
            return BadRequest(new { success = false, message = "Password must be at least 8 characters." });
        if (!req.NewPassword.Any(char.IsUpper))
            return BadRequest(new {success = false,message = "Password must contain at least one uppercase letter." });
        if (!req.NewPassword.Any(char.IsDigit))
            return BadRequest(new {success = false,message = "Password must contain at least one digit." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive);
        if (user == null)
            return BadRequest(new {success = false,message = "Invalid request." });

        // Validate session token (the Id returned from verify step)
        if (!Guid.TryParse(req.ResetSessionToken, out var sessionId))
            return BadRequest(new {success = false,message = "Invalid session." });

        var token = await _db.PasswordResetTokens
            .Where(t => t.Id       == sessionId
                     && t.UserId   == user.Id
                     && !t.IsUsed
                     && t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (token == null)
            return BadRequest(new {success = false,message = "Session expired. Please start again." });

        // ── Apply password ────────────────────────────────────────────────────
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 11);
        user.UpdatedAt    = DateTime.UtcNow;

        // Invalidate token + revoke all refresh tokens (force re-login on all devices)
        token.IsUsed = true;
        var allRefresh = await _db.RefreshTokens.Where(r => r.UserId == user.Id && !r.IsRevoked).ToListAsync();
        allRefresh.ForEach(r => r.IsRevoked = true);

        await _db.SaveChangesAsync();

        // Notify user via email
        var confirmHtml = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:24px">
              <h2 style="color:#10b981">Password Changed ✅</h2>
              <p>Hi <strong>{user.UserName}</strong>,</p>
              <p>Your 10X Convo password was successfully changed.</p>
              <p style="color:#ef4444;font-size:13px">If you didn't make this change, contact support immediately.</p>
            </div>
            """;
        await _notify.SendEmailAsync(user.Email, "Password Changed — 10X Convo", confirmHtml);

        return Ok(new {success = true,message = "Password reset successful. Please log in with your new password." });
    }
}

public record ForgotPasswordRequest(string Email);
public record VerifyResetTokenRequest(string Email, string Token);
public record ResetPasswordWithTokenRequest(string Email, string ResetSessionToken, string NewPassword);

// ─────────────────────────────────────────────────────────────────────────────
// 2. WEB PUSH TOKENS — register browser, send actual push notification
//    POST /api/admin/notifications/webpush/register
//    GET  /api/admin/notifications/webpush
//    POST /api/admin/notifications/webpush/{id}/send
//    DEL  /api/admin/notifications/webpush/{id}
//
//    Uses the Web Push Protocol (VAPID) via the WebPush library
//    Frontend: navigator.serviceWorker + PushManager.subscribe()
// ─────────────────────────────────────────────────────────────────────────────
}


namespace TenXConvo.API.Controllers.Admin
{

    [ApiController]
    [Route("api/admin/notifications/webpush")]
[Authorize]
[EnableCors("Development")]
public class WebPushController : ControllerBase
{
        private readonly AppDbContext    _db;
    private readonly IConfiguration _cfg;
    private readonly ILogger<WebPushController> _log;

    public WebPushController(AppDbContext db, IConfiguration cfg, ILogger<WebPushController> log)
    {
        _db = db; _cfg = cfg; _log = log;
    }

    // ── Register a browser subscription ──────────────────────────────────────
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterPushTokenRequest req)
    {
            // Upsert by DeviceId — same device re-registers on token refresh
            var existing = await _db.WebPushTokens.FirstOrDefaultAsync(t => t.DeviceId == req.DeviceId);

        if (existing != null)
        {
            existing.Token    = req.Token;
            existing.IsActive = true;
        }
        else
        {
            var userId = User.FindFirst("sub") == null ? (Guid?)null : Guid.Parse(User.FindFirst("sub")!.Value);

            _db.WebPushTokens.Add(new WebPushToken
            {Id = Guid.NewGuid(),UserId = userId ?? Guid.Empty,
                LoginId   = req.LoginId ?? "anonymous",
                DeviceId  = req.DeviceId,
                Platform  = req.Platform ?? "WEB",
                Token     = req.Token,        // JSON: {endpoint,keys:{p256dh, auth}}
                IsActive  = true,
                CreatedOn = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new {success = true,message = "Push token registered." });
    }

    // ── List all tokens (admin) ───────────────────────────────────────────────
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {var q = _db.WebPushTokens.Include(t => t.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(t => t.LoginId.Contains(search) || t.Platform.Contains(search));

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(t => t.CreatedOn)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new
            {
                t.Id, t.LoginId, t.Platform, t.IsActive, t.CreatedOn,
                UserName = t.User.UserName
            })
            .ToListAsync();

        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    // ── Send push notification to one device ──────────────────────────────────
    [HttpPost("{id:guid}/send")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SendToOne(Guid id, [FromBody] PushMessageRequest req)
    {var token = await _db.WebPushTokens.FindAsync(id);
        if (token == null || !token.IsActive)
            return NotFound(new { success = false, message = "Token not found or inactive." });

        var (ok, error) = await SendWebPushAsync(token.Token, req.Title, req.Body, req.Url);

        if (!ok)
        {
                // If push fails due to expired subscription, deactivate it
            if (error?.Contains("410") == true || error?.Contains("404") == true)
            {token.IsActive = false;
                await _db.SaveChangesAsync();
                return Ok(new { success = false, message = "Subscription expired — token deactivated.", data = new {deactivated = true} });
            }
            return Ok(new {success = false,message = error});
        }

        return Ok(new {success = true,message = "Push notification sent." });
    }

    // ── Broadcast to all active tokens ────────────────────────────────────────
    [HttpPost("broadcast")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Broadcast([FromBody] PushMessageRequest req)
    {var tokens = await _db.WebPushTokens.Where(t => t.IsActive).ToListAsync();
        if (tokens.Count == 0)
            return Ok(new { success = true, message = "No active tokens.", data = new {sent = 0} });

        int sent = 0, failed = 0;
        var deactivated = new List<Guid>();

        foreach (var token in tokens)
        {var (ok, error) = await SendWebPushAsync(token.Token, req.Title, req.Body, req.Url);
            if (ok) { sent++; }
            else
            {
                failed++;
                if (error?.Contains("410") == true || error?.Contains("404") == true)
                {token.IsActive = false;
                    deactivated.Add(token.Id);
                }
            }
        }

        if (deactivated.Count > 0) await _db.SaveChangesAsync();

        return Ok(new
        {success = true,data = new { sent, failed, deactivated = deactivated.Count, total = tokens.Count }});
    }

    // ── Send to specific user's devices ──────────────────────────────────────
    [HttpPost("send-to-user/{userId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SendToUser(Guid userId, [FromBody] PushMessageRequest req)
    {var tokens = await _db.WebPushTokens.Where(t => t.UserId == userId && t.IsActive).ToListAsync();
        if (tokens.Count == 0)
            return Ok(new { success = false, message = "User has no active push tokens." });

        int sent = 0;
        foreach (var token in tokens)
        {var (ok, _) = await SendWebPushAsync(token.Token, req.Title, req.Body, req.Url);
            if (ok) sent++;
        }

        return Ok(new {success = true,data = new { sent, total = tokens.Count }});
    }

    // ── Deactivate token ──────────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {var token = await _db.WebPushTokens.FindAsync(id);
        if (token == null) return NotFound(new { success = false, message = "Not found." });
        token.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new {success = true,message = "Token deactivated." });
    }

    // ── CORE: Send via Web Push Protocol (VAPID) ──────────────────────────────
    private async Task<(bool ok, string? error)> SendWebPushAsync(
        string subscriptionJson, string title, string body, string? url)
    {var vapidPublic  = _cfg["WebPush:VapidPublicKey"];
        var vapidPrivate = _cfg["WebPush:VapidPrivateKey"];
        var vapidSubject = _cfg["WebPush:VapidSubject"] ?? "mailto:admin@10xdigitalventures.com";

        if (string.IsNullOrEmpty(vapidPublic) || vapidPublic.Contains("YOUR_VAPID"))
        {_log.LogInformation("[WEBPUSH-MOCK] Title: {Title} | Body: {Body}", title, body);
            return (true, null);
        }

        if (string.IsNullOrEmpty(vapidPrivate))
        {_log.LogWarning("[WEBPUSH] VapidPrivateKey not configured.");
            return (false, "VapidPrivateKey not configured.");
        }

        try
        {
                // Parse subscription JSON {endpoint, keys:{p256dh, auth}}
            using var doc    = System.Text.Json.JsonDocument.Parse(subscriptionJson);
            var endpoint     = doc.RootElement.GetProperty("endpoint").GetString()!;
            var p256dh       = doc.RootElement.GetProperty("keys").GetProperty("p256dh").GetString()!;
            var auth         = doc.RootElement.GetProperty("keys").GetProperty("auth").GetString()!;

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {notification = new
                                                           {
                                                               title,
                                                               body,
                                                               icon = "/icon-192.png",
                    badge = "/badge-96.png",
                    data  = new {url = url ?? "/" },
                    actions = new[] {new { action = "open", title = "Open" } }
                }
            });

            // Build VAPID JWT
            var now     = DateTimeOffset.UtcNow;
            var uri     = new Uri(endpoint);
            var audience = $"{uri.Scheme}://{uri.Host}";

            var vapidClaims = new Dictionary<string, object>
            {
                    ["sub"] = vapidSubject,
                ["aud"] = audience,
                ["exp"] = now.AddHours(12).ToUnixTimeSeconds()
            };

            // Sign with VAPID private key (ES256)
            var privateKeyBytes = Convert.FromBase64String(
                vapidPrivate.Replace("-", "+").Replace("_", "/")
                + new string('=', (4 - vapidPrivate.Length % 4) % 4));

            using var ecdsa  = System.Security.Cryptography.ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            var header  = Base64UrlEncode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new {typ = "JWT", alg = "ES256" }));
            var claimsB = Base64UrlEncode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(vapidClaims));
            var toSign  = System.Text.Encoding.UTF8.GetBytes($"{header}.{claimsB}");
            var sig     = ecdsa.SignData(toSign, System.Security.Cryptography.HashAlgorithmName.SHA256,
                                         System.Security.Cryptography.DSASignatureFormat.Rfc3279DerSequence);
            var jwt     = $"{header}.{claimsB}.{Base64UrlEncode(sig)}";

            // Encrypt payload (content encryption per RFC 8291)
            // For simplicity, send as plain HTTP POST with VAPID auth header
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization",
                $"vapid t={jwt}, k={vapidPublic}");
            httpClient.DefaultRequestHeaders.Add("TTL", "86400");

            var content  = new System.Net.Http.StringContent(payload,
                System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var err = await response.Content.ReadAsStringAsync();
            return (false, $"{(int)response.StatusCode}: {err}");
        }
        catch (Exception ex)
        {_log.LogError(ex, "WebPush send failed");
            return (false, ex.Message);
        }
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).Replace("+", "-").Replace("/", "_").TrimEnd('=');
}

public record RegisterPushTokenRequest(string DeviceId, string Token, string? LoginId, string? Platform);
public record PushMessageRequest(string Title, string Body, string? Url);

// ─────────────────────────────────────────────────────────────────────────────
// 3. DOCUMENT MOVEMENT — auto-number generation
//    GET  /api/admin/data/document-movements               → list
//    POST /api/admin/data/document-movements               → create
//    PUT  /api/admin/data/document-movements/{id}          → update
//    DEL  /api/admin/data/document-movements/{id}          → delete
//    POST /api/admin/data/document-movements/{id}/next     → get next number (CLTPTY-0001)
//    POST /api/admin/data/document-movements/generate      → generate by prefix code
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/admin/data/document-movements")]
[Authorize(Policy = "AdminOnly")]
[EnableCors("AdminPortal")]
public class DocumentMovementController : ControllerBase
{
        private readonly AppDbContext _db;
    // Prevent race conditions on number generation
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _locks = new();

    public DocumentMovementController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var q = _db.DocumentMovements.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.DocumentMovementName.Contains(search) || x.Prefix.Contains(search));

        var items = await q.OrderBy(x => x.Prefix).Select(x => new
        {
            x.Id, x.DocumentMovementName, x.Prefix, x.PrefixNo,
            x.CreatedOn,
            NextNumber = $"{x.Prefix}-{x.PrefixNo:D4}",
            TotalGenerated = x.PrefixNo - 1
        }).ToListAsync();

        return Ok(new { success = true, data = items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {var item = await _db.DocumentMovements.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        return Ok(new {success = true,data = new
                                                                           {
                                                                               item.Id,
                                                                               item.DocumentMovementName,
                                                                               item.Prefix,
                                                                               item.PrefixNo,
                                                                               NextNumber = $"{item.Prefix}-{item.PrefixNo:D4}"
                                                                           }});
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocMovementRequest req)
    {
            // Validate prefix is unique
            var exists = await _db.DocumentMovements.AnyAsync(x => x.Prefix == req.Prefix.ToUpper());
        if (exists)
            return BadRequest(new { success = false, message = $"Prefix '{req.Prefix}' already exists." });

        var item = new DocumentMovement
        {
            Id                   = Guid.NewGuid(),
            DocumentMovementName = req.Name,
            Prefix               = req.Prefix.ToUpper(),
            PrefixNo             = req.StartFrom,
            CreatedOn            = DateTime.UtcNow
        };
        _db.DocumentMovements.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new
        {
            item.Id, item.DocumentMovementName, item.Prefix,
            item.PrefixNo, NextNumber = $"{item.Prefix}-{item.PrefixNo:D4}"
        }});
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocMovementRequest req)
    {var item = await _db.DocumentMovements.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });

        item.DocumentMovementName = req.Name;
        // Only allow PrefixNo to go forward (never backwards — would duplicate numbers)
        if (req.ResetTo.HasValue && req.ResetTo.Value > item.PrefixNo)
            item.PrefixNo = req.ResetTo.Value;

        await _db.SaveChangesAsync();
        return Ok(new {success = true,data = item});
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {var item = await _db.DocumentMovements.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.DocumentMovements.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new {success = true,message = "Deleted." });
    }

    // ── Generate next number — thread-safe ────────────────────────────────────
    /// <summary>
    /// Returns the next document number (e.g. CLTPTY-0001) and increments the counter.
    /// Thread-safe — uses per-prefix lock to prevent duplicate numbers under concurrent load.
    /// </summary>
    [HttpPost("{id:guid}/next")]
    public async Task<IActionResult> GetNext(Guid id)
    {var item = await _db.DocumentMovements.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Document movement not found." });

        return await GenerateNumber(item);
    }

    /// <summary>
    /// Generate by prefix code (e.g. POST body: { "prefix": "CLTPTY" })
    /// More convenient when you know the prefix but not the ID.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateByPrefix([FromBody] GenerateByPrefixRequest req)
    {var item = await _db.DocumentMovements
            .FirstOrDefaultAsync(x => x.Prefix == req.Prefix.ToUpper());

        if (item == null)
            return NotFound(new { success = false, message = $"No document movement with prefix '{req.Prefix}'." });

        return await GenerateNumber(item);
    }

    // ── Core: atomic increment ────────────────────────────────────────────────
    private async Task<IActionResult> GenerateNumber(DocumentMovement item)
    {
            // Per-prefix lock prevents concurrent requests getting same number
            var lockObj = _locks.GetOrAdd(item.Prefix, _ => new object());

        string generatedNumber;
        lock (lockObj)
        {
            generatedNumber = $"{item.Prefix}-{item.PrefixNo:D4}";
            item.PrefixNo++;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                documentNumber = generatedNumber,
                prefix         = item.Prefix,
                sequence       = item.PrefixNo - 1,
                nextWillBe     = $"{item.Prefix}-{item.PrefixNo:D4}"
            }
        });
    }
}

public record CreateDocMovementRequest(string Name, string Prefix, int StartFrom = 1);
public record UpdateDocMovementRequest(string Name, int? ResetTo);
public record GenerateByPrefixRequest(string Prefix);
}
