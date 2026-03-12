using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenXConvo.Infrastructure.Data;
using TenXConvo.Infrastructure.Services;

namespace TenXConvo.API.Controllers.Admin;

// ═══════════════════════════════════════════════════════════════════════════
//  ADMIN — INVOICES & PURCHASE HISTORY
//  All purchases across all users, filterable, with invoice download
// ═══════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/admin/invoices")]
[Authorize(Policy = "AdminOnly")]
public class AdminInvoiceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly InvoiceService _invoices;

    public AdminInvoiceController(AppDbContext db, InvoiceService invoices)
    { _db = db; _invoices = invoices; }

    /// <summary>List all invoices (all users) with search + pagination</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var q = _db.Invoices
            .Include(i => i.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(i => i.InvoiceNumber.Contains(search)
                          || i.User.UserName.Contains(search)
                          || i.User.Email.Contains(search)
                          || (i.ConsultantName != null && i.ConsultantName.Contains(search)));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(i => i.IssuedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new AdminInvoiceDto(
                i.Id, i.InvoiceNumber,
                i.UserId, i.User.UserName, i.User.Email,
                i.Total, i.Currency, i.Gateway, i.Status,
                i.TextCharsBought, i.AudioMinsBought, i.VideoMinsBought,
                i.ImageCreditsBought, i.FileCreditsBought,
                i.ConsultantName, i.IssuedAt
            ))
            .ToListAsync();

        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }

    /// <summary>Get invoice stats / summary</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var totalRevenue  = await _db.Invoices.Where(i => i.Status == "paid").SumAsync(i => i.Total);
        var totalInvoices = await _db.Invoices.CountAsync();
        var thisMonth     = await _db.Invoices.Where(i => i.IssuedAt.Month == DateTime.UtcNow.Month && i.IssuedAt.Year == DateTime.UtcNow.Year && i.Status == "paid").SumAsync(i => i.Total);
        var uniqueBuyers  = await _db.Invoices.Select(i => i.UserId).Distinct().CountAsync();

        return Ok(new { success = true, data = new { totalRevenue, totalInvoices, thisMonth, uniqueBuyers } });
    }

    /// <summary>Download any invoice as HTML (admin can view any user's invoice)</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice == null) return NotFound(new { success = false, message = "Invoice not found." });

        try
        {
            var html = await _invoices.GenerateInvoiceHtmlAsync(id, invoice.UserId);
            return Content(html, "text/html");
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
    }

    /// <summary>List all purchase transactions (payments — pending, completed, failed)</summary>
    [HttpGet("purchases")]
    public async Task<IActionResult> Purchases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var q = _db.PaymentTransactions
            .Include(p => p.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.TransactionRef.Contains(search)
                          || p.User.UserName.Contains(search)
                          || p.User.Email.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new AdminPurchaseDto(
                p.Id, p.TransactionRef,
                p.UserId, p.User.UserName, p.User.Email,
                p.Amount, p.Currency, p.Gateway, p.Status,
                p.TextCharsBought, p.AudioMinsBought, p.VideoMinsBought,
                p.ImageCreditsBought, p.FileCreditsBought,
                p.GatewayTxnId, p.CreatedAt, p.CompletedAt
            ))
            .ToListAsync();

        return Ok(new { success = true, data = new { items, totalRecords = total, page, pageSize } });
    }
}

public record AdminInvoiceDto(
    Guid Id, string InvoiceNumber,
    Guid UserId, string UserName, string UserEmail,
    decimal Total, string Currency, string Gateway, string Status,
    int TextCharsBought, double AudioMinsBought, double VideoMinsBought,
    int ImageCreditsBought, int FileCreditsBought,
    string? ConsultantName, DateTime IssuedAt
);

public record AdminPurchaseDto(
    Guid Id, string TransactionRef,
    Guid UserId, string UserName, string UserEmail,
    decimal Amount, string Currency, string Gateway, string Status,
    int TextCharsBought, double AudioMinsBought, double VideoMinsBought,
    int ImageCreditsBought, int FileCreditsBought,
    string? GatewayTxnId, DateTime CreatedAt, DateTime? CompletedAt
);
