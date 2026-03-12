using Microsoft.EntityFrameworkCore;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

namespace TenXConvo.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════
//  INVOICE SERVICE — Auto-generates invoice on every credit purchase
//  Client can list invoices + download as HTML (rendered as PDF by browser)
// ═══════════════════════════════════════════════════════════════════════════

public class InvoiceService
{
    private readonly AppDbContext _db;
    public InvoiceService(AppDbContext db) => _db = db;

    // ── AUTO-GENERATE INVOICE (called after payment confirmed) ───────────────
    public async Task<Invoice> GenerateInvoiceAsync(PaymentTransaction payment)
    {
        var user = await _db.Users.FindAsync(payment.UserId);

        // Get org info for invoice header
        var org = await _db.Organizations.FirstOrDefaultAsync();

        // Generate sequential invoice number: INV-YYYYMMDD-NNNN
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _db.Invoices.CountAsync(i => i.InvoiceNumber.StartsWith($"INV-{today}"));
        var invoiceNumber = $"INV-{today}-{(count + 1):D4}";

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            UserId = payment.UserId,
            PaymentId = payment.Id,
            TextCharsBought = payment.TextCharsBought,
            AudioMinsBought = payment.AudioMinsBought,
            VideoMinsBought = payment.VideoMinsBought,
            ImageCreditsBought = payment.ImageCreditsBought,
            FileCreditsBought = payment.FileCreditsBought,
            SubTotal = payment.Amount,
            Tax = 0,
            Total = payment.Amount,
            Currency = payment.Currency,
            Gateway = payment.Gateway,
            Status = "paid",
            BillingName = user?.UserName,
            BillingEmail = user?.Email,
            IssuedAt = DateTime.UtcNow,
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    // ── LIST INVOICES (for client history) ────────────────────────────────────
    public async Task<List<InvoiceDto>> GetUserInvoicesAsync(Guid userId, int page, int pageSize)
    {
        return await _db.Invoices
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.IssuedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InvoiceDto(
                i.Id, i.InvoiceNumber, i.Total, i.Currency, i.Gateway, i.Status,
                i.TextCharsBought, i.AudioMinsBought, i.VideoMinsBought,
                i.ImageCreditsBought, i.FileCreditsBought,
                i.ConsultantName, i.IssuedAt
            ))
            .ToListAsync();
    }

    public async Task<int> GetUserInvoiceCountAsync(Guid userId)
        => await _db.Invoices.CountAsync(i => i.UserId == userId);

