using System.Security.Cryptography;
using System.Text;
using System.Net.Http;

using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TenXConvo.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════
//  PAYFAST (Pakistan) PAYMENT SERVICE
//  Based on: gopayfast.com/docs + WooCommerce plugin reverse-engineering
//
//  FLOW:
//  1. Backend gets ACCESS_TOKEN from PayFast
//  2. Backend returns auto-submit HTML form → customer auto-redirected to PayFast
//  3. Customer pays on PayFast hosted page (card/account/wallet/RAAST)
//  4. PayFast redirects back to SUCCESS_URL or FAILURE_URL with transaction result
//  5. PayFast also calls CHECKOUT_URL (IPN/backend callback) for server confirmation
//  6. Backend validates hash → marks payment complete → adds credits → generates invoice
//
//  CONFIG (appsettings.json):
//    "PayFast": {
//      "MerchantId":   "YOUR_MERCHANT_ID",
//      "SecuredKey":    "YOUR_SECURED_KEY",
//      "StoreId":       "YOUR_STORE_ID",
//      "MerchantName":  "10X Convo",
//      "BaseUrl":       "https://ipguat.apps.net.pk",     ← sandbox
//      "BaseUrl":       "https://ipg1.apps.net.pk",       ← production
//      "SuccessUrl":    "https://api.yourdomain.com/api/credits/payfast/callback?redirect=Y",
//      "FailureUrl":    "https://api.yourdomain.com/api/credits/payfast/callback?redirect=Y",
//      "CheckoutUrl":   "https://api.yourdomain.com/api/credits/payfast/callback",
//      "Currency":      "PKR",
//      "FrontendBillingUrl": "https://user.yourdomain.com/billing"
//    }
// ═══════════════════════════════════════════════════════════════════════════

public class PayFastService
{
    private readonly IConfiguration _config;
    private readonly ILogger<PayFastService> _log;
    private readonly HttpClient _http;

    public PayFastService(IConfiguration config, ILogger<PayFastService> log, IHttpClientFactory httpFactory)
    {
        _config = config;
        _log    = log;
        _http   = httpFactory.CreateClient("PayFast");
    }

    public string MerchantId  => _config["PayFast:MerchantId"] ?? "";
    public string SecuredKey  => _config["PayFast:SecuredKey"] ?? "";
    public string StoreId     => _config["PayFast:StoreId"] ?? "";
    public string MerchantName => _config["PayFast:MerchantName"] ?? "10X Convo";
    public string BaseUrl     => _config["PayFast:BaseUrl"] ?? "https://ipguat.apps.net.pk";
    public string Currency    => _config["PayFast:Currency"] ?? "PKR";

