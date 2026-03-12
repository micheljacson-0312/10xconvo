# 🏦 PayFast (Pakistan) Integration Guide

## Credentials (Already Configured)

| Key | Value |
|-----|-------|
| **Merchant ID** | `26290` |
| **Secured Key** | `-cVfb5YhoBJVjenfxfgNnod2` |
| **Partner Code** | `MEN.HUB25` |

---

## PayFast URLs

### Sandbox (Testing)
```
Base URL:       https://ipguat.apps.net.pk
Token URL:      https://ipguat.apps.net.pk/Ecommerce/api/Transaction/GetAccessToken
Checkout URL:   https://ipguat.apps.net.pk/Ecommerce/api/Transaction/PostTransaction
```

### Production (Live)
```
Base URL:       https://ipg1.apps.net.pk
Token URL:      https://ipg1.apps.net.pk/Ecommerce/api/Transaction/GetAccessToken
Checkout URL:   https://ipg1.apps.net.pk/Ecommerce/api/Transaction/PostTransaction
```

> **Switch to Live:** Change `PayFast:BaseUrl` in `appsettings.json` from
> `https://ipguat.apps.net.pk` → `https://ipg1.apps.net.pk`

---

## PayFast Dashboard
- **Signup Portal:** https://getstarted.apps.net.pk
- **Merchant Dashboard:** https://merchant.gopayfast.com
- **Developer Docs:** https://gopayfast.com/docs
- **Contact Support:** complaints@gopayfast.com | +92 21 37132793

---

## appsettings.json Configuration

All callback URLs are **auto-generated** from `PortalUrls`. Set your domains in ONE place:

```json
"PortalUrls": {
    "Api":        "https://api.10xdigitalventures.com",
    "User":       "https://user.10xdigitalventures.com"
}
```

PayFast only needs gateway keys (no callback URLs needed):
```json
"PayFast": {
    "MerchantId":   "26290",
    "SecuredKey":    "-cVfb5YhoBJVjenfxfgNnod2",
    "StoreId":       "",
    "MerchantName":  "10X Convo",
    "BaseUrl":       "https://ipguat.apps.net.pk",
    "Currency":      "PKR"
}
```

Auto-generated URLs from PortalUrls:
- `{Api}/api/credits/payfast/callback?redirect=Y` → Success/Failure redirect
- `{Api}/api/credits/payfast/callback` → IPN backend callback
- `{User}/billing?payment=success` → Frontend redirect after payment

### For Local Development
Just set PortalUrls in `appsettings.Development.json`:
```json
"PortalUrls": {
    "Api":   "http://localhost:5000",
    "User":  "http://localhost:3004"
}
```
All payment callbacks auto-adjust.

---

## Payment Flow

```
Customer clicks "Buy Credits" → Selects PayFast → Clicks "Pay with PayFast"
    │
    ▼
Frontend: POST /api/credits/purchase/payfast
    │    { textChars: 5000, audioMins: 10 }
    ▼
Backend: POST https://ipguat.apps.net.pk/Ecommerce/api/Transaction/GetAccessToken
    │    MERCHANT_ID=26290 & SECURED_KEY=... & TXNAMT=850.00 & BASKET_ID=TXN-xxx
    │    → Returns: { ACCESS_TOKEN: "abc123..." }
    ▼
Backend builds HTML auto-submit form → Returns to frontend
    │
    ▼
Frontend opens new window → Form auto-submits → PayFast hosted checkout page
    │
    ▼
Customer pays via: Card (Visa/MC/UPI/PayPak) | Bank Account | Wallet | RAAST
    │
    ├── SUCCESS (err_code=000)
    │   PayFast redirects → /api/credits/payfast/callback?redirect=Y&order_id=xxx
    │   &err_code=000&transaction_id=PF123&validation_hash=abc...
    │       │
    │       ▼ Backend validates hash: sha256(basket_id|secured_key|merchant_id|err_code)
    │       ▼ Credits added → Invoice generated → Redirect to /billing?payment=success
    │
    └── FAILED (err_code != 000)
        PayFast redirects → /api/credits/payfast/callback?redirect=Y&order_id=xxx
        &err_code=14&err_msg=Incorrect+details&validation_hash=def...
            │
            ▼ Backend marks payment failed → Redirect to /billing?payment=failed
```

---

## API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/credits/purchase/payfast` | POST | Create PayFast payment → returns auto-submit form |
| `/api/credits/payfast/callback` | GET | PayFast callback (success + failure on same URL) |

---

## How Hash Validation Works

PayFast sends `validation_hash` in callback. We verify:

```
Input:    basket_id + "|" + secured_key + "|" + merchant_id + "|" + err_code
          TXN-20260307-A1B2C3D4 | -cVfb5YhoBJVjenfxfgNnod2 | 26290 | 000

Hash:     SHA-256(input)

Compare:  our_hash == validation_hash from PayFast → VALID
```

If hash doesn't match → payment rejected (tamper protection).

---

## Error Codes (from PayFast)

| Code | Meaning |
|------|---------|
| `000` | Success |
| `001` | Pending |
| `002` | Timeout |
| `97` | Insufficient balance |
| `106` | Transaction limit exceeded |
| `14` | Incorrect details |
| `55` | Invalid OTP/PIN |
| `54` | Card expired |
| `13` | Invalid amount |
| `126` | Invalid account details |
| `9000` | Rejected by Fraud Monitoring |

---

## Supported Payment Methods

- Visa / MasterCard / PayPak / UnionPay cards
- Bank account + CNIC + OTP
- Mobile wallets (JazzCash, EasyPaisa, etc.)
- RAAST (real-time P2M)
- Google Pay (via PayFast)

---

## Going Live Checklist

1. Test with sandbox URL (`ipguat.apps.net.pk`) — verify payments work
2. Change `PayFast:BaseUrl` to `https://ipg1.apps.net.pk`
3. Update `SuccessUrl`, `FailureUrl`, `CheckoutUrl` to production domain
4. Update `FrontendBillingUrl` to production user portal URL
5. Verify callback URL is accessible from internet (PayFast must reach it)
6. Test a real transaction with a small amount
7. Monitor logs for any hash validation failures
