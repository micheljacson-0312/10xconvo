using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TenXConvo.Infrastructure.Services;

namespace TenXConvo.API.Controllers;

// ═══════════════════════════════════════════════════════════════════════════
//  CREDITS — Client buys chars/minutes, uses them when messaging
// ═══════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/credits")]
[Authorize]
public class CreditsController : ControllerBase
{
    private readonly CreditService _credits;

    public CreditsController(CreditService credits)
    {
        _credits = credits;
    }

    /// <summary>
    /// Extract logged-in user ID safely from JWT
    /// </summary>
    private Guid UserId
    {
        get
        {
            var id =
                User?.FindFirst("sub")?.Value ??
                User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User?.FindFirst("userId")?.Value;

            if (string.IsNullOrWhiteSpace(id))
                throw new UnauthorizedAccessException("UserId claim missing in token.");

            if (!Guid.TryParse(id, out var userId))
                throw new UnauthorizedAccessException("Invalid userId claim.");

            return userId;
        }
    }

    /// <summary>
    /// Get my remaining credits
    /// </summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var credits = await _credits.GetCreditsAsync(UserId);

        return Ok(new
        {
            success = true,
            data = credits
        });
    }

/// <summary>
/// Get my purchase + usage history
/// </summary>
[HttpGet("history")]
public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
{
    var history = await _credits.GetHistoryAsync(UserId, page, pageSize);

    return Ok(new
    {
        success = true,
        data = history
    });
}

/// <summary>
/// Public pricing
/// </summary>
[HttpGet("pricing")]
[AllowAnonymous]
public async Task<IActionResult> GetPricing()
{
    var pricing = await _credits.GetPricingAsync();

    return Ok(new
    {
        success = true,
        data = pricing
    });
}

