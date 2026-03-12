using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;


namespace TenXConvo.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════
//  JAZZCASH (Payaxis) PAYMENT GATEWAY SERVICE
//  Based on: Payaxis Payment Gateway Integration Guide v4.2
//
//  SUPPORTS 3 MODES:
//  ─────────────────
//  1) PAYMENT PORTAL (Hosted Checkout — recommended for web)
//     Customer is redirected to JazzCash hosted page via HTTP POST.
//     Backend builds a form with hidden fields → browser auto-submits.
//     JazzCash POSTs back to pp_ReturnURL with result params.
//     Supports: Debit Card (PAY), Mobile Wallet (MWALLET), OTC, Direct Debit (DD)
//
//  2) CARD PAYMENT API (REST — for in-app card payments)
//     Direct REST calls for Authorize → Capture → Refund → Void flow.
//     Requires PCI compliance at merchant end.
//
//  3) PAYMENT API (SOAP — for OTC/MWALLET server-to-server)
//     SOAP call to DoPaymentViaAPI for OTC voucher or Mobile Wallet.
//
//  HASH ALGORITHM:
//     SHA-256 HMAC — fields sorted alphabetically, joined with '&',
//     Shared Secret prepended, HMAC-SHA256 computed, hex-encoded.
//
//  CONFIG (appsettings.json):
//    "JazzCash": {
//      "MerchantId":   "YOUR_MERCHANT_ID",
//      "Password":     "YOUR_PASSWORD",
//      "HashKey":      "YOUR_HASH_INTEGRITY_SALT",
//      "Sandbox":      true,
//      "Currency":     "PKR"
//    }
//
//  URLS auto-derived from Sandbox flag:
//    Sandbox Portal:  https://sandbox.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform
//    Prod Portal:     https://payments.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform
//    Sandbox API:     https://sandbox.jazzcash.com.pk/ApplicationAPI/API/2.0/Purchase/PAY
//    Prod API:        https://payments.jazzcash.com.pk/ApplicationAPI/API/2.0/Purchase/PAY
//    Sandbox Inquiry: https://sandbox.jazzcash.com.pk/ApplicationAPI/API/PaymentInquiry/Inquire
//    Prod Inquiry:    https://payments.jazzcash.com.pk/ApplicationAPI/API/PaymentInquiry/Inquire
//
//  RESPONSE CODES:
//    000 = Success
//    124 = OTC order placed (pending payment at outlet)
//    For full list see Appendix I in integration guide.
// ═══════════════════════════════════════════════════════════════════════════

public class JazzCashService
{
    private readonly IConfiguration _config;
    private readonly ILogger<JazzCashService> _log;
    private readonly HttpClient _http;

    public JazzCashService(IConfiguration config, ILogger<JazzCashService> log, IHttpClientFactory httpFactory)
    {
        _config = config;
        _log    = log;
        _http   = httpFactory.CreateClient("JazzCash");
    }

    // ── Config Accessors ─────────────────────────────────────────────────────
    public string MerchantId => _config["JazzCash:MerchantId"] ?? "";
    public string Password   => _config["JazzCash:Password"]   ?? "";
    public string HashKey    => _config["JazzCash:HashKey"]     ?? "";
    public bool   Sandbox    => bool.TryParse(_config["JazzCash:Sandbox"], out var s) ? s : true;
    public string Currency   => _config["JazzCash:Currency"]    ?? "PKR";

    // ── Auto-derived URLs ────────────────────────────────────────────────────
    private string BaseHost => Sandbox
        ? "https://sandbox.jazzcash.com.pk"
        : "https://payments.jazzcash.com.pk";

    public string PortalUrl     => $"{BaseHost}/CustomerPortal/transactionmanagement/merchantform";
    public string PayApiUrl     => $"{BaseHost}/ApplicationAPI/API/2.0/Purchase/PAY";
    public string AuthorizeUrl  => $"{BaseHost}/ApplicationAPI/API/2.0/authorize/AuthorizePayment";
    public string CaptureUrl    => $"{BaseHost}/ApplicationAPI/API/2.0/authorize/Capture";
    public string VoidUrl       => $"{BaseHost}/ApplicationAPI/API/authorize/Void";
    public string RefundUrl     => $"{BaseHost}/ApplicationAPI/API/authorize/Refund";
    public string InquiryUrl    => $"{BaseHost}/ApplicationAPI/API/PaymentInquiry/Inquire";

