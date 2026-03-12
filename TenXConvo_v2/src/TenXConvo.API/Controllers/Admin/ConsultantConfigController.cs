using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

namespace TenXConvo.API.Controllers.Admin;

// ═══════════════════════════════════════════════════════════════════════════
//  CONSULTANT SERVICE CONFIG — Admin manages per-consultant settings
//  ⚙️ Settings icon on each consultant row in Users table
//
//  GET  /api/admin/consultant-config/{userId}     → get config (or defaults)
//  PUT  /api/admin/consultant-config/{userId}     → save config
// ═══════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/admin/consultant-config")]
[Authorize(Policy = "AdminOnly")]
public class AdminConsultantConfigController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminConsultantConfigController(AppDbContext db) => _db = db;

    /// <summary>Get consultant's service config (creates default if none exists)</summary>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId)
    {
        // Verify user exists and is a consultant
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { success = false, message = "User not found." });

        var config = await _db.ConsultantServiceConfigs
            .FirstOrDefaultAsync(c => c.ConsultantUserId == userId);

        if (config == null)
        {
            // Return defaults (don't save yet — save on first PUT)
            return Ok(new
            {
                success = true,
                data = new ConsultantConfigDto(
                    userId, user.UserName, user.Role?.RoleName ?? "",
                    // Services
                    true, true, false, true, true,
                    // Pricing (null = use global)
                    null, null, null, null, null, "USD",
                    // Gateways
                    true, true, true,
                    null, null, null, null, null,
                    false // isCustomized
                )
            });
        }

        return Ok(new
        {
            success = true,
            data = new ConsultantConfigDto(
                userId, user.UserName, user.Role?.RoleName ?? "",
                config.TextEnabled, config.AudioEnabled, config.VideoEnabled,
                config.ImageEnabled, config.FileEnabled,
                config.TextRate, config.AudioRate, config.VideoRate,
                config.ImageRate, config.FileRate, config.Currency,
                config.StripeEnabled, config.JazzCashEnabled, config.EasyPaisaEnabled,
                config.StripeAccountId, config.JazzCashAccount, config.EasyPaisaAccount,
                config.BankAccountNo, config.BankName,
                true // isCustomized
            )
        });
    }

    /// <summary>Save consultant's service config (upsert)</summary>
    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Save(Guid userId, [FromBody] SaveConsultantConfigRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { success = false, message = "User not found." });

        var config = await _db.ConsultantServiceConfigs
            .FirstOrDefaultAsync(c => c.ConsultantUserId == userId);

        if (config == null)
        {
            config = new ConsultantServiceConfig { ConsultantUserId = userId };
            _db.ConsultantServiceConfigs.Add(config);
        }

        // Tab 1: Services
        config.TextEnabled     = req.TextEnabled;
        config.AudioEnabled    = req.AudioEnabled;
        config.VideoEnabled    = req.VideoEnabled;
        config.ImageEnabled    = req.ImageEnabled;
        config.FileEnabled     = req.FileEnabled;

        // Tab 2: Pricing overrides
        config.TextRate  = req.TextRate;
        config.AudioRate = req.AudioRate;
        config.VideoRate = req.VideoRate;
        config.ImageRate = req.ImageRate;
        config.FileRate  = req.FileRate;
        config.Currency  = req.Currency ?? "USD";

        // Tab 3: Gateways
        config.StripeEnabled    = req.StripeEnabled;
        config.JazzCashEnabled  = req.JazzCashEnabled;
        config.EasyPaisaEnabled = req.EasyPaisaEnabled;
        config.StripeAccountId  = req.StripeAccountId;
        config.JazzCashAccount  = req.JazzCashAccount;
        config.EasyPaisaAccount = req.EasyPaisaAccount;
        config.BankAccountNo    = req.BankAccountNo;
        config.BankName         = req.BankName;

        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = User.FindFirst("name")?.Value ?? "Admin";

        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Consultant config saved." });
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────

public record ConsultantConfigDto(
    Guid UserId, string UserName, string RoleName,
    // Services
    bool TextEnabled, bool AudioEnabled, bool VideoEnabled,
    bool ImageEnabled, bool FileEnabled,
    // Pricing
    decimal? TextRate, decimal? AudioRate, decimal? VideoRate,
    decimal? ImageRate, decimal? FileRate, string Currency,
    // Gateways
    bool StripeEnabled, bool JazzCashEnabled, bool EasyPaisaEnabled,
    string? StripeAccountId, string? JazzCashAccount, string? EasyPaisaAccount,
    string? BankAccountNo, string? BankName,
    bool IsCustomized
);

public record SaveConsultantConfigRequest(
    // Services
    bool TextEnabled, bool AudioEnabled, bool VideoEnabled,
    bool ImageEnabled, bool FileEnabled,
    // Pricing
    decimal? TextRate, decimal? AudioRate, decimal? VideoRate,
    decimal? ImageRate, decimal? FileRate, string? Currency,
    // Gateways
    bool StripeEnabled, bool JazzCashEnabled, bool EasyPaisaEnabled,
    string? StripeAccountId, string? JazzCashAccount, string? EasyPaisaAccount,
    string? BankAccountNo, string? BankName
);