/// <summary>
/// Purchase credits via Stripe Checkout.
    /// Creates a Stripe Checkout Session → returns URL → client redirects to Stripe → pays → Stripe calls webhook → credits added.
    ///
    /// SETUP:
    ///   1. Set Stripe:SecretKey and Stripe:PublishableKey in appsettings.json
    ///   2. Set Stripe:WebhookSecret (from Stripe Dashboard → Webhooks)
    ///   3. Set Stripe:SuccessUrl and Stripe:CancelUrl to your frontend billing page
    ///   4. Webhook URL to register in Stripe: https://api.yourdomain.com/api/credits/callback/stripe
    /// </summary>
    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] PurchaseRequest req)
    {
        if (req.TextChars <= 0 && req.AudioMins <= 0 && req.VideoMins <= 0 && req.ImageCredits <= 0 && req.FileCredits <= 0)
            return BadRequest(new { success = false, message = "Select at least one credit type to purchase." });

        var db     = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        // Calculate total price from pricing table
        var pricing = await _credits.GetPricingAsync();
        decimal totalPrice = 0;
        string currency = config["Stripe:Currency"] ?? "usd";
        var breakdown = new List<string>();
        var lineItems = new List<Stripe.Checkout.SessionLineItemOptions>();

        foreach (var p in pricing)
        {
            var (units, label) = p.MessageType switch
            {
                "text"  => ((decimal)req.TextChars,    $"{req.TextChars:N0} text chars"),
                "audio" => ((decimal)req.AudioMins,     $"{req.AudioMins:F1} audio min"),
                "video" => ((decimal)req.VideoMins,     $"{req.VideoMins:F1} video min"),
                "image" => ((decimal)req.ImageCredits, $"{req.ImageCredits} images"),
                "file"  => ((decimal)req.FileCredits,  $"{req.FileCredits} files"),
                _       => (0m, "")
            };
            if (units > 0)
            {
                var requiredUnits = decimal.Ceiling(units / p.UnitSize);
                var unitCost = requiredUnits * p.PricePerUnit;

                totalPrice += unitCost;
                breakdown.Add($"{label} = {unitCost:F2} {currency.ToUpper()}");

                // Stripe line item (amount in cents)
                lineItems.Add(new Stripe.Checkout.SessionLineItemOptions
                {
                    PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                    {
                        Currency   = currency,
                        UnitAmount = (long)(unitCost * 100), // Stripe uses cents
                        ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                        {
                            Name        = $"{p.MessageType.ToUpper()} Credits",
                            Description = label,
                        }
                    },
                    Quantity = 1,
                });
            }
        }

        if (totalPrice <= 0 || lineItems.Count == 0)
            return BadRequest(new { success = false, message = "Nothing to purchase." });

        // Create pending payment record in our DB
        var txnRef = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        var payment = new TenXConvo.Domain.Entities.PaymentTransaction
        {
            UserId             = UserId,
            TransactionRef     = txnRef,
            TextCharsBought    = req.TextChars,
            AudioMinsBought    = req.AudioMins,
            VideoMinsBought    = req.VideoMins,
            ImageCreditsBought = req.ImageCredits,
            FileCreditsBought  = req.FileCredits,
            Amount             = totalPrice,
            Currency           = currency.ToUpper(),
            Gateway            = "stripe",
            Status             = "pending",
            IpAddress          = HttpContext.Connection.RemoteIpAddress?.ToString(),
        };
        db.PaymentTransactions.Add(payment);
        await db.SaveChangesAsync();

        // ── CREATE STRIPE CHECKOUT SESSION ────────────────────────────────────
        Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"];

        var sessionOptions = new Stripe.Checkout.SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Mode               = "payment",
            SuccessUrl         = $"{config["PortalUrls:User"]}/billing?payment=success&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl          = $"{config["PortalUrls:User"]}/billing?payment=cancelled",
            ClientReferenceId  = payment.Id.ToString(), // our payment ID — used in webhook
            CustomerEmail      = User.FindFirst("email")?.Value,
            LineItems          = lineItems,
            Metadata = new Dictionary<string, string>
            {
                ["paymentId"]      = payment.Id.ToString(),
                ["transactionRef"] = txnRef,
                ["userId"]         = UserId.ToString(),
            },
            // Pass metadata to the PaymentIntent too — needed for payment_intent.payment_failed event
            PaymentIntentData = new Stripe.Checkout.SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["paymentId"]      = payment.Id.ToString(),
                    ["transactionRef"] = txnRef,
                    ["userId"]         = UserId.ToString(),
                }
            }
        };

        var sessionService = new Stripe.Checkout.SessionService();
        var session = await sessionService.CreateAsync(sessionOptions);

        // Save Stripe session ID to our payment record
        payment.GatewayTxnId = session.Id;
        await db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                paymentId       = payment.Id,
                transactionRef  = txnRef,
                totalPrice      = totalPrice,
                currency        = currency.ToUpper(),
                breakdown       = string.Join(" + ", breakdown),
                stripeSessionId = session.Id,
                checkoutUrl     = session.Url,  // ← REDIRECT CLIENT TO THIS URL
                message         = "Redirecting to Stripe Checkout..."
            }
        });
    }

    /// <summary>
    /// Stripe Webhook — handles both success and failure on ONE URL.
    /// 
    /// REGISTER THIS URL IN STRIPE DASHBOARD:
    ///   https://api.yourdomain.com/api/credits/callback/stripe
    ///
    /// ADD THESE 2 EVENTS:
    ///   ✅ checkout.session.completed      → payment succeeded → credits added + invoice generated
    ///   ❌ payment_intent.payment_failed   → payment failed → status marked failed + reason logged
    /// </summary>
    [HttpPost("callback/stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var db     = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CreditsController>>();

        // Read raw body for signature verification
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        Stripe.Event stripeEvent;
        try
        {
            stripeEvent = Stripe.EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                config["Stripe:WebhookSecret"]
            );
        }
        catch (Stripe.StripeException ex)
        {
            logger.LogWarning("Stripe webhook signature failed: {Error}", ex.Message);
            return BadRequest(new { error = $"Webhook signature verification failed: {ex.Message}" });
        }

        logger.LogInformation("Stripe webhook received: {EventType} | {EventId}", stripeEvent.Type, stripeEvent.Id);

        // ══════════════════════════════════════════════════════════════════════
        //  EVENT 1: checkout.session.completed → PAYMENT SUCCEEDED
        // ══════════════════════════════════════════════════════════════════════
        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session == null) return BadRequest(new { error = "Invalid session data." });

            // Find our payment by Stripe session ID
            var payment = await db.PaymentTransactions
                .FirstOrDefaultAsync(p => p.GatewayTxnId == session.Id && p.Status == "pending");

            if (payment != null)
            {
                // ✅ Mark payment as completed
                payment.Status          = "completed";
                payment.CompletedAt     = DateTime.UtcNow;
                payment.GatewayResponse = json.Length > 2000 ? json[..2000] : json;
                await db.SaveChangesAsync();

                // ✅ Add purchased credits to user's balance
                await _credits.AddCreditsAsync(
                    payment.UserId, payment.Id,
                    payment.TextCharsBought, payment.AudioMinsBought, payment.VideoMinsBought,
                    payment.ImageCreditsBought, payment.FileCreditsBought);

                // ✅ Auto-generate invoice
                var invoiceService = HttpContext.RequestServices.GetRequiredService<InvoiceService>();
                await invoiceService.GenerateInvoiceAsync(payment);

                logger.LogInformation("✅ Payment {TxnRef} completed — credits added for user {UserId}",
                    payment.TransactionRef, payment.UserId);
            }
            else
            {
                logger.LogWarning("Stripe session {SessionId} — no matching pending payment found", session.Id);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  EVENT 2: payment_intent.payment_failed → PAYMENT FAILED
        // ══════════════════════════════════════════════════════════════════════
        else if (stripeEvent.Type == "payment_intent.payment_failed")
        {
            var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
            if (paymentIntent == null) return BadRequest(new { error = "Invalid payment intent data." });

            // Extract failure details
            var failureMessage = paymentIntent.LastPaymentError?.Message ?? "Payment failed";
            var failureCode    = paymentIntent.LastPaymentError?.Code ?? "unknown";
            var declineCode    = paymentIntent.LastPaymentError?.DeclineCode;

            // Try to find the payment by metadata or by matching payment intent
            // Stripe Checkout Session creates a PaymentIntent — we stored session ID as GatewayTxnId
            // We need to find the session that created this payment intent
            Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"];

            // Search for our payment — the PaymentIntent's metadata may have our paymentId
            TenXConvo.Domain.Entities.PaymentTransaction? payment = null;

            if (paymentIntent.Metadata?.ContainsKey("paymentId") == true)
            {
                var paymentIdStr = paymentIntent.Metadata["paymentId"];
                if (Guid.TryParse(paymentIdStr, out var paymentId))
                    payment = await db.PaymentTransactions.FirstOrDefaultAsync(p => p.Id == paymentId && p.Status == "pending");
            }

            // Fallback: search by recent pending payments for this amount
            if (payment == null && paymentIntent.Amount > 0)
            {
                var amount = paymentIntent.Amount / 100m; // cents to dollars
                payment = await db.PaymentTransactions
                    .Where(p => p.Status == "pending" && p.Amount == amount && p.Gateway == "stripe")
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            if (payment != null)
            {
                // ❌ Mark payment as failed
                payment.Status        = "failed";
                payment.FailureReason = declineCode != null
                    ? $"{failureMessage} (code: {failureCode}, decline: {declineCode})"
                    : $"{failureMessage} (code: {failureCode})";
                payment.GatewayResponse = json.Length > 2000 ? json[..2000] : json;
                await db.SaveChangesAsync();

                logger.LogWarning("❌ Payment {TxnRef} failed: {Reason}",
                    payment.TransactionRef, payment.FailureReason);
            }
            else
            {
                logger.LogWarning("Stripe payment_intent.payment_failed — no matching payment found. Intent: {IntentId}, Error: {Error}",
                    paymentIntent.Id, failureMessage);
            }
        }

        // Any other event — just acknowledge
        else
        {
            logger.LogInformation("Stripe event {EventType} received but not handled", stripeEvent.Type);
        }

        // Stripe expects 200 OK to acknowledge receipt
        return Ok();
    }

    /// <summary>
    /// Verify payment status after redirect (frontend calls this after returning from Stripe).
    /// </summary>
    [HttpGet("verify/{paymentId:guid}")]
    public async Task<IActionResult> VerifyPayment(Guid paymentId)
    {
        var db = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var payment = await db.PaymentTransactions.FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == UserId);
        if (payment == null) return NotFound(new { success = false, message = "Payment not found." });

        return Ok(new
        {
            success = true,
            data = new
            {
                paymentId      = payment.Id,
                transactionRef = payment.TransactionRef,
                status         = payment.Status,
                amount         = payment.Amount,
                currency       = payment.Currency,
                completedAt    = payment.CompletedAt,
            }
        });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PAYFAST (Pakistan) — Purchase + Callback
    //
    //  Webhook URL (register ONE URL for success + failure):
    //    https://api.yourdomain.com/api/credits/payfast/callback
    //
    //  PayFast redirects customer to SUCCESS_URL or FAILURE_URL with:
    //    err_code=000 → success, anything else → failure
    //    validation_hash = sha256(basket_id|secured_key|merchant_id|err_code)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Purchase credits via PayFast (Pakistan).
    /// Returns an HTML page that auto-redirects to PayFast hosted payment page.
    /// </summary>
    [HttpPost("purchase/payfast")]
    public async Task<IActionResult> PurchasePayFast([FromBody] PurchaseRequest req)
    {
        if (req.TextChars <= 0 && req.AudioMins <= 0 && req.VideoMins <= 0 && req.ImageCredits <= 0 && req.FileCredits <= 0)
            return BadRequest(new { success = false, message = "Select at least one credit type." });

        var db      = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var config  = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var payfast = HttpContext.RequestServices.GetRequiredService<PayFastService>();

        // Calculate total price
        var pricing = await _credits.GetPricingAsync();
        decimal totalPrice = 0;
        string currency = payfast.Currency;
        var breakdown = new List<string>();
        foreach (var p in pricing)
        {
            var (units, label) = p.MessageType switch
            {
                "text"  => ((decimal)req.TextChars,    $"{req.TextChars:N0} text chars"),
                "audio" => ((decimal)req.AudioMins,     $"{req.AudioMins:F1} audio min"),
                "video" => ((decimal)req.VideoMins,     $"{req.VideoMins:F1} video min"),
                "image" => ((decimal)req.ImageCredits, $"{req.ImageCredits} images"),
                "file"  => ((decimal)req.FileCredits,  $"{req.FileCredits} files"),
                _       => (0m, "")
            };
            if (units > 0)
            {
                var requiredUnits = decimal.Ceiling(units / p.UnitSize);
                var unitCost = requiredUnits * p.PricePerUnit;

                totalPrice += unitCost;
                breakdown.Add($"{label} = {unitCost:F2} {currency.ToUpper()}");
            }
        }
        if (totalPrice <= 0) return BadRequest(new { success = false, message = "Nothing to purchase." });

        // Create pending payment
        var txnRef = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        var payment = new TenXConvo.Domain.Entities.PaymentTransaction
        {
            UserId             = UserId,
            TransactionRef     = txnRef,
            TextCharsBought    = req.TextChars,
            AudioMinsBought    = req.AudioMins,
            VideoMinsBought    = req.VideoMins,
            ImageCreditsBought = req.ImageCredits,
            FileCreditsBought  = req.FileCredits,
            Amount             = totalPrice,
            Currency           = payfast.Currency,
            Gateway            = "payfast",
            Status             = "pending",
            IpAddress          = HttpContext.Connection.RemoteIpAddress?.ToString(),
        };
        db.PaymentTransactions.Add(payment);
        await db.SaveChangesAsync();

        // Get user info
        var user = await db.Users.FindAsync(UserId);
        var apiUrl = config["PortalUrls:Api"] ?? $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        var baseCallbackUrl = $"{apiUrl}/api/credits/payfast/callback";
        var successUrl = $"{baseCallbackUrl}?redirect=Y";
        var failureUrl = $"{baseCallbackUrl}?redirect=Y";

        var result = await payfast.GeneratePaymentFormAsync(
            payment.Id, txnRef, totalPrice,
            user?.Email ?? "", user?.CellNo ?? "",
            $"Credits: {string.Join(" + ", breakdown)}",
            successUrl, failureUrl, baseCallbackUrl);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Error });

        // Return the auto-submit HTML or a redirect URL
        return Ok(new
        {
            success = true,
            data = new
            {
                paymentId      = payment.Id,
                transactionRef = txnRef,
                totalPrice,
                currency       = payfast.Currency,
                breakdown      = string.Join(" + ", breakdown),
                gateway        = "payfast",
                paymentFormHtml = result.Html,  // Frontend opens this in new window/iframe
                message        = "Redirecting to PayFast..."
            }
        });
    }

    /// <summary>
    /// PayFast Callback — ONE URL handles both success + failure.
    /// PayFast sends: err_code, transaction_id, basket_id, validation_hash, err_msg
    /// err_code=000 → success, anything else → failure
    /// </summary>
    [HttpGet("payfast/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PayFastCallback(
        [FromQuery] string? redirect,
        [FromQuery] string? order_id,
        [FromQuery] string? basket_id,
        [FromQuery] string? err_code,
        [FromQuery] string? err_msg,
        [FromQuery] string? transaction_id,
        [FromQuery] string? validation_hash,
        [FromQuery] string? PaymentName)
    {
        var db      = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var config  = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var payfast = HttpContext.RequestServices.GetRequiredService<PayFastService>();
        var logger  = HttpContext.RequestServices.GetRequiredService<ILogger<CreditsController>>();

        var frontendUrl = $"{config["PortalUrls:User"]}/billing";

        logger.LogInformation("PayFast callback: order_id={OrderId}, basket_id={BasketId}, err_code={ErrCode}, err_msg={ErrMsg}, txn_id={TxnId}",
            order_id, basket_id, err_code, err_msg, transaction_id);

        // Find payment
        TenXConvo.Domain.Entities.PaymentTransaction? payment = null;
        if (Guid.TryParse(order_id, out var paymentGuid))
            payment = await db.PaymentTransactions.FirstOrDefaultAsync(p => p.Id == paymentGuid);
        if (payment == null && !string.IsNullOrEmpty(basket_id))
            payment = await db.PaymentTransactions.FirstOrDefaultAsync(p => p.TransactionRef == basket_id);

        if (payment == null)
        {
            logger.LogWarning("PayFast callback: payment not found for order_id={OrderId}", order_id);
            if (redirect == "Y") return Redirect($"{frontendUrl}?payment=failed&error=payment_not_found");
            return BadRequest("Payment not found");
        }

        // Already processed?
        if (payment.Status != "pending")
        {
            if (redirect == "Y") return Redirect($"{frontendUrl}?payment={payment.Status}");
            return Ok("Already processed");
        }

        // Validate hash
        var hashValid = payfast.ValidateCallbackHash(
            validation_hash ?? "", basket_id ?? payment.TransactionRef, err_code ?? "");

        if (!hashValid)
        {
            logger.LogWarning("PayFast hash validation failed for {TxnRef}", payment.TransactionRef);
            if (redirect == "Y") return Redirect($"{frontendUrl}?payment=failed&error=hash_invalid");
            return BadRequest("Invalid hash");
        }

        // ── SUCCESS (err_code=000) ───────────────────────────────────────────
        if (err_code == "000")
        {
            payment.Status       = "completed";
            payment.CompletedAt  = DateTime.UtcNow;
            payment.GatewayTxnId = transaction_id;
            payment.GatewayResponse = $"err_code={err_code}&err_msg={err_msg}&transaction_id={transaction_id}&PaymentName={PaymentName}";
            await db.SaveChangesAsync();

            // Add credits
            await _credits.AddCreditsAsync(
                payment.UserId, payment.Id,
                payment.TextCharsBought, payment.AudioMinsBought, payment.VideoMinsBought,
                payment.ImageCreditsBought, payment.FileCreditsBought);

            // Generate invoice
            var invoiceService = HttpContext.RequestServices.GetRequiredService<InvoiceService>();
            await invoiceService.GenerateInvoiceAsync(payment);

            logger.LogInformation("✅ PayFast payment {TxnRef} completed via {Method}", payment.TransactionRef, PaymentName);

            if (redirect == "Y") return Redirect($"{frontendUrl}?payment=success");
            return Ok("Payment completed");
        }

        // ── FAILED ───────────────────────────────────────────────────────────
        payment.Status        = "failed";
        payment.FailureReason = $"{err_msg} (code: {err_code})";
        payment.GatewayTxnId  = transaction_id;
        payment.GatewayResponse = $"err_code={err_code}&err_msg={err_msg}&transaction_id={transaction_id}";
        await db.SaveChangesAsync();

        logger.LogWarning("❌ PayFast payment {TxnRef} failed: {Reason}", payment.TransactionRef, payment.FailureReason);

        if (redirect == "Y") return Redirect($"{frontendUrl}?payment=failed&error={Uri.EscapeDataString(err_msg ?? "Payment failed")}");
        return Ok("Payment failed");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  EASYPAISA (Pakistan) — Hosted Checkout + OpenAPI OTC
    //
    //  CALLBACK URL to configure on EasyPaisa merchant portal as postBackURL:
    //    https://api.yourdomain.com/api/credits/easypaisa/callback
    //
    //  EasyPaisa posts back: auth_token, orderRefNumber, status (0000=success)
    //  One URL handles success + failure. Redirects back to /billing on frontend.
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Purchase credits via EasyPaisa Hosted Checkout.
    /// Returns HTML that auto-POSTs to EasyPaisa payment page.
    /// Frontend opens this in a new window — customer pays — EasyPaisa POSTs callback.
    ///
    /// SETUP (appsettings.json — only 2 fields needed):
    ///   "EasyPaisa": {
    ///     "StoreId": "YOUR_STORE_ID",
    ///     "HashKey": "YOUR_HASH_KEY",
    ///     "Sandbox": true
    ///   }
    /// </summary>
    [HttpPost("purchase/easypaisa")]
    public async Task<IActionResult> PurchaseEasyPaisa([FromBody] PurchaseRequest req)
    {
        if (req.TextChars <= 0 && req.AudioMins <= 0 && req.VideoMins <= 0 && req.ImageCredits <= 0 && req.FileCredits <= 0)
            return BadRequest(new { success = false, message = "Select at least one credit type." });

        var db        = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var config    = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var easypaisa = HttpContext.RequestServices.GetRequiredService<EasyPaisaService>();

        // Calculate total price from pricing table
        var pricing = await _credits.GetPricingAsync();
        decimal totalPrice = 0;
        string currency = easypaisa.Currency;
        var breakdown = new List<string>();
        foreach (var p in pricing)
        {
            var (units, label) = p.MessageType switch
            {
                "text"  => ((decimal)req.TextChars,    $"{req.TextChars:N0} text chars"),
                "audio" => ((decimal)req.AudioMins,     $"{req.AudioMins:F1} audio min"),
                "video" => ((decimal)req.VideoMins,     $"{req.VideoMins:F1} video min"),
                "image" => ((decimal)req.ImageCredits, $"{req.ImageCredits} images"),
                "file"  => ((decimal)req.FileCredits,  $"{req.FileCredits} files"),
                _       => (0m, "")
            };
            if (units > 0)
            {
                var requiredUnits = decimal.Ceiling(units / p.UnitSize);
                var unitCost = requiredUnits * p.PricePerUnit;

                totalPrice += unitCost;
                breakdown.Add($"{label} = {unitCost:F2} {currency.ToUpper()}");
            }
        }
        if (totalPrice <= 0) return BadRequest(new { success = false, message = "Nothing to purchase." });

        // Create pending payment record
        var txnRef = $"EP-{DateTime.UtcNow:yyyyMMddHHmm}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var payment = new TenXConvo.Domain.Entities.PaymentTransaction
        {
            UserId             = UserId,
            TransactionRef     = txnRef,
            TextCharsBought    = req.TextChars,
            AudioMinsBought    = req.AudioMins,
            VideoMinsBought    = req.VideoMins,
            ImageCreditsBought = req.ImageCredits,
            FileCreditsBought  = req.FileCredits,
            Amount             = totalPrice,
            Currency           = easypaisa.Currency,
            Gateway            = "easypaisa",
            Status             = "pending",
            IpAddress          = HttpContext.Connection.RemoteIpAddress?.ToString(),
        };
        db.PaymentTransactions.Add(payment);
        await db.SaveChangesAsync();

        // postBackURL — EasyPaisa will POST result here after payment
        var apiUrl      = config["PortalUrls:Api"] ?? $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        var postBackUrl = $"{apiUrl}/api/credits/easypaisa/callback?paymentId={payment.Id}";

        // Generate hosted checkout HTML form
        var result = easypaisa.GenerateHostedCheckoutForm(
            orderRefNum: txnRef,
            amount:      totalPrice,
            postBackUrl: postBackUrl);

        if (!result.Success)
        {
            db.PaymentTransactions.Remove(payment);
            await db.SaveChangesAsync();
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                paymentId       = payment.Id,
                transactionRef  = txnRef,
                totalPrice,
                currency        = easypaisa.Currency,
                breakdown       = string.Join(" + ", breakdown),
                gateway         = "easypaisa",
                sandbox         = easypaisa.Sandbox,
                paymentFormHtml = result.Html,
                message         = "Redirecting to EasyPaisa..."
            }
        });
    }

    /// <summary>
    /// Initiate EasyPaisa OTC (Over-The-Counter) transaction via REST OpenAPI.
    /// Returns auth_token — customer shows this at any EasyPaisa shop or pays via mobile app.
    /// </summary>
    [HttpPost("purchase/easypaisa/otc")]
    public async Task<IActionResult> PurchaseEasyPaisaOtc([FromBody] EasyPaisaOtcRequest req)
    {
        if (req.TextChars <= 0 && req.AudioMins <= 0 && req.VideoMins <= 0 && req.ImageCredits <= 0 && req.FileCredits <= 0)
            return BadRequest(new { success = false, message = "Select at least one credit type." });

        var db        = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var easypaisa = HttpContext.RequestServices.GetRequiredService<EasyPaisaService>();

        var pricing = await _credits.GetPricingAsync();
        decimal totalPrice = 0;
        string currency = easypaisa.Currency;
        var breakdown = new List<string>();
        foreach (var p in pricing)
        {
            var (units, label) = p.MessageType switch
            {
                "text"  => ((decimal)req.TextChars,    $"{req.TextChars:N0} text chars"),
                "audio" => ((decimal)req.AudioMins,     $"{req.AudioMins:F1} audio min"),
                "video" => ((decimal)req.VideoMins,     $"{req.VideoMins:F1} video min"),
                "image" => ((decimal)req.ImageCredits, $"{req.ImageCredits} images"),
                "file"  => ((decimal)req.FileCredits,  $"{req.FileCredits} files"),
                _       => (0m, "")
            };
            if (units > 0)
            {
                var requiredUnits = decimal.Ceiling(units / p.UnitSize);
                var unitCost = requiredUnits * p.PricePerUnit;

                totalPrice += unitCost;
                breakdown.Add($"{label} = {unitCost:F2} {currency.ToUpper()}");
            }
        }
        if (totalPrice <= 0) return BadRequest(new { success = false, message = "Nothing to purchase." });

        var txnRef = $"EP-OTC-{DateTime.UtcNow:yyyyMMddHHmm}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var payment = new TenXConvo.Domain.Entities.PaymentTransaction
        {
            UserId             = UserId,
            TransactionRef     = txnRef,
            TextCharsBought    = req.TextChars,
            AudioMinsBought    = req.AudioMins,
            VideoMinsBought    = req.VideoMins,
            ImageCreditsBought = req.ImageCredits,
            FileCreditsBought  = req.FileCredits,
            Amount             = totalPrice,
            Currency           = easypaisa.Currency,
            Gateway            = "easypaisa_otc",
            Status             = "pending",
            IpAddress          = HttpContext.Connection.RemoteIpAddress?.ToString(),
        };
        db.PaymentTransactions.Add(payment);
        await db.SaveChangesAsync();

        var otcResult = await easypaisa.InitiateOtcTransactionAsync(
            orderRefNum:     txnRef,
            amount:          totalPrice,
            mobileAccountNo: req.MobileAccountNo);

        if (!otcResult.Success)
        {
            db.PaymentTransactions.Remove(payment);
            await db.SaveChangesAsync();
            return BadRequest(new { success = false, message = otcResult.Error });
        }

        payment.GatewayTxnId = otcResult.AuthToken;
        await db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                paymentId      = payment.Id,
                transactionRef = txnRef,
                authToken      = otcResult.AuthToken,
                totalPrice,
                currency       = easypaisa.Currency,
                breakdown      = string.Join(" + ", breakdown),
                gateway        = "easypaisa_otc",
                message        = $"OTC initiated. Auth token: {otcResult.AuthToken}. Pay at any EasyPaisa shop."
            }
        });
    }

    /// <summary>
    /// EasyPaisa Callback — receives POST from EasyPaisa after payment attempt.
    ///
    /// REGISTER IN EASYPAISA MERCHANT PORTAL as postBackURL:
    ///   https://api.yourdomain.com/api/credits/easypaisa/callback
    ///
    /// EasyPaisa sends (POST form-data or query string):
    ///   auth_token       — unique transaction token
    ///   orderRefNumber   — our txnRef
    ///   status           — 0000 = success, anything else = failure
    ///   storeId          — your store ID
    ///   paymentMethod    — MA_PAYMENT / OTC_PAYMENT
    ///   desc             — description / error message
    ///   signature        — optional AES hash for verification
    /// </summary>
    [HttpPost("easypaisa/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> EasyPaisaCallback(
        [FromQuery] Guid?   paymentId       = null,
        [FromForm]  string? auth_token      = null,
        [FromForm]  string? orderRefNumber  = null,
        [FromForm]  string? status          = null,
        [FromForm]  string? storeId         = null,
        [FromForm]  string? desc            = null,
        [FromForm]  string? paymentMethod   = null,
        [FromForm]  string? signature       = null)
    {
        var db        = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var config    = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var easypaisa = HttpContext.RequestServices.GetRequiredService<EasyPaisaService>();
        var logger    = HttpContext.RequestServices.GetRequiredService<ILogger<CreditsController>>();

        var frontendUrl = $"{config["PortalUrls:User"]}/billing";

        // Also check query string fallbacks (EasyPaisa sometimes sends as GET params)
        if (string.IsNullOrEmpty(auth_token))     auth_token     = Request.Query["auth_token"];
        if (string.IsNullOrEmpty(orderRefNumber)) orderRefNumber = Request.Query["orderRefNumber"];
        if (string.IsNullOrEmpty(status))         status         = Request.Query["status"];
        if (string.IsNullOrEmpty(signature))      signature      = Request.Query["signature"];

        logger.LogInformation(
            "EasyPaisa callback: paymentId={PaymentId}, orderRef={OrderRef}, status={Status}, token={Token}",
            paymentId, orderRefNumber, status, auth_token);

        // Find our payment record
        TenXConvo.Domain.Entities.PaymentTransaction? payment = null;

        if (paymentId.HasValue)
            payment = await db.PaymentTransactions.FindAsync(paymentId.Value);

        if (payment == null && !string.IsNullOrEmpty(orderRefNumber))
            payment = await db.PaymentTransactions
                .FirstOrDefaultAsync(p => p.TransactionRef == orderRefNumber);

        if (payment == null)
        {
            logger.LogWarning("EasyPaisa callback: payment not found. paymentId={PaymentId}, orderRef={OrderRef}",
                paymentId, orderRefNumber);
            return Redirect($"{frontendUrl}?payment=failed&error=payment_not_found");
        }

        // Idempotent — already processed
        if (payment.Status != "pending")
        {
            logger.LogInformation("EasyPaisa already processed ({Status}) for {TxnRef}", payment.Status, payment.TransactionRef);
            return Redirect($"{frontendUrl}?payment={(payment.Status == "completed" ? "success" : payment.Status)}");
        }

        // Validate hash (only if signature was sent)
        if (!string.IsNullOrEmpty(signature))
        {
            var isValid = easypaisa.ValidateCallback(auth_token, orderRefNumber, status, signature);
            if (!isValid)
            {
                logger.LogWarning("EasyPaisa hash validation failed for {TxnRef}", payment.TransactionRef);
                payment.Status        = "failed";
                payment.FailureReason = "Hash validation failed";
                payment.GatewayResponse = $"status={status}&signature_mismatch=true";
                await db.SaveChangesAsync();
                return Redirect($"{frontendUrl}?payment=failed&error=hash_invalid");
            }
        }

        // ── SUCCESS ──────────────────────────────────────────────────────────
        if (status == "0000")
        {
            payment.Status          = "completed";
            payment.CompletedAt     = DateTime.UtcNow;
            payment.GatewayTxnId    = auth_token;
            payment.GatewayResponse = $"status={status}&auth_token={auth_token}&orderRefNumber={orderRefNumber}&paymentMethod={paymentMethod}";
            await db.SaveChangesAsync();

            await _credits.AddCreditsAsync(
                payment.UserId, payment.Id,
                payment.TextCharsBought, payment.AudioMinsBought, payment.VideoMinsBought,
                payment.ImageCreditsBought, payment.FileCreditsBought);

            var invoiceService = HttpContext.RequestServices.GetRequiredService<InvoiceService>();
            await invoiceService.GenerateInvoiceAsync(payment);

            logger.LogInformation("✅ EasyPaisa {TxnRef} completed via {Method}, token={Token}",
                payment.TransactionRef, paymentMethod, auth_token);

            return Redirect($"{frontendUrl}?payment=success");
        }

        // ── FAILED ───────────────────────────────────────────────────────────
        payment.Status          = "failed";
        payment.FailureReason   = $"status={status}, desc={desc}";
        payment.GatewayTxnId    = auth_token;
        payment.GatewayResponse = $"status={status}&auth_token={auth_token}&orderRefNumber={orderRefNumber}&desc={desc}";
        await db.SaveChangesAsync();

        logger.LogWarning("❌ EasyPaisa {TxnRef} failed: status={Status}, desc={Desc}",
            payment.TransactionRef, status, desc);

        return Redirect($"{frontendUrl}?payment=failed&error={Uri.EscapeDataString(desc ?? $"Payment failed (status: {status})")}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  JAZZCASH (Pakistan — Payaxis) — Hosted Checkout + Callback
    //
    //  CALLBACK URL (register as pp_ReturnURL):
    //    https://api.yourdomain.com/api/credits/jazzcash/callback
    //
    //  JazzCash posts back: pp_ResponseCode (000=success), pp_TxnRefNo,
    //  pp_RetreivalReferenceNo, pp_AuthCode, pp_SecureHash
    //
    //  SETUP (appsettings.json):
    //    "JazzCash": {
    //      "MerchantId": "YOUR_MERCHANT_ID",
    //      "Password":   "YOUR_PASSWORD",
    //      "HashKey":    "YOUR_HASH_INTEGRITY_SALT",
    //      "Sandbox":    true,
    //      "Currency":   "PKR"
    //    }
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Purchase credits via JazzCash Hosted Payment Portal.
    /// Returns HTML that auto-POSTs to JazzCash payment page.
    /// Supports Debit Card, Mobile Wallet, OTC, Direct Debit.
    /// </summary>
    [HttpPost("purchase/jazzcash")]
    public async Task<IActionResult> PurchaseJazzCash([FromBody] PurchaseRequest req)
    {
        if (req.TextChars <= 0 && req.AudioMins <= 0 && req.VideoMins <= 0 && req.ImageCredits <= 0 && req.FileCredits <= 0)
            return BadRequest(new { success = false, message = "Select at least one credit type." });

        var db       = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var config   = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var jazzcash = HttpContext.RequestServices.GetRequiredService<JazzCashService>();

        // Calculate total price from pricing table
        var pricing = await _credits.GetPricingAsync();
        decimal totalPrice = 0;
        string currency = jazzcash.Currency;
        var breakdown = new List<string>();
        foreach (var p in pricing)
        {
            var (units, label) = p.MessageType switch
            {
                "text"  => ((decimal)req.TextChars,    $"{req.TextChars:N0} text chars"),
                "audio" => ((decimal)req.AudioMins,     $"{req.AudioMins:F1} audio min"),
                "video" => ((decimal)req.VideoMins,     $"{req.VideoMins:F1} video min"),
                "image" => ((decimal)req.ImageCredits, $"{req.ImageCredits} images"),
                "file"  => ((decimal)req.FileCredits,  $"{req.FileCredits} files"),
                _       => (0m, "")
            };
            if (units > 0)
            {
                var requiredUnits = decimal.Ceiling(units / p.UnitSize);
                var unitCost = requiredUnits * p.PricePerUnit;

                totalPrice += unitCost;
                breakdown.Add($"{label} = {unitCost:F2} {currency.ToUpper()}");
            }
        }
        if (totalPrice <= 0) return BadRequest(new { success = false, message = "Nothing to purchase." });

        // Create pending payment record
        var txnRef = $"JC-{DateTime.UtcNow:yyyyMMddHHmm}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var payment = new TenXConvo.Domain.Entities.PaymentTransaction
        {
            UserId             = UserId,
            TransactionRef     = txnRef,
            TextCharsBought    = req.TextChars,
            AudioMinsBought    = req.AudioMins,
            VideoMinsBought    = req.VideoMins,
            ImageCreditsBought = req.ImageCredits,
            FileCreditsBought  = req.FileCredits,
            Amount             = totalPrice,
            Currency           = jazzcash.Currency,
            Gateway            = "jazzcash",
            Status             = "pending",
            IpAddress          = HttpContext.Connection.RemoteIpAddress?.ToString(),
        };
        db.PaymentTransactions.Add(payment);
        await db.SaveChangesAsync();

        // Build return URL — JazzCash will POST result here
        var apiUrl    = config["PortalUrls:Api"] ?? $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        var returnUrl = $"{apiUrl}/api/credits/jazzcash/callback?paymentId={payment.Id}";

        // Get user info
        var user = await db.Users.FindAsync(UserId);

        // Generate hosted checkout HTML form
        var result = jazzcash.GenerateHostedCheckoutForm(
            txnRefNo:       txnRef,
            amount:         totalPrice,
            returnUrl:      returnUrl,
            billReference:  txnRef,
            description:    $"Credits: {string.Join(" + ", breakdown)}",
            customerEmail:  user?.Email,
            customerMobile: user?.CellNo,
            customerId:     UserId.ToString());

        if (!result.Success)
        {
            db.PaymentTransactions.Remove(payment);
            await db.SaveChangesAsync();
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                paymentId       = payment.Id,
                transactionRef  = txnRef,
                totalPrice,
                currency        = jazzcash.Currency,
                breakdown       = string.Join(" + ", breakdown),
                gateway         = "jazzcash",
                sandbox         = jazzcash.Sandbox,
                paymentFormHtml = result.Html,
                message         = "Redirecting to JazzCash..."
            }
        });
    }

    /// <summary>
    /// JazzCash Callback — receives POST/GET from JazzCash after payment attempt.
    /// pp_ResponseCode "000" = success. Redirects user to frontend /billing page.
    /// </summary>
    [HttpPost("jazzcash/callback")][HttpGet("jazzcash/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> JazzCashCallback([FromQuery] Guid? paymentId = null)
    {
        var db       = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var config   = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var jazzcash = HttpContext.RequestServices.GetRequiredService<JazzCashService>();
        var logger   = HttpContext.RequestServices.GetRequiredService<ILogger<CreditsController>>();

        var frontendUrl = $"{config["PortalUrls:User"]}/billing";

        // Parse and validate the callback (form POST or query string)
        JazzCashCallbackResult cb;
        if (Request.HasFormContentType)
            cb = jazzcash.ValidateCallback(Request.Form);
        else
            cb = jazzcash.ValidateCallbackFromQuery(Request.Query);

        logger.LogInformation(
            "JazzCash callback: paymentId={PaymentId}, txnRef={TxnRef}, respCode={RespCode}, rrn={RRN}, msg={Msg}",
            paymentId, cb.TxnRefNo, cb.ResponseCode, cb.RetrievalRefNo, cb.ResponseMessage);

        // Find our payment record
        TenXConvo.Domain.Entities.PaymentTransaction? payment = null;
        if (paymentId.HasValue)
            payment = await db.PaymentTransactions.FindAsync(paymentId.Value);
        if (payment == null && !string.IsNullOrEmpty(cb.TxnRefNo))
            payment = await db.PaymentTransactions
                .FirstOrDefaultAsync(p => p.TransactionRef == cb.TxnRefNo);

        if (payment == null)
        {
            logger.LogWarning("JazzCash callback: payment not found. paymentId={PaymentId}, txnRef={TxnRef}",
                paymentId, cb.TxnRefNo);
            return Redirect($"{frontendUrl}?payment=failed&error=payment_not_found");
        }

        // Idempotent — already processed
        if (payment.Status != "pending")
        {
            logger.LogInformation("JazzCash already processed ({Status}) for {TxnRef}", payment.Status, payment.TransactionRef);
            return Redirect($"{frontendUrl}?payment={(payment.Status == "completed" ? "success" : payment.Status)}");
        }

        // Validate hash
        if (!cb.HashValid)
        {
            logger.LogWarning("JazzCash hash validation failed for {TxnRef}", payment.TransactionRef);
            payment.Status        = "failed";
            payment.FailureReason = "Hash validation failed";
            payment.GatewayResponse = $"pp_ResponseCode={cb.ResponseCode}&hash_mismatch=true";
            await db.SaveChangesAsync();
            return Redirect($"{frontendUrl}?payment=failed&error=hash_invalid");
        }

        // ── SUCCESS ──────────────────────────────────────────────────────────
        if (cb.Success)
        {
            payment.Status          = "completed";
            payment.CompletedAt     = DateTime.UtcNow;
            payment.GatewayTxnId    = cb.RetrievalRefNo;
            payment.GatewayResponse = $"pp_ResponseCode={cb.ResponseCode}&pp_AuthCode={cb.AuthCode}" +
                                      $"&pp_RetreivalReferenceNo={cb.RetrievalRefNo}&pp_BankID={cb.BankId}";
            await db.SaveChangesAsync();

            await _credits.AddCreditsAsync(
                payment.UserId, payment.Id,
                payment.TextCharsBought, payment.AudioMinsBought, payment.VideoMinsBought,
                payment.ImageCreditsBought, payment.FileCreditsBought);

            var invoiceService = HttpContext.RequestServices.GetRequiredService<InvoiceService>();
            await invoiceService.GenerateInvoiceAsync(payment);

            logger.LogInformation("✅ JazzCash {TxnRef} completed, RRN={RRN}, AuthCode={AuthCode}",
                payment.TransactionRef, cb.RetrievalRefNo, cb.AuthCode);

            return Redirect($"{frontendUrl}?payment=success");
        }

        // ── FAILED ───────────────────────────────────────────────────────────
        payment.Status          = "failed";
        payment.FailureReason   = $"{cb.ResponseMessage} (code: {cb.ResponseCode})";
        payment.GatewayTxnId    = cb.RetrievalRefNo;
        payment.GatewayResponse = $"pp_ResponseCode={cb.ResponseCode}&pp_ResponseMessage={cb.ResponseMessage}" +
                                  $"&pp_RetreivalReferenceNo={cb.RetrievalRefNo}";
        await db.SaveChangesAsync();

        logger.LogWarning("❌ JazzCash {TxnRef} failed: code={Code}, msg={Msg}",
            payment.TransactionRef, cb.ResponseCode, cb.ResponseMessage);

        return Redirect($"{frontendUrl}?payment=failed&error={Uri.EscapeDataString(cb.ResponseMessage ?? $"Payment failed (code: {cb.ResponseCode})")}");
    }

    /// <summary>
    /// JazzCash Payment Inquiry — check the status of a JazzCash payment.
    /// Useful for verifying pending transactions or OTC payments.
    /// </summary>
    [HttpGet("jazzcash/inquiry/{paymentId:guid}")]
    public async Task<IActionResult> JazzCashInquiry(Guid paymentId)
    {
        var db       = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var jazzcash = HttpContext.RequestServices.GetRequiredService<JazzCashService>();

        var payment = await db.PaymentTransactions
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == UserId && p.Gateway == "jazzcash");
        if (payment == null)
            return NotFound(new { success = false, message = "JazzCash payment not found." });

        var result = await jazzcash.InquirePaymentAsync(payment.TransactionRef);

        return Ok(new
        {
            success = true,
            data = new
            {
                paymentId       = payment.Id,
                transactionRef  = payment.TransactionRef,
                localStatus     = payment.Status,
                gatewayResponse = result.ResponseCode,
                rawResponse     = result.RawResponse,
            }
        });
    }

    /// <summary>Admin: confirm payment manually (for testing or manual verification)</summary>
    [HttpPost("confirm-payment")][Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest req)
    {
        var db = HttpContext.RequestServices.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        var payment = await db.PaymentTransactions.FindAsync(req.PaymentId);
        if (payment == null) return NotFound(new { success = false, message = "Payment not found." });
        if (payment.Status != "pending") return BadRequest(new { success = false, message = $"Payment already {payment.Status}." });

        payment.Status      = "completed";
        payment.CompletedAt = DateTime.UtcNow;
        payment.GatewayTxnId = req.GatewayTxnId;
        await db.SaveChangesAsync();

        // Add purchased credits to user's balance
        var credits = await _credits.AddCreditsAsync(
            payment.UserId, payment.Id,
            payment.TextCharsBought, payment.AudioMinsBought, payment.VideoMinsBought,
            payment.ImageCreditsBought, payment.FileCreditsBought);

        // Auto-generate invoice
        var invoiceService = HttpContext.RequestServices.GetRequiredService<InvoiceService>();
        var invoice = await invoiceService.GenerateInvoiceAsync(payment);

        return Ok(new { success = true, data = credits, invoiceNumber = invoice.InvoiceNumber, message = "Payment confirmed. Credits added. Invoice generated." });
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  ADMIN — PRICING + CREDIT MANAGEMENT
// ═══════════════════════════════════════════════════════════════════════════

[ApiController][Route("api/admin/pricing")][Authorize(Policy = "AdminOnly")]
public class AdminPricingController : ControllerBase
{
    private readonly CreditService _credits;
    public AdminPricingController(CreditService credits) => _credits = credits;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(new { success = true, data = await _credits.GetPricingAsync() });

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePricingRequest req)
    {
        try
        {
            var admin = User.FindFirst("name")?.Value ?? "Admin";
            await _credits.UpdatePricingAsync(id, req.PricePerUnit, req.UnitSize, req.Description, req.IsActive, admin);
            return Ok(new { success = true, message = "Pricing updated." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
    }

    /// <summary>Grant or deduct credits for a user (admin tool)</summary>
    [HttpPost("grant-credits")]
    public async Task<IActionResult> GrantCredits([FromBody] GrantCreditsRequest req)
    {
        var admin = User.FindFirst("name")?.Value ?? "Admin";
        var result = await _credits.AdminGrantAsync(req.UserId,
            req.TextChars, req.AudioMins, req.VideoMins, req.ImageCredits, req.FileCredits, admin);
        return Ok(new { success = true, data = result });
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────
public record PurchaseRequest(int TextChars = 0, double AudioMins = 0, double VideoMins = 0, int ImageCredits = 0, int FileCredits = 0, string? Gateway = null);
public record ConfirmPaymentRequest(Guid PaymentId, string? GatewayTxnId = null);
public record UpdatePricingRequest(decimal PricePerUnit, int UnitSize = 1, string? Description = null, bool IsActive = true);
public record GrantCreditsRequest(Guid UserId, int TextChars = 0, double AudioMins = 0, double VideoMins = 0, int ImageCredits = 0, int FileCredits = 0);
public record EasyPaisaOtcRequest(int TextChars = 0, double AudioMins = 0, double VideoMins = 0, int ImageCredits = 0, int FileCredits = 0, string? MobileAccountNo = null);