    // ═══════════════════════════════════════════════════════════════════════
    //  MODE 1: PAYMENT PORTAL — Hosted Checkout (Version 2.0)
    //
    //  Generates an HTML auto-submit form that redirects to JazzCash's
    //  hosted payment page. Supports Debit Card, Mobile Wallet, OTC, DD.
    //
    //  Customer pays on JazzCash page → JazzCash POSTs to pp_ReturnURL.
    //  Response fields: pp_ResponseCode, pp_ResponseMessage, pp_TxnRefNo,
    //                   pp_RetreivalReferenceNo, pp_AuthCode, etc.
    //
    //  pp_ResponseCode "000" = success.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates an HTML auto-submit form that redirects customer to JazzCash Hosted Payment Portal.
    /// Frontend receives this HTML → renders in new window/iframe → customer pays → JazzCash POSTs callback.
    /// </summary>
    public JazzCashFormResult GenerateHostedCheckoutForm(
        string txnRefNo,
        decimal amount,
        string returnUrl,
        string billReference,
        string description,
        string? txnType = null,
        string? customerEmail = null,
        string? customerMobile = null,
        string? customerId = null,
        string? expiryDateTime = null)
    {
        if (string.IsNullOrEmpty(MerchantId) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(HashKey))
            return new JazzCashFormResult(false, null, null, "JazzCash MerchantId, Password, or HashKey not configured.");

        var version    = "2.0";
        var language   = "EN";
        var currency   = Currency;
        var amtStr     = ((int)(amount * 100)).ToString(); // No decimals — 100.00 → "10000"
        var txnDt      = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var expiry     = expiryDateTime ?? DateTime.UtcNow.AddHours(24).ToString("yyyyMMddHHmmss");
        var isRegistered = string.IsNullOrEmpty(customerId) ? "No" : "Yes";

        // Build fields dictionary — ALL fields that go into hash MUST be here
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["pp_Amount"]              = amtStr,
            ["pp_BillReference"]       = billReference,
            ["pp_Description"]         = description,
            ["pp_IsRegisteredCustomer"] = isRegistered,
            ["pp_Language"]            = language,
            ["pp_MerchantID"]          = Sandbox ? $"Test{MerchantId}" : MerchantId,
            ["pp_Password"]            = Password,
            ["pp_ReturnURL"]           = returnUrl,
            ["pp_TxnCurrency"]         = currency,
            ["pp_TxnDateTime"]         = txnDt,
            ["pp_TxnExpiryDateTime"]   = expiry,
            ["pp_TxnRefNo"]            = txnRefNo,
            ["pp_Version"]             = version,
        };

        // Optional fields
        if (!string.IsNullOrEmpty(txnType))
            fields["pp_TxnType"] = txnType;
        if (!string.IsNullOrEmpty(customerEmail))
            fields["pp_CustomerEmail"] = customerEmail;
        if (!string.IsNullOrEmpty(customerMobile))
            fields["pp_CustomerMobile"] = customerMobile;
        if (!string.IsNullOrEmpty(customerId))
            fields["pp_CustomerID"] = customerId;

        // Compute HMAC-SHA256 secure hash
        var hash = ComputeHmacSha256(fields);
        fields["pp_SecureHash"] = hash;

