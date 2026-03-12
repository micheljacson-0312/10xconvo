namespace TenXConvo.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════════
//  CONSULTANT SERVICE CONFIGURATION
//  Admin sets these per consultant from the Settings ⚙️ icon
//
//  Tab 1 — Services: which message types this consultant accepts
//  Tab 2 — Pricing:  override global pricing per consultant (optional)
//  Tab 3 — Gateway:  which payment gateways are enabled for this consultant
// ═══════════════════════════════════════════════════════════════════════════

// ─── Per-consultant service toggles + pricing overrides ───────────────────
public class ConsultantServiceConfig
{
    public Guid     Id              { get; set; } = Guid.NewGuid();
    public Guid     ConsultantUserId { get; set; }

    // ── Tab 1: Services Enabled ──
    public bool     TextEnabled     { get; set; } = true;
    public bool     AudioEnabled    { get; set; } = true;
    public bool     VideoEnabled    { get; set; } = false;
    public bool     ImageEnabled    { get; set; } = true;
    public bool     FileEnabled     { get; set; } = true;

    // ── Tab 2: Pricing Overrides (null = use global default) ──
    public decimal? TextRate        { get; set; }   // null = use global MessagePricing rate
    public decimal? AudioRate       { get; set; }   // null = use global
    public decimal? VideoRate       { get; set; }   // null = use global
    public decimal? ImageRate       { get; set; }   // null = use global
    public decimal? FileRate        { get; set; }   // null = use global
    public string   Currency        { get; set; } = "USD";

    // ── Tab 3: Payment Gateway Enablement ──
    public bool     StripeEnabled   { get; set; } = true;
    public bool     JazzCashEnabled { get; set; } = true;
    public bool     EasyPaisaEnabled { get; set; } = true;

    // Optional: consultant's own gateway credentials (for direct payouts)
    public string?  StripeAccountId { get; set; }    // Stripe Connect account ID
    public string?  JazzCashAccount { get; set; }    // JazzCash mobile number
    public string?  EasyPaisaAccount { get; set; }   // EasyPaisa mobile number
    public string?  BankAccountNo   { get; set; }    // bank account for manual payouts
    public string?  BankName        { get; set; }

    public DateTime UpdatedAt       { get; set; } = DateTime.UtcNow;
    public string?  UpdatedBy       { get; set; }

    // Navigation
    public AppUser  Consultant      { get; set; } = null!;
}
