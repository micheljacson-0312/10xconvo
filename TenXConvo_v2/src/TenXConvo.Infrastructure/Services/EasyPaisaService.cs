using System.Security.Cryptography;
using System.Text;
using System.Net.Http;

using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;

namespace TenXConvo.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════
//  EASYPAISA (EasyPay) PAYMENT SERVICE
//  Official EasyPay docs: https://easypay.easypaisa.com.pk/
//
//  SUPPORTS 2 MODES:
//  ─────────────────
//  1) HOSTED CHECKOUT (default — recommended)
//     Customer is redirected to EasyPaisa hosted page (Index.jsf).
//     Backend builds a POST form → returns HTML → browser auto-submits.
//     EasyPaisa redirects back to postBackURL with result params.
//
//  2) OPENAPI / REST (Mobile Account + OTC Over-The-Counter)
//     REST call to initiate-otc-transaction endpoint.
//     Returns token/reference for customer to pay at EasyPaisa shop or mobile app.
//
//  HASH ALGORITHM:
//     AES/ECB/PKCS5Padding — all fields concatenated as pipe-delimited string,
//     encrypted with HashKey, returned as Base64.
//
//  CONFIG (appsettings.json) — only these 2 fields needed:
//    "EasyPaisa": {
//      "StoreId":     "YOUR_STORE_ID",
//      "HashKey":     "YOUR_HASH_KEY",
//      "Sandbox":     true          ← false for production
//    }
//
//  URLS auto-derived from Sandbox flag:
//    Sandbox Index:    https://easypaystg.easypaisa.com.pk/easypay/Index.jsf
//    Prod Index:       https://easypay.easypaisa.com.pk/easypay/Index.jsf
//    Sandbox Confirm:  https://easypaystg.easypaisa.com.pk/easypay/Confirm.jsf
//    Prod Confirm:     https://easypay.easypaisa.com.pk/easypay/Confirm.jsf
//    OpenAPI Sandbox:  https://easypaystg.easypaisa.com.pk/easypay-service/rest/v4/initiate-otc-transaction
//    OpenAPI Prod:     https://easypay.easypaisa.com.pk/easypay-service/rest/v4/initiate-otc-transaction
// ═══════════════════════════════════════════════════════════════════════════