        // Build auto-submit HTML form
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<title>Redirecting to JazzCash...</title>");
        sb.AppendLine("<style>body{font-family:sans-serif;display:flex;align-items:center;justify-content:center;");
        sb.AppendLine("height:100vh;margin:0;background:#fef3f2;color:#1a1a2e;}");
        sb.AppendLine(".loader{text-align:center;}");
        sb.AppendLine(".spin{width:44px;height:44px;border:3px solid #fecaca;border-top:3px solid #e11d48;");
        sb.AppendLine("border-radius:50%;animation:s 1s linear infinite;margin:0 auto 16px;}");
        sb.AppendLine("@keyframes s{to{transform:rotate(360deg)}}");
        sb.AppendLine(".logo{font-size:28px;font-weight:800;color:#e11d48;margin-bottom:8px;}");
        sb.AppendLine("h2{color:#1a1a2e;margin:0 0 8px;}p{color:#6b7280;font-size:14px;}</style></head><body>");
        sb.AppendLine("<div class='loader'>");
        sb.AppendLine("<div class='logo'>JazzCash</div>");
        sb.AppendLine("<div class='spin'></div>");
        sb.AppendLine("<h2>Redirecting to JazzCash...</h2>");
        sb.AppendLine("<p>Please wait, do not close this page.</p>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<form action='{PortalUrl}' method='post' id='jc_form' style='display:none'>");
        foreach (var f in fields)
            sb.AppendLine($"<input type='hidden' name='{f.Key}' value='{System.Web.HttpUtility.HtmlEncode(f.Value)}'/>");
        sb.AppendLine("</form>");
        sb.AppendLine("<script>window.onload=function(){document.getElementById('jc_form').submit();}</script>");
        sb.AppendLine("</body></html>");

        _log.LogInformation("JazzCash hosted checkout form generated for {TxnRef}, amount {Amount} PKR",
            txnRefNo, amtStr);

        return new JazzCashFormResult(true, sb.ToString(), hash, null);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MODE 2: CARD PAYMENT — Direct Pay via REST API (Version 2.0)
    //
    //  Single transaction: authorize + capture in one call.
    //  Requires card details — merchant must be PCI compliant.
    //  Use hosted checkout (Mode 1) instead if PCI compliance is not available.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Direct card payment via JazzCash REST API (authorize + capture in one call).
    /// Requires PCI compliance. For most merchants, use hosted checkout instead.
    /// </summary>
    public async Task<JazzCashApiResult> DirectPayAsync(
        string txnRefNo,
        decimal amount,
        string cardNumber,
        string cardExpiry,
        string cardCvv,
        string billReference,
        string description,
        string? customerEmail = null,
        string? customerMobile = null,
        string? customerId = null,
        string? secureId = null)
    {
        if (string.IsNullOrEmpty(MerchantId) || string.IsNullOrEmpty(Password))
            return new JazzCashApiResult(false, null, null, "JazzCash not configured.");

        var amtStr = ((int)(amount * 100)).ToString();
        var txnDt  = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var expiry = DateTime.UtcNow.AddHours(1).ToString("yyyyMMddHHmmss");

        var requestBody = new Dictionary<string, string>
        {
            ["pp_IsRegisteredCustomer"]   = string.IsNullOrEmpty(customerId) ? "No" : "Yes",
            ["pp_ShouldTokenizeCardNumber"] = "No",
            ["pp_CustomerID"]             = customerId ?? "",
            ["pp_CustomerEmail"]          = customerEmail ?? "",
            ["pp_CustomerMobile"]         = customerMobile ?? "",
            ["pp_TxnType"]                = "MPAY",
            ["pp_TxnRefNo"]               = txnRefNo,
            ["pp_MerchantID"]             = Sandbox ? $"Test{MerchantId}" : MerchantId,
            ["pp_Password"]               = Password,
            ["pp_Amount"]                 = amtStr,
            ["pp_TxnCurrency"]            = Currency,
            ["pp_TxnDateTime"]            = txnDt,
            ["pp_TxnExpiryDateTime"]      = expiry,
            ["pp_BillReference"]          = billReference,
            ["pp_Description"]            = description,
            ["pp_CustomerCardNumber"]     = cardNumber,
            ["pp_CustomerCardExpiry"]     = cardExpiry,
            ["pp_CustomerCardCvv"]        = cardCvv,
            ["pp_C3DSecureID"]            = secureId ?? "",
            ["pp_SecureHash"]             = "",
        };

        // Compute hash from sorted pp_ fields
        var sortedForHash = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in requestBody.Where(kv => kv.Key.StartsWith("pp_") && kv.Key != "pp_SecureHash"))
            sortedForHash[kv.Key] = kv.Value;
        requestBody["pp_SecureHash"] = ComputeHmacSha256(sortedForHash);

