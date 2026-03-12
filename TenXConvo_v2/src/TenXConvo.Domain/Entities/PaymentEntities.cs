namespace TenXConvo.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════════
//  DIRECT PURCHASE CREDIT SYSTEM (no wallet — buy units directly)
//
//  How it works:
//  1. Client purchases credits: "5000 text chars + 10 audio min"
//  2. CreditBalance tracks remaining: TextCharsRemaining, AudioMinsRemaining, etc.
//  3. When client sends a message:
//     - Text:  deduct char count from TextCharsRemaining
//     - Audio: deduct duration from AudioMinsRemaining
//     - Video: deduct duration from VideoMinsRemaining
//     - Image: deduct 1 from ImageCreditsRemaining
//     - File:  deduct 1 from FileCreditsRemaining
//  4. Frontend shows: "2,340 chars | 7.5 min audio | 3 images"
//  5. Balance hits 0 → "Buy more" prompt
//  6. Consultant messages are FREE
// ═══════════════════════════════════════════════════════════════════════════

public class CreditBalance
{
    public Guid     Id                    { get; set; } = Guid.NewGuid();
    public Guid     UserId                { get; set; }
    public int      TextCharsRemaining    { get; set; } = 0;
    public double   AudioMinsRemaining    { get; set; } = 0;
    public double   VideoMinsRemaining    { get; set; } = 0;
    public int      ImageCreditsRemaining { get; set; } = 0;
    public int      FileCreditsRemaining  { get; set; } = 0;
    public DateTime UpdatedAt             { get; set; } = DateTime.UtcNow;
    public AppUser  User                  { get; set; } = null!;
    public ICollection<CreditTransaction> Transactions { get; set; } = new List<CreditTransaction>();
}

public class CreditTransaction
{
    public Guid     Id              { get; set; } = Guid.NewGuid();
    public Guid     CreditBalanceId { get; set; }
    public string   Type            { get; set; } = string.Empty;  // "purchase" | "message_charge" | "admin_grant" | "refund"
    public string   CreditType      { get; set; } = string.Empty;  // "text" | "audio" | "video" | "image" | "file"
    public double   Units           { get; set; }                  // +5000 chars or -320 chars
    public double   BalanceAfter    { get; set; }
    public Guid?    MessageId       { get; set; }
    public Guid?    PaymentId       { get; set; }
    public string?  Description     { get; set; }
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
    public CreditBalance Balance    { get; set; } = null!;
    public PaymentTransaction? Payment { get; set; }
}

public class PaymentTransaction
{
    public Guid     Id              { get; set; } = Guid.NewGuid();
    public string   TransactionRef  { get; set; } = string.Empty;
    public Guid     UserId          { get; set; }
    public int      TextCharsBought { get; set; } = 0;
    public double   AudioMinsBought { get; set; } = 0;
    public double   VideoMinsBought { get; set; } = 0;
    public int      ImageCreditsBought { get; set; } = 0;
    public int      FileCreditsBought  { get; set; } = 0;
    public decimal  Amount          { get; set; }
    public string   Currency        { get; set; } = "USD";
    public string   Gateway         { get; set; } = string.Empty;
    public string?  GatewayTxnId    { get; set; }
    public string?  GatewayResponse { get; set; }
    public string   Status          { get; set; } = "pending";
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt    { get; set; }
    public string?  FailureReason   { get; set; }
    public string?  IpAddress       { get; set; }
    public AppUser  User            { get; set; } = null!;
    public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
}

public class MessagePricing
{
    public Guid     Id              { get; set; } = Guid.NewGuid();
    public string   MessageType     { get; set; } = string.Empty;   // "text" | "audio" | "video" | "image" | "file"
    public string   UnitType        { get; set; } = string.Empty;   // "characters" | "minutes" | "count"
    public decimal  PricePerUnit    { get; set; }                   // $ per unitSize
    public int      UnitSize        { get; set; } = 1;              // 250 for text, 1 for audio/video/image/file
    public string   Currency        { get; set; } = "USD";
    public string?  Description     { get; set; }
    public bool     IsActive        { get; set; } = true;
    public DateTime UpdatedAt       { get; set; } = DateTime.UtcNow;
    public string?  UpdatedBy       { get; set; }
}
