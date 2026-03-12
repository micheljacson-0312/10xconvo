using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

namespace TenXConvo.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════
//  CREDIT SERVICE — Direct purchase model (no wallet/money balance)
//
//  Client buys: "5000 text chars + 10 audio mins" → pays via gateway
//  Client sends: "Hello world" (11 chars) → TextCharsRemaining -= 11
//  Frontend shows: "4,989 chars remaining | 10.0 min audio"
//  Balance 0 → "Buy more credits" prompt
//  Consultant → always FREE
// ═══════════════════════════════════════════════════════════════════════════

public class CreditService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CreditService> _log;

    public CreditService(AppDbContext db, ILogger<CreditService> log)
    { _db = db; _log = log; }

    // ── GET OR CREATE BALANCE ────────────────────────────────────────────────
    public async Task<CreditBalance> GetOrCreateBalanceAsync(Guid userId)
    {
        var bal = await _db.CreditBalances.FirstOrDefaultAsync(b => b.UserId == userId);
        if (bal != null) return bal;
        bal = new CreditBalance { UserId = userId };
        _db.CreditBalances.Add(bal);
        await _db.SaveChangesAsync();
        return bal;
    }

    // ── GET REMAINING CREDITS (for frontend display) ─────────────────────────
    public async Task<CreditInfo> GetCreditsAsync(Guid userId)
    {
        var bal = await GetOrCreateBalanceAsync(userId);
        return new CreditInfo(
            bal.TextCharsRemaining,
            bal.AudioMinsRemaining,
            bal.VideoMinsRemaining,
            bal.ImageCreditsRemaining,
            bal.FileCreditsRemaining
        );
    }

    // ── CHARGE FOR MESSAGE (deduct units) ────────────────────────────────────
    // Returns success/error. If false → message should NOT be sent.
    public async Task<ChargeResult> ChargeForMessageAsync(
        Guid userId, Guid messageId, string messageType,
        int? charCount = null, double? durationSeconds = null)
    {
        var bal = await GetOrCreateBalanceAsync(userId);

        switch (messageType)
        {
            case "text":
                var chars = charCount ?? 0;
                if (chars <= 0) return new ChargeResult(true, "text", 0, bal.TextCharsRemaining, null);
                if (bal.TextCharsRemaining < chars)
                    return new ChargeResult(false, "text", chars, bal.TextCharsRemaining,
                        $"Need {chars} characters but only {bal.TextCharsRemaining} remaining. Buy more text credits.");
                bal.TextCharsRemaining -= chars;
                RecordTransaction(bal, "message_charge", "text", -chars, bal.TextCharsRemaining, messageId, null, $"Text ({chars} chars)");
                break;

            case "audio":
                var audioMins = Math.Ceiling((durationSeconds ?? 60) / 60.0 * 10) / 10; // round up to 0.1 min
                if (bal.AudioMinsRemaining < audioMins)
                    return new ChargeResult(false, "audio", audioMins, bal.AudioMinsRemaining,
                        $"Need {audioMins:F1} min but only {bal.AudioMinsRemaining:F1} min remaining. Buy more audio credits.");
                bal.AudioMinsRemaining -= audioMins;
                RecordTransaction(bal, "message_charge", "audio", -audioMins, bal.AudioMinsRemaining, messageId, null, $"Audio ({audioMins:F1} min)");
                break;

            case "video":
                var videoMins = Math.Ceiling((durationSeconds ?? 60) / 60.0 * 10) / 10;
                if (bal.VideoMinsRemaining < videoMins)
                    return new ChargeResult(false, "video", videoMins, bal.VideoMinsRemaining,
                        $"Need {videoMins:F1} min but only {bal.VideoMinsRemaining:F1} min remaining. Buy more video credits.");
                bal.VideoMinsRemaining -= videoMins;
                RecordTransaction(bal, "message_charge", "video", -videoMins, bal.VideoMinsRemaining, messageId, null, $"Video ({videoMins:F1} min)");
                break;

            case "image":
                if (bal.ImageCreditsRemaining < 1)
                    return new ChargeResult(false, "image", 1, bal.ImageCreditsRemaining, "No image credits remaining. Buy more.");
                bal.ImageCreditsRemaining -= 1;
                RecordTransaction(bal, "message_charge", "image", -1, bal.ImageCreditsRemaining, messageId, null, "Image (1 credit)");
                break;

            case "file":
                if (bal.FileCreditsRemaining < 1)
                    return new ChargeResult(false, "file", 1, bal.FileCreditsRemaining, "No file credits remaining. Buy more.");
                bal.FileCreditsRemaining -= 1;
                RecordTransaction(bal, "message_charge", "file", -1, bal.FileCreditsRemaining, messageId, null, "File (1 credit)");
                break;

            default:
                return new ChargeResult(true, messageType, 0, 0, null); // unknown type = free
        }

        bal.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _log.LogInformation("Charged {Type} from user {UserId}: {Desc}", messageType, userId, messageType);
        return new ChargeResult(true, messageType, 0, GetRemainingForType(bal, messageType), null);
    }

    // ── ADD CREDITS (after successful payment) ───────────────────────────────
    public async Task<CreditInfo> AddCreditsAsync(Guid userId, Guid paymentId,
        int textChars, double audioMins, double videoMins, int imageCredits, int fileCredits)
    {
        var bal = await GetOrCreateBalanceAsync(userId);

        if (textChars > 0)   { bal.TextCharsRemaining    += textChars;   RecordTransaction(bal, "purchase", "text",  textChars,   bal.TextCharsRemaining,    null, paymentId, $"Purchased {textChars:N0} text chars"); }
        if (audioMins > 0)   { bal.AudioMinsRemaining    += audioMins;   RecordTransaction(bal, "purchase", "audio", audioMins,   bal.AudioMinsRemaining,    null, paymentId, $"Purchased {audioMins:F1} audio min"); }
        if (videoMins > 0)   { bal.VideoMinsRemaining    += videoMins;   RecordTransaction(bal, "purchase", "video", videoMins,   bal.VideoMinsRemaining,    null, paymentId, $"Purchased {videoMins:F1} video min"); }
        if (imageCredits > 0){ bal.ImageCreditsRemaining  += imageCredits; RecordTransaction(bal, "purchase", "image", imageCredits, bal.ImageCreditsRemaining, null, paymentId, $"Purchased {imageCredits} image credits"); }
        if (fileCredits > 0) { bal.FileCreditsRemaining   += fileCredits;  RecordTransaction(bal, "purchase", "file", fileCredits,  bal.FileCreditsRemaining,  null, paymentId, $"Purchased {fileCredits} file credits"); }

        bal.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetCreditsAsync(userId);
    }

    // ── ADMIN: GRANT CREDITS ─────────────────────────────────────────────────
    public async Task<CreditInfo> AdminGrantAsync(Guid userId, int textChars, double audioMins, double videoMins, int imageCredits, int fileCredits, string adminName)
    {
        var bal = await GetOrCreateBalanceAsync(userId);
        if (textChars != 0)   { bal.TextCharsRemaining    += textChars;    RecordTransaction(bal, "admin_grant", "text",  textChars,    bal.TextCharsRemaining,    null, null, $"Admin ({adminName}) granted {textChars:N0} text chars"); }
        if (audioMins != 0)   { bal.AudioMinsRemaining    += audioMins;    RecordTransaction(bal, "admin_grant", "audio", audioMins,    bal.AudioMinsRemaining,    null, null, $"Admin ({adminName}) granted {audioMins:F1} audio min"); }
        if (videoMins != 0)   { bal.VideoMinsRemaining    += videoMins;    RecordTransaction(bal, "admin_grant", "video", videoMins,    bal.VideoMinsRemaining,    null, null, $"Admin ({adminName}) granted {videoMins:F1} video min"); }
        if (imageCredits != 0){ bal.ImageCreditsRemaining  += imageCredits; RecordTransaction(bal, "admin_grant", "image", imageCredits, bal.ImageCreditsRemaining, null, null, $"Admin ({adminName}) granted {imageCredits} images"); }
        if (fileCredits != 0) { bal.FileCreditsRemaining   += fileCredits;  RecordTransaction(bal, "admin_grant", "file",  fileCredits,  bal.FileCreditsRemaining,  null, null, $"Admin ({adminName}) granted {fileCredits} files"); }
        bal.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetCreditsAsync(userId);
    }

    // ── GET TRANSACTION HISTORY ───────────────────────────────────────────────
    public async Task<List<CreditTransactionDto>> GetHistoryAsync(Guid userId, int page, int pageSize)
    {
        var bal = await _db.CreditBalances.FirstOrDefaultAsync(b => b.UserId == userId);
        if (bal == null) return new();
        return await _db.CreditTransactions
            .Where(t => t.CreditBalanceId == bal.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new CreditTransactionDto(t.Id, t.Type, t.CreditType, t.Units, t.BalanceAfter, t.Description, t.CreatedAt))
            .ToListAsync();
    }

    // ── GET ALL PRICING ──────────────────────────────────────────────────────
    public async Task<List<MessagePricingDto>> GetPricingAsync()
        => await _db.MessagePricings.OrderBy(p => p.MessageType)
            .Select(p => new MessagePricingDto(p.Id, p.MessageType, p.UnitType, p.PricePerUnit, p.UnitSize, p.Currency, p.Description, p.IsActive))
            .ToListAsync();

    // ── ADMIN: UPDATE PRICING ────────────────────────────────────────────────
    public async Task UpdatePricingAsync(Guid id, decimal pricePerUnit, int unitSize, string? description, bool isActive, string adminName)
    {
        var p = await _db.MessagePricings.FindAsync(id) ?? throw new KeyNotFoundException("Pricing not found.");
        p.PricePerUnit = pricePerUnit;
        p.UnitSize     = unitSize;
        p.Description  = description ?? p.Description;
        p.IsActive     = isActive;
        p.UpdatedAt    = DateTime.UtcNow;
        p.UpdatedBy    = adminName;
        await _db.SaveChangesAsync();
    }

    // ── IS CONSULTANT (free messaging) ───────────────────────────────────────
    public async Task<bool> IsConsultantAsync(Guid userId)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        return user?.Role?.RoleName == "Consultant Role" || user?.Role?.RoleName == "Admin Role";
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────
    private void RecordTransaction(CreditBalance bal, string type, string creditType, double units, double balanceAfter, Guid? messageId, Guid? paymentId, string desc)
    {
        _db.CreditTransactions.Add(new CreditTransaction
        {
            CreditBalanceId = bal.Id,
            Type            = type,
            CreditType      = creditType,
            Units           = units,
            BalanceAfter    = balanceAfter,
            MessageId       = messageId,
            PaymentId       = paymentId,
            Description     = desc,
        });
    }

    private static double GetRemainingForType(CreditBalance bal, string type) => type switch
    {
        "text"  => bal.TextCharsRemaining,
        "audio" => bal.AudioMinsRemaining,
        "video" => bal.VideoMinsRemaining,
        "image" => bal.ImageCreditsRemaining,
        "file"  => bal.FileCreditsRemaining,
        _       => 0
    };
}

// ── DTOs ──────────────────────────────────────────────────────────────────
public record CreditInfo(int TextCharsRemaining, double AudioMinsRemaining, double VideoMinsRemaining, int ImageCreditsRemaining, int FileCreditsRemaining);
public record ChargeResult(bool Success, string CreditType, double UnitsNeeded, double UnitsRemaining, string? Error);
public record CreditTransactionDto(Guid Id, string Type, string CreditType, double Units, double BalanceAfter, string? Description, DateTime CreatedAt);
public record MessagePricingDto(Guid Id, string MessageType, string UnitType, decimal PricePerUnit, int UnitSize, string Currency, string? Description, bool IsActive);
