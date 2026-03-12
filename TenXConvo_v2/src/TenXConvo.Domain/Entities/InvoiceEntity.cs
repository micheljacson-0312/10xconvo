namespace TenXConvo.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════════
//  INVOICE — Auto-generated on every credit purchase
//  Client can view history + download PDF from the user portal
// ═══════════════════════════════════════════════════════════════════════════

public class Invoice
{
    public Guid     Id              { get; set; } = Guid.NewGuid();
    public string   InvoiceNumber   { get; set; } = string.Empty;   // INV-20260307-0001
    public Guid     UserId          { get; set; }                   // client who purchased
    public Guid     PaymentId       { get; set; }                   // linked payment

    // Purchased items breakdown
    public int      TextCharsBought { get; set; } = 0;
    public double   AudioMinsBought { get; set; } = 0;
    public double   VideoMinsBought { get; set; } = 0;
    public int      ImageCreditsBought { get; set; } = 0;
    public int      FileCreditsBought  { get; set; } = 0;

    // Money
    public decimal  SubTotal        { get; set; }
    public decimal  Tax             { get; set; } = 0;
    public decimal  Total           { get; set; }
    public string   Currency        { get; set; } = "USD";

    // Payment info
    public string   Gateway         { get; set; } = string.Empty;   // "stripe" | "jazzcash" | "easypaisa"
    public string   Status          { get; set; } = "paid";         // paid | refunded

    // Billing details (snapshot at time of purchase)
    public string?  BillingName     { get; set; }
    public string?  BillingEmail    { get; set; }
    public string?  BillingAddress  { get; set; }

    // For which consultant (if applicable)
    public Guid?    ConsultantUserId { get; set; }
    public string?  ConsultantName  { get; set; }

    public DateTime IssuedAt        { get; set; } = DateTime.UtcNow;

    // Navigation
    public AppUser  User            { get; set; } = null!;
    public PaymentTransaction Payment { get; set; } = null!;
}