    // ── STEP 1: Get Access Token from PayFast ────────────────────────────────
    public async Task<string?> GetAccessTokenAsync(decimal amount, string basketId)
    {
        var tokenUrl = $"{BaseUrl}/Ecommerce/api/Transaction/GetAccessToken";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("MERCHANT_ID", MerchantId),
            new KeyValuePair<string, string>("SECURED_KEY", SecuredKey),
            new KeyValuePair<string, string>("TXNAMT", amount.ToString("F2")),
            new KeyValuePair<string, string>("BASKET_ID", basketId),
            new KeyValuePair<string, string>("CURRENCY_CODE", Currency),
        });

        try
        {
            var response = await _http.PostAsync(tokenUrl, formData);
            var json = await response.Content.ReadAsStringAsync();
            _log.LogInformation("PayFast token response: {Response}", json);

            var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ACCESS_TOKEN", out var tokenProp))
                return tokenProp.GetString();

            _log.LogWarning("PayFast token failed: {Response}", json);
            return null;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "PayFast GetAccessToken failed");
            return null;
        }
    }

    // ── STEP 2: Generate Auto-Submit HTML Form ───────────────────────────────
    // Returns HTML page that auto-submits to PayFast's PostTransaction endpoint
    // Customer's browser receives this → instantly redirected to PayFast payment page
    public async Task<PayFastFormResult> GeneratePaymentFormAsync(
        Guid paymentId, string basketId, decimal amount,
        string customerEmail, string customerPhone, string description,
        string successUrl, string failureUrl, string checkoutUrl)
    {
        var token = await GetAccessTokenAsync(amount, basketId);
        if (string.IsNullOrEmpty(token))
            return new PayFastFormResult(false, null, "Failed to get PayFast access token.");

        var postUrl = $"{BaseUrl}/Ecommerce/api/Transaction/PostTransaction";
        var signature = ComputeSha256(paymentId.ToString());
        var orderDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var fields = new Dictionary<string, string>
        {
            ["MERCHANT_ID"]             = MerchantId,
            ["MERCHANT_NAME"]           = MerchantName,
            ["TOKEN"]                   = token,
            ["PROCCODE"]                = "00",
            ["TXNAMT"]                  = amount.ToString("F2"),
            ["CUSTOMER_MOBILE_NO"]      = customerPhone,
            ["CUSTOMER_EMAIL_ADDRESS"]  = customerEmail,
            ["SIGNATURE"]               = signature,
            ["PLUGIN_VERSION"]          = "TENXCONVO-DOTNET-1.0",
            ["TXNDESC"]                 = description,
            ["SUCCESS_URL"]             = Uri.EscapeDataString($"{successUrl}&order_id={paymentId}"),
            ["FAILURE_URL"]             = Uri.EscapeDataString($"{failureUrl}&order_id={paymentId}"),
            ["BASKET_ID"]               = basketId,
            ["ORDER_DATE"]              = orderDate,
            ["CHECKOUT_URL"]            = Uri.EscapeDataString($"{checkoutUrl}?order_id={paymentId}"),
            ["TRAN_TYPE"]               = "ECOMM_PURCHASE",
            ["STORE_ID"]                = StoreId,
            ["CURRENCY_CODE"]           = Currency,
        };

        // Build auto-submit HTML form
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Redirecting to PayFast...</title>");
        sb.AppendLine("<style>body{font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;background:#f8fafc;color:#1a1a2e;}");
        sb.AppendLine(".loader{text-align:center;}.spin{width:40px;height:40px;border:3px solid #e2e8f0;border-top:3px solid #0ea5e9;border-radius:50%;animation:s 1s linear infinite;}");
        sb.AppendLine("@keyframes s{to{transform:rotate(360deg)}}</style></head><body>");
        sb.AppendLine("<div class='loader'><div class='spin' style='margin:0 auto 16px'></div>");
        sb.AppendLine("<h2>Redirecting to PayFast...</h2><p style='color:#64748b'>Please wait, do not close this page.</p></div>");
        sb.AppendLine($"<form action='{postUrl}' method='post' id='pf_form' style='display:none'>");
        foreach (var f in fields)
            sb.AppendLine($"<input type='hidden' name='{f.Key}' value='{f.Value}'/>");
        sb.AppendLine("</form>");
        sb.AppendLine("<script>window.onload=function(){document.getElementById('pf_form').submit();}</script>");
        sb.AppendLine("</body></html>");

        return new PayFastFormResult(true, sb.ToString(), null);
    }

    // ── STEP 3: Validate Callback Hash ───────────────────────────────────────
    // PayFast sends: validation_hash = sha256(basket_id|secured_key|merchant_id|err_code)
    public bool ValidateCallbackHash(string validationHash, string basketId, string errCode)
    {
        var protocol = $"{basketId}|{SecuredKey}|{MerchantId}|{errCode}";
        var computed = ComputeSha256(protocol);
        var isValid = string.Equals(computed, validationHash, StringComparison.OrdinalIgnoreCase);
        if (!isValid)
            _log.LogWarning("PayFast hash mismatch: expected={Computed}, received={Received}", computed, validationHash);
        return isValid;
    }

    // ── Helper ───────────────────────────────────────────────────────────────
    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}

public record PayFastFormResult(bool Success, string? Html, string? Error);