    // ── GET SINGLE INVOICE ───────────────────────────────────────────────────
    public async Task<Invoice?> GetInvoiceAsync(Guid invoiceId, Guid userId)
        => await _db.Invoices
            .Include(i => i.User)
            .Include(i => i.Payment)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.UserId == userId);

    // ── GENERATE INVOICE HTML (for PDF download via browser print) ───────────
    public async Task<string> GenerateInvoiceHtmlAsync(Guid invoiceId, Guid userId)
    {
        var inv = await GetInvoiceAsync(invoiceId, userId)
            ?? throw new KeyNotFoundException("Invoice not found.");

        var org = await _db.Organizations.FirstOrDefaultAsync();

        var rows = new List<string>();
        if (inv.TextCharsBought > 0) rows.Add(Row("Text Credits", $"{inv.TextCharsBought:N0} characters", inv.TextCharsBought, inv.Currency));
        if (inv.AudioMinsBought > 0) rows.Add(Row("Audio Credits", $"{inv.AudioMinsBought:F1} minutes", inv.AudioMinsBought, inv.Currency));
        if (inv.VideoMinsBought > 0) rows.Add(Row("Video Credits", $"{inv.VideoMinsBought:F1} minutes", inv.VideoMinsBought, inv.Currency));
        if (inv.ImageCreditsBought > 0) rows.Add(Row("Image Credits", $"{inv.ImageCreditsBought} images", inv.ImageCreditsBought, inv.Currency));
        if (inv.FileCreditsBought > 0) rows.Add(Row("File Credits", $"{inv.FileCreditsBought} files", inv.FileCreditsBought, inv.Currency));

        var html = $$$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<title>Invoice {inv.InvoiceNumber}</title>
<style>
  * {{ margin: 0; padding: 0; box-sizing: border-box; }}
  body {{ font-family: -apple-system, 'Segoe UI', sans-serif; color: #1a1a2e; background: #fff; padding: 40px; max-width: 800px; margin: 0 auto; }}
  .header {{ display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 40px; padding-bottom: 20px; border-bottom: 2px solid #e2e8f0; }}
  .company {{ font-size: 22px; font-weight: 800; color: #0ea5e9; }}
  .company-sub {{ font-size: 12px; color: #64748b; margin-top: 4px; }}
  .invoice-title {{ font-size: 28px; font-weight: 800; text-align: right; }}
  .invoice-num {{ font-size: 14px; color: #64748b; text-align: right; }}
  .meta {{ display: flex; justify-content: space-between; margin-bottom: 30px; }}
  .meta-block {{ font-size: 13px; }}
  .meta-label {{ font-weight: 700; color: #64748b; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px; }}
  table {{ width: 100%; border-collapse: collapse; margin-bottom: 24px; }}
  th {{ background: #f8fafc; text-align: left; padding: 10px 14px; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; color: #64748b; border-bottom: 2px solid #e2e8f0; }}
  td {{ padding: 12px 14px; border-bottom: 1px solid #f1f5f9; font-size: 14px; }}
  .total-row td {{ font-weight: 800; font-size: 16px; border-top: 2px solid #1a1a2e; border-bottom: none; }}
  .amount {{ text-align: right; }}
  .badge {{ display: inline-block; padding: 3px 10px; border-radius: 20px; font-size: 11px; font-weight: 700; }}
  .badge-paid {{ background: #dcfce7; color: #16a34a; }}
  .badge-gateway {{ background: #dbeafe; color: #2563eb; }}
  .footer {{ margin-top: 40px; padding-top: 20px; border-top: 1px solid #e2e8f0; font-size: 12px; color: #94a3b8; text-align: center; }}
  @media print {{ body {{ padding: 20px; }} .no-print {{ display: none; }} }}
</style>
</head>
<body>
  <div class="header">
    <div>
      <div class="company">{org?.ClientName ?? "10X Convo"}</div>
      <div class="company-sub">{org?.Email ?? ""}</div>
      <div class="company-sub">{org?.NTN ?? ""}</div>
    </div>
    <div>
      <div class="invoice-title">INVOICE</div>
      <div class="invoice-num">{inv.InvoiceNumber}</div>
    </div>
  </div>

  <div class="meta">
    <div class="meta-block">
      <div class="meta-label">Billed To</div>
      <div><strong>{inv.BillingName}</strong></div>
      <div>{inv.BillingEmail}</div>
      {(inv.BillingAddress != null ? $"<div>{inv.BillingAddress}</div>" : "")}
    </div>
    <div class="meta-block" style="text-align: right;">
      <div class="meta-label">Invoice Date</div>
      <div>{inv.IssuedAt:MMMM dd, yyyy}</div>
      <div style="margin-top: 8px;">
        <span class="badge badge-paid">{inv.Status.ToUpper()}</span>
        <span class="badge badge-gateway">{inv.Gateway.ToUpper()}</span>
      </div>
    </div>
  </div>

  <table>
    <thead><tr><th>Item</th><th>Description</th><th class="amount">Amount</th></tr></thead>
    <tbody>
      {string.Join("\n      ", rows)}
      <tr class="total-row">
        <td colspan="2">Total</td>
        <td class="amount">{inv.Currency} {inv.Total:F2}</td>
      </tr>
    </tbody>
  </table>

  {(inv.ConsultantName != null ? $"<p style=\"font-size:13px;color:#64748b\">Consultant: <strong>{inv.ConsultantName}</strong></p>" : "")}

  <div class="footer">
    Thank you for your purchase! — {org?.ClientName ?? "10X Convo"}<br/>
    {org?.Website ?? ""}
  </div>

  <div class="no-print" style="text-align:center;margin-top:30px">
    <button onclick="window.print()" style="padding:10px 30px;background:#0ea5e9;color:#fff;border:none;border-radius:8px;cursor:pointer;font-weight:600;font-size:14px">
      Download PDF (Print)
    </button>
  </div>
</body>
</html>
""";
        return html;
    }

    private static string Row(string item, string desc, double qty, string currency)
    {
        return $"<tr><td>{item}</td><td>{desc}</td><td class=\"amount\">{currency} —</td></tr>";
    }
}

public record InvoiceDto(
    Guid Id, string InvoiceNumber, decimal Total, string Currency,
    string Gateway, string Status,
    int TextCharsBought, double AudioMinsBought, double VideoMinsBought,
    int ImageCreditsBought, int FileCreditsBought,
    string? ConsultantName, DateTime IssuedAt
);