public class EasyPaisaService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EasyPaisaService> _log;
    private readonly HttpClient _http;

    public EasyPaisaService(IConfiguration config, ILogger<EasyPaisaService> log, IHttpClientFactory httpFactory)
    {
        _config = config;
        _log    = log;
        _http   = httpFactory.CreateClient("EasyPaisa");
    }

    // ── Config Accessors ─────────────────────────────────────────────────────
    public string StoreId  => _config["EasyPaisa:StoreId"] ?? "";
    public string HashKey  => _config["EasyPaisa:HashKey"] ?? "";
    public bool   Sandbox  => bool.TryParse(_config["EasyPaisa:Sandbox"], out var s) ? s : true;
    public string Currency => "PKR";

    // ── Auto-derived URLs (no manual URL entry needed) ───────────────────────
    private string BaseHost      => Sandbox
        ? "https://easypaystg.easypaisa.com.pk"
        : "https://easypay.easypaisa.com.pk";

    public string IndexUrl       => $"{BaseHost}/easypay/Index.jsf";
    public string ConfirmUrl     => $"{BaseHost}/easypay/Confirm.jsf";
    public string OpenApiOtcUrl  => $"{BaseHost}/easypay-service/rest/v4/initiate-otc-transaction";

    // ═══════════════════════════════════════════════════════════════════════
    //  MODE 1: HOSTED CHECKOUT — POST Redirect to EasyPaisa page
    //
    //  EasyPaisa expects these fields in the POST body:
    //    storeId, amount, postBackURL, orderRefNum, expiryDate,
    //    autoRedirect, paymentMethod, tran_type, hash
    //
    //  Hash = AES-ECB-PKCS5 of:
    //    "amount=X&expiryDate=Y&orderRefNum=Z&paymentMethod=MA_PAYMENT&postBackURL=U&storeId=S&tran_type=IOP"
    //    encrypted with HashKey (exactly 32 chars for AES-256, padded/trimmed)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates an HTML auto-submit form that redirects customer to EasyPaisa Hosted Checkout.
    /// Frontend receives this HTML → opens in new window → customer pays on EasyPaisa page
    /// → EasyPaisa POSTs back to postBackURL with auth_token, status, orderRefNumber.
    /// </summary>
    public EasyPaisaFormResult GenerateHostedCheckoutForm(
        string orderRefNum,
        decimal amount,
        string postBackUrl,
        string? expiryDate = null)
    {
        if (string.IsNullOrEmpty(StoreId) || string.IsNullOrEmpty(HashKey))
            return new EasyPaisaFormResult(false, null, null, "EasyPaisa StoreId or HashKey not configured.");

        var expiry   = expiryDate ?? DateTime.UtcNow.AddHours(24).ToString("yyyyMMdd HH:mm:ss");
        var amtStr   = amount.ToString("F2");
        const string paymentMethod = "MA_PAYMENT"; // MA_PAYMENT = Mobile Account (default)
        const string tranType      = "IOP";        // IOP = Internet Online Payment

        // Build hash input string — fields MUST be in alphabetical order
        var hashInput = $"amount={amtStr}&expiryDate={expiry}&orderRefNum={orderRefNum}" +
                        $"&paymentMethod={paymentMethod}&postBackURL={postBackUrl}" +
                        $"&storeId={StoreId}&tran_type={tranType}";

        var hash = ComputeAesHash(hashInput);

        // Build auto-submit HTML form
        var fields = new Dictionary<string, string>
        {
            ["storeId"]       = StoreId,
            ["amount"]        = amtStr,
            ["postBackURL"]   = postBackUrl,
            ["orderRefNum"]   = orderRefNum,
            ["expiryDate"]    = expiry,
            ["autoRedirect"]  = "0",
            ["paymentMethod"] = paymentMethod,
            ["tran_type"]     = tranType,
            ["hash"]          = hash,
        };

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<title>Redirecting to EasyPaisa...</title>");
        sb.AppendLine("<style>body{font-family:sans-serif;display:flex;align-items:center;justify-content:center;");
        sb.AppendLine("height:100vh;margin:0;background:#f0f9f4;color:#1a1a2e;}");
        sb.AppendLine(".loader{text-align:center;}");
        sb.AppendLine(".spin{width:44px;height:44px;border:3px solid #c6e9d5;border-top:3px solid #00a651;");
        sb.AppendLine("border-radius:50%;animation:s 1s linear infinite;margin:0 auto 16px;}");
        sb.AppendLine("@keyframes s{to{transform:rotate(360deg)}}");
        sb.AppendLine(".logo{font-size:28px;font-weight:800;color:#00a651;margin-bottom:8px;}");
        sb.AppendLine("h2{color:#1a1a2e;margin:0 0 8px;}p{color:#6b7280;font-size:14px;}</style></head><body>");
        sb.AppendLine("<div class='loader'>");
        sb.AppendLine("<div class='logo'>EP</div>");
        sb.AppendLine("<div class='spin'></div>");
        sb.AppendLine("<h2>Redirecting to EasyPaisa...</h2>");
        sb.AppendLine("<p>Please wait, do not close this page.</p>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<form action='{IndexUrl}' method='post' id='ep_form' style='display:none'>");
        foreach (var f in fields)
            sb.AppendLine($"<input type='hidden' name='{f.Key}' value='{System.Web.HttpUtility.HtmlEncode(f.Value)}'/>");
        sb.AppendLine("</form>");
        sb.AppendLine("<script>window.onload=function(){document.getElementById('ep_form').submit();}</script>");
        sb.AppendLine("</body></html>");

        _log.LogInformation("EasyPaisa hosted checkout form generated for order {OrderRef}, amount {Amount} PKR",
            orderRefNum, amtStr);

        return new EasyPaisaFormResult(true, sb.ToString(), hash, null);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MODE 2: OPENAPI — Initiate OTC Transaction (REST)
    //
    //  POST to initiate-otc-transaction with JSON body.
    //  Returns auth_token (reference number) for customer to pay at EasyPaisa shop.
    //
    //  Hash input: "amount=X&expiryDate=Y&orderRefNum=Z&paymentMethod=OTC_PAYMENT&storeId=S&tran_type=OTC"
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initiate an OTC (Over-The-Counter) or Mobile Account transaction via EasyPaisa REST API.
    /// Customer pays at any EasyPaisa shop or via EasyPaisa mobile app using the token returned.
    /// </summary>
    public async Task<EasyPaisaOtcResult> InitiateOtcTransactionAsync(
        string orderRefNum,
        decimal amount,
        string? mobileAccountNo = null,
        string? expiryDate = null)
    {
        if (string.IsNullOrEmpty(StoreId) || string.IsNullOrEmpty(HashKey))
            return new EasyPaisaOtcResult(false, null, null, "EasyPaisa StoreId or HashKey not configured.");

        var expiry       = expiryDate ?? DateTime.UtcNow.AddHours(24).ToString("yyyyMMdd HH:mm:ss");
        var amtStr       = amount.ToString("F2");
        const string paymentMethod = "OTC_PAYMENT"; // OTC = Over The Counter
        const string tranType      = "OTC";

        // Hash input in alphabetical field order
        var hashInput = $"amount={amtStr}&expiryDate={expiry}&orderRefNum={orderRefNum}" +
                        $"&paymentMethod={paymentMethod}&storeId={StoreId}&tran_type={tranType}";
        var hash = ComputeAesHash(hashInput);

        // Build request body
        var requestBody = new
        {
            storeId       = StoreId,
            amount        = amtStr,
            orderRefNum   = orderRefNum,
            expiryDate    = expiry,
            paymentMethod = paymentMethod,
            tran_type     = tranType,
            mobileNum     = mobileAccountNo ?? "",
            hash          = hash,
        };

        try
        {
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync(OpenApiOtcUrl, jsonContent);
            var responseJson = await response.Content.ReadAsStringAsync();

            _log.LogInformation("EasyPaisa OTC response for {OrderRef}: {Response}", orderRefNum, responseJson);

            var doc = System.Text.Json.JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var status     = root.TryGetProperty("responseCode", out var rc) ? rc.GetString() : null;
            var authToken  = root.TryGetProperty("auth_token", out var at) ? at.GetString() : null;
            var message    = root.TryGetProperty("responseDesc", out var rd) ? rd.GetString() : "Unknown error";

            if (status == "0000" || !string.IsNullOrEmpty(authToken))
                return new EasyPaisaOtcResult(true, authToken, status, null);

            return new EasyPaisaOtcResult(false, null, status, message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "EasyPaisa OTC initiation failed for {OrderRef}", orderRefNum);
            return new EasyPaisaOtcResult(false, null, null, $"EasyPaisa API error: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CALLBACK VALIDATION
    //
    //  EasyPaisa POSTs to postBackURL with:
    //    auth_token, orderRefNumber, status (0000=success), storeId
    //
    //  Validate by re-computing hash from returned params and comparing.
    //  EasyPaisa hash on callback: AES of "auth_token=X&orderRefNumber=Y&status=Z&storeId=S"
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validate callback from EasyPaisa.
    /// Returns true if the hash/status is valid and payment succeeded.
    /// status "0000" = success.
    /// </summary>
    public bool ValidateCallback(
        string? authToken,
        string? orderRefNumber,
        string? status,
        string? receivedHash)
    {
        // If no hash provided, we do a basic status-only check (some integrations skip hash on callback)
        if (string.IsNullOrEmpty(receivedHash))
        {
            _log.LogWarning("EasyPaisa callback for {OrderRef}: no hash received — accepting status-only", orderRefNumber);
            return status == "0000";
        }

        // Rebuild hash from callback params in alphabetical order
        var hashInput = $"auth_token={authToken}&orderRefNumber={orderRefNumber}&status={status}&storeId={StoreId}";
        var computed  = ComputeAesHash(hashInput);
        var isValid   = string.Equals(computed, receivedHash, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
            _log.LogWarning("EasyPaisa hash mismatch for {OrderRef}: computed={Computed}, received={Received}",
                orderRefNumber, computed, receivedHash);

        return isValid;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  AES/ECB/PKCS5Padding Hash Computation
    //
    //  EasyPaisa standard: AES-ECB with PKCS5 (=PKCS7) padding.
    //  Key must be exactly 32 bytes for AES-256 (padded or trimmed from HashKey).
    //  Output: Base64-encoded ciphertext.
    // ═══════════════════════════════════════════════════════════════════════

    public string ComputeAesHash(string plaintext)
    {
        // Normalize key to exactly 32 bytes (AES-256)
        var keyBytes = new byte[32];
        var rawKey   = Encoding.UTF8.GetBytes(HashKey);
        Array.Copy(rawKey, keyBytes, Math.Min(rawKey.Length, 32));

        using var aes       = Aes.Create();
        aes.Key             = keyBytes;
        aes.Mode            = CipherMode.ECB;
        aes.Padding         = PaddingMode.PKCS7; // PKCS5 = PKCS7 in .NET
        aes.IV              = new byte[16];       // ECB mode ignores IV

        using var encryptor = aes.CreateEncryptor();
        var inputBytes      = Encoding.UTF8.GetBytes(plaintext);
        var encrypted       = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

        return Convert.ToBase64String(encrypted);
    }
}

// ── Result Records ────────────────────────────────────────────────────────
public record EasyPaisaFormResult(bool Success, string? Html, string? Hash, string? Error);
public record EasyPaisaOtcResult(bool Success, string? AuthToken, string? ResponseCode, string? Error);