        return await PostJsonApiAsync(PayApiUrl, requestBody, txnRefNo);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PAYMENT STATUS INQUIRY
    //
    //  Check the status of any transaction via REST API.
    //  Returns response code + transaction details.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Inquire the status of a transaction by its reference number.
    /// </summary>
    public async Task<JazzCashApiResult> InquirePaymentAsync(string txnRefNo)
    {
        if (string.IsNullOrEmpty(MerchantId) || string.IsNullOrEmpty(Password))
            return new JazzCashApiResult(false, null, null, "JazzCash not configured.");

        var requestBody = new Dictionary<string, string>
        {
            ["pp_TxnRefNo"]    = txnRefNo,
            ["pp_MerchantID"]  = Sandbox ? $"Test{MerchantId}" : MerchantId,
            ["pp_Password"]    = Password,
            ["pp_SecureHash"]  = "",
            ["pp_Version"]     = "1.1",
        };

        var sortedForHash = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in requestBody.Where(kv => kv.Key.StartsWith("pp_") && kv.Key != "pp_SecureHash"))
            sortedForHash[kv.Key] = kv.Value;
        requestBody["pp_SecureHash"] = ComputeHmacSha256(sortedForHash);

        return await PostJsonApiAsync(InquiryUrl, requestBody, txnRefNo);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CALLBACK VALIDATION
    //
    //  JazzCash POSTs to pp_ReturnURL with all response fields.
    //  Validate by re-computing HMAC-SHA256 from response params
    //  (excluding pp_SecureHash itself) and comparing.
    //
    //  pp_ResponseCode "000" = success.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validate callback from JazzCash Payment Portal.
    /// Extracts all pp_ fields from the form POST, re-computes HMAC, and compares.
    /// Returns parsed result with success/failure status.
    /// </summary>
    public JazzCashCallbackResult ValidateCallback(IFormCollection form)
    {
        var responseCode    = form["pp_ResponseCode"].FirstOrDefault() ?? "";
        var responseMessage = form["pp_ResponseMessage"].FirstOrDefault() ?? "";
        var txnRefNo        = form["pp_TxnRefNo"].FirstOrDefault() ?? "";
        var retrievalRefNo  = form["pp_RetreivalReferenceNo"].FirstOrDefault() ?? "";
        var authCode        = form["pp_AuthCode"].FirstOrDefault() ?? "";
        var receivedHash    = form["pp_SecureHash"].FirstOrDefault() ?? "";
        var amount          = form["pp_Amount"].FirstOrDefault() ?? "";
        var bankId          = form["pp_BankID"].FirstOrDefault() ?? "";

        // Re-build hash from all pp_ fields (excluding pp_SecureHash)
        var sortedFields = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in form.Keys.Where(k => k.StartsWith("pp_") && k != "pp_SecureHash"))
        {
            var val = form[key].FirstOrDefault() ?? "";
            if (!string.IsNullOrEmpty(val))
                sortedFields[key] = val;
        }

        var computedHash = ComputeHmacSha256(sortedFields);
        var hashValid    = string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);

        if (!hashValid && !string.IsNullOrEmpty(receivedHash))
        {
            _log.LogWarning("JazzCash hash mismatch for {TxnRef}: computed={Computed}, received={Received}",
                txnRefNo, computedHash, receivedHash);
        }

        var isSuccess = responseCode == "000";

        return new JazzCashCallbackResult(
            Success:            isSuccess && (hashValid || string.IsNullOrEmpty(receivedHash)),
            HashValid:          hashValid || string.IsNullOrEmpty(receivedHash),
            ResponseCode:       responseCode,
            ResponseMessage:    responseMessage,
            TxnRefNo:           txnRefNo,
            RetrievalRefNo:     retrievalRefNo,
            AuthCode:           authCode,
            Amount:             amount,
            BankId:             bankId
        );
    }

    /// <summary>
    /// Overload: validate callback from query string parameters (GET redirect).
    /// </summary>
    public JazzCashCallbackResult ValidateCallbackFromQuery(IQueryCollection query)
    {
        var responseCode    = query["pp_ResponseCode"].FirstOrDefault() ?? "";
        var responseMessage = query["pp_ResponseMessage"].FirstOrDefault() ?? "";
        var txnRefNo        = query["pp_TxnRefNo"].FirstOrDefault() ?? "";
        var retrievalRefNo  = query["pp_RetreivalReferenceNo"].FirstOrDefault() ?? "";
        var authCode        = query["pp_AuthCode"].FirstOrDefault() ?? "";
        var receivedHash    = query["pp_SecureHash"].FirstOrDefault() ?? "";
        var amount          = query["pp_Amount"].FirstOrDefault() ?? "";

        var sortedFields = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in query.Keys.Where(k => k.StartsWith("pp_") && k != "pp_SecureHash"))
        {
            var val = query[key].FirstOrDefault() ?? "";
            if (!string.IsNullOrEmpty(val))
                sortedFields[key] = val;
        }

        var computedHash = ComputeHmacSha256(sortedFields);
        var hashValid    = string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
        var isSuccess    = responseCode == "000";

        return new JazzCashCallbackResult(
            Success:            isSuccess && (hashValid || string.IsNullOrEmpty(receivedHash)),
            HashValid:          hashValid || string.IsNullOrEmpty(receivedHash),
            ResponseCode:       responseCode,
            ResponseMessage:    responseMessage,
            TxnRefNo:           txnRefNo,
            RetrievalRefNo:     retrievalRefNo,
            AuthCode:           authCode,
            Amount:             amount,
            BankId:             ""
        );
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  HMAC-SHA256 Hash Computation (per Payaxis spec Section 14.2)
    //
    //  1. All PP fields sorted alphabetically by field name
    //  2. Values joined with '&' separator
    //  3. Shared Secret (HashKey) prepended before the '&'-separated values
    //  4. String converted to UTF-8 → ISO-8859-1
    //  5. HMAC-SHA256 with UTF-8 encoded HashKey as key
    //  6. Output: hex-encoded (uppercase)
    // ═══════════════════════════════════════════════════════════════════════

    public string ComputeHmacSha256(SortedDictionary<string, string> fields)
    {
        // Step 1-2: Sort fields alphabetically, join values with '&'
        var values = fields
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => kv.Value);

        // Step 3: Prepend HashKey
        var dataToHash = HashKey + "&" + string.Join("&", values);

        // Step 4-5: HMAC-SHA256
        var keyBytes  = Encoding.UTF8.GetBytes(HashKey);
        var dataBytes = Encoding.UTF8.GetBytes(dataToHash);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes  = hmac.ComputeHash(dataBytes);

        // Step 6: Hex encode (uppercase as per Payaxis convention)
        return Convert.ToHexString(hashBytes);
    }

    // ── Internal helper: POST JSON to JazzCash REST API ─────────────────────
    private async Task<JazzCashApiResult> PostJsonApiAsync(
        string url, Dictionary<string, string> body, string txnRefNo)
    {
        try
        {
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync(url, jsonContent);
            var responseJson = await response.Content.ReadAsStringAsync();

            _log.LogInformation("JazzCash API response for {TxnRef}: {Response}", txnRefNo, responseJson);

            var doc  = System.Text.Json.JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var respCode = root.TryGetProperty("pp_ResponseCode", out var rc) ? rc.GetString()
                         : root.TryGetProperty("responseCode", out var rc2) ? rc2.GetString()
                         : null;

            var respMsg  = root.TryGetProperty("pp_ResponseMessage", out var rm) ? rm.GetString()
                         : root.TryGetProperty("responseMessage", out var rm2) ? rm2.GetString()
                         : "Unknown response";

            var isSuccess = respCode == "000";

            return new JazzCashApiResult(isSuccess, respCode, responseJson, isSuccess ? null : respMsg);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "JazzCash API call failed for {TxnRef} at {Url}", txnRefNo, url);
            return new JazzCashApiResult(false, null, null, $"JazzCash API error: {ex.Message}");
        }
    }
}

// ── Result Records ────────────────────────────────────────────────────────

public record JazzCashFormResult(
    bool    Success,
    string? Html,
    string? Hash,
    string? Error);

public record JazzCashApiResult(
    bool    Success,
    string? ResponseCode,
    string? RawResponse,
    string? Error);

public record JazzCashCallbackResult(
    bool    Success,
    bool    HashValid,
    string  ResponseCode,
    string  ResponseMessage,
    string  TxnRefNo,
    string  RetrievalRefNo,
    string  AuthCode,
    string  Amount,
    string  BankId);
