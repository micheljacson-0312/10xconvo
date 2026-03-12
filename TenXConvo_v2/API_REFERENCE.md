# 📡 API REFERENCE — TenXConvo v7

**Base URL (Dev):** `http://localhost:5000`
**API Docs UI:** `http://localhost:5000/scalar/v1`
**Health Check:** `GET /health`
**SignalR Hub:** `ws://localhost:5000/hubs/chat`

All responses follow: `{ "success": true/false, "data": {...}, "message": "..." }`

---

## 🔐 Authentication

All protected endpoints need: `Authorization: Bearer <accessToken>`

### Login Flow (2-Step)

**Step 1 — Get user info by email:**
```
POST /auth/login/step1
Body: { "email": "admin@htag.mhm" }
Response: { locations, connections, fiscalYears, userName, loginId }
```
Rate limit: 10/min per IP

**Step 2 — Login with password + context:**
```
POST /auth/login/step2
Body: {
  "email": "admin@htag.mhm",
  "password": "Admin@123",
  "locationId": "guid-from-step1",
  "connection": "default",
  "fiscalYearId": "guid-from-step1",
  "rememberMe": false
}
Response: { accessToken, refreshToken, expiresAt, user: { id, userName, roleName, ... } }
```
Rate limit: 5/min per IP

**Token Refresh:**
```
POST /auth/refresh
Body: { "refreshToken": "..." }
Response: { accessToken, refreshToken, expiresAt }
```

**Logout:**
```
POST /auth/logout          [Authorize]
Body: { "refreshToken": "..." }
```

**Get Current User:**
```
GET /auth/me               [Authorize]
Response: { id, userName, loginId, email, cellNo, imageUrl, roleName }
```

**Change Password:**
```
PUT /auth/change-password   [Authorize]
Body: { "currentPassword": "...", "newPassword": "..." }
```

---

### Forgot / Reset Password

```
POST /auth/forgot-password        [Public, Rate: 3/5min]
Body: { "email": "user@example.com" }
→ Sends 6-digit OTP to email (15 min expiry)

POST /auth/verify-reset-token     [Public]
Body: { "email": "...", "token": "123456" }
→ Returns: { resetSessionToken: "..." }

POST /auth/reset-password         [Public]
Body: { "email": "...", "resetSessionToken": "...", "newPassword": "..." }
→ Password changed + all refresh tokens revoked + confirmation email sent
```

---

### OAuth / Social Login

**Config (appsettings.json):**
```json
"OAuth": {
  "Google": {
    "ClientId": "xxxx.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-xxxx"
  },
  "Microsoft": {
    "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientSecret": "xxxx"
  }
}
```

**Google Setup:**
1. https://console.cloud.google.com/apis/credentials
2. Create Credentials → OAuth 2.0 Client ID → Web application
3. Authorized redirect URIs:
   - Dev: `http://localhost:5000/signin-google`
   - Prod: `https://api.yourdomain.com/signin-google`
4. Copy ClientId + ClientSecret → appsettings.json

**Microsoft Setup:**
1. https://portal.azure.com → App Registrations → New Registration
2. Redirect URI:
   - Dev: `http://localhost:5000/signin-microsoft`
   - Prod: `https://api.yourdomain.com/signin-microsoft`
3. Certificates & secrets → New client secret
4. Copy ClientId + ClientSecret → appsettings.json

**Endpoints:**
```
POST /auth/external-login         [Public]
Body: {
  "provider": "Google",              ← "Google" ya "Microsoft"
  "providerKey": "google-user-id",   ← OAuth provider se mila hua ID
  "email": "user@gmail.com",
  "displayName": "User Name",
  "avatarUrl": "https://...",
  "locationId": "guid",             ← optional
  "connection": "default",          ← optional
  "fiscalYearId": "guid"            ← optional
}
Response: { accessToken, refreshToken, expiresAt, user: {...} }
→ User nahi mila toh auto-register + login
→ User mila toh direct login

GET /auth/external-logins         [Authorize]
→ Linked providers list: [{ provider, email, linkedAt }]

POST /auth/external-logins/link   [Authorize]
Body: { "provider": "Microsoft", "providerKey": "...", "email": "..." }
→ Current account ke saath naya provider link karo

DELETE /auth/external-logins/{provider}  [Authorize]
→ Provider unlink karo (e.g. /external-logins/Google)
```

**Frontend Flow:**
1. Frontend Google/Microsoft OAuth popup open karta hai
2. User authorize karta hai → id_token milta hai
3. Frontend id_token se claims extract karta hai (email, name, sub)
4. `POST /auth/external-login` call karta hai claims ke saath
5. Backend JWT issue karta hai → frontend store karta hai

---

## 💳 Credits & Payments

### Credit Balance & History
```
GET /credits/balance              [Authorize]
→ { textCharsRemaining, audioMinsRemaining, videoMinsRemaining, imageCreditsRemaining, fileCreditsRemaining }

GET /credits/history?page=1&pageSize=20   [Authorize]
→ Purchase + usage log with pagination

GET /credits/pricing              [Public]
→ Pricing rates: [{ messageType, unitType, pricePerUnit, unitSize, currency }]

GET /credits/verify/{paymentId}   [Authorize]
→ Payment status check (after redirect from gateway)
```

### Admin — Pricing & Credits
```
GET /admin/pricing                [Admin]
PUT /admin/pricing/{id}           [Admin]
Body: { "pricePerUnit": 0.50, "unitSize": 250, "description": "...", "isActive": true }

POST /admin/pricing/grant-credits [Admin]
Body: { "userId": "guid", "textChars": 5000, "audioMins": 10, ... }

POST /credits/confirm-payment     [Admin]
Body: { "paymentId": "guid", "gatewayTxnId": "optional" }
→ Manual payment confirmation (testing/verification)
```

---

### Payment Gateway: Stripe (International)

**Config:**
```json
"Stripe": {
  "SecretKey":     "sk_test_xxx",
  "PublishableKey": "pk_test_xxx",
  "WebhookSecret":  "whsec_xxx",
  "Currency":       "usd"
}
```

**Endpoints:**
```
POST /credits/purchase            [Authorize]
Body: { "textChars": 5000, "audioMins": 10, "videoMins": 0, "imageCredits": 5, "fileCredits": 0 }
Response: { paymentId, stripeSessionId, checkoutUrl, totalPrice, breakdown }
→ Frontend user ko checkoutUrl pe redirect kare

POST /credits/callback/stripe     [Public — Webhook]
→ Stripe calls this. Register in Stripe Dashboard:
  URL: https://api.yourdomain.com/credits/callback/stripe
  Events: checkout.session.completed + payment_intent.payment_failed
```

**Local Testing:**
```bash
stripe listen --forward-to localhost:5000/credits/callback/stripe
```

---

### Payment Gateway: PayFast (Pakistan)

**Config:**
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
Sandbox: `https://ipguat.apps.net.pk` | Production: `https://ipg1.apps.net.pk`

**Endpoints:**
```
POST /credits/purchase/payfast    [Authorize]
Body: { "textChars": 5000, "audioMins": 10, ... }
Response: { paymentId, paymentFormHtml, totalPrice, breakdown }
→ Frontend paymentFormHtml ko new window mein render kare

GET /credits/payfast/callback     [Public]
→ PayFast redirects here after payment
  Params: order_id, basket_id, err_code (000=success), validation_hash, transaction_id
  Validates hash → credits add → invoice generate → redirect to /billing
```

**Hash:** `SHA-256( basket_id | secured_key | merchant_id | err_code )`

---

### Payment Gateway: EasyPaisa (Pakistan)

**Config:**
```json
"EasyPaisa": {
  "StoreId": "YOUR_STORE_ID",
  "HashKey": "YOUR_HASH_KEY",
  "Sandbox": true
}
```
URLs auto-derived: `Sandbox: true` → `easypaystg.easypaisa.com.pk`

**Endpoints:**
```
POST /credits/purchase/easypaisa      [Authorize]
Body: { "textChars": 5000, "audioMins": 10, ... }
Response: { paymentId, paymentFormHtml, totalPrice, sandbox }
→ Frontend HTML ko new window mein render kare → EasyPaisa hosted page

POST /credits/purchase/easypaisa/otc  [Authorize]
Body: { "textChars": 5000, ..., "mobileAccountNo": "03001234567" }
Response: { paymentId, authToken, totalPrice }
→ Customer authToken le ke EasyPaisa shop pe jaaye

POST /credits/easypaisa/callback      [Public]
→ EasyPaisa POSTs here: auth_token, orderRefNumber, status (0000=success)
  Validates hash → credits add → redirect to /billing
```

**Hash:** AES/ECB/PKCS5Padding (fields alphabetical, pipe-delimited, encrypted with HashKey)

---

### Payment Gateway: JazzCash (Pakistan — Payaxis)

**Config:**
```json
"JazzCash": {
  "MerchantId": "YOUR_MERCHANT_ID",
  "Password":   "YOUR_PASSWORD",
  "HashKey":    "YOUR_HASH_INTEGRITY_SALT",
  "Sandbox":    true,
  "Currency":   "PKR"
}
```

**URLs auto-derived (hardcoded nahi):**
- `Sandbox: true` → `sandbox.jazzcash.com.pk`
- `Sandbox: false` → `payments.jazzcash.com.pk`

**Sandbox test mein** Merchant ID ke aage "Test" prefix auto-add hota hai code mein.

**Endpoints:**
```
POST /credits/purchase/jazzcash       [Authorize]
Body: { "textChars": 5000, "audioMins": 10, "videoMins": 0, "imageCredits": 5, "fileCredits": 0 }
Response: {
  paymentId, transactionRef, totalPrice, currency, breakdown,
  gateway: "jazzcash", sandbox: true/false,
  paymentFormHtml   ← Frontend isko new window mein render kare
}

POST /credits/jazzcash/callback       [Public — also GET]
→ JazzCash POSTs here after payment attempt
  Form data: pp_ResponseCode (000=success), pp_TxnRefNo, pp_RetreivalReferenceNo,
             pp_AuthCode, pp_Amount, pp_BankID, pp_SecureHash
  Validates HMAC-SHA256 hash → credits add → invoice generate → redirect to frontend /billing

GET /credits/jazzcash/inquiry/{paymentId}  [Authorize]
→ Check payment status from JazzCash API (useful for pending/OTC transactions)
  Response: { paymentId, transactionRef, localStatus, gatewayResponse, rawResponse }
```

**Hash Algorithm (HMAC-SHA256 — per Payaxis spec Section 14.2):**
1. All `pp_` fields sort alphabetically by field name
2. Values join with `&`
3. HashKey prepend before the `&`-joined values
4. HMAC-SHA256 compute with HashKey as key
5. Output: hex-encoded uppercase

**JazzCash Merchant Portal mein register karo:**
- `pp_ReturnURL`: `https://api.yourdomain.com/credits/jazzcash/callback`

**Production switch:** Sirf `"Sandbox": false` karo. Baqi sab URLs automatic change hongi.

**Supported Payment Methods:**
- Debit Card (PAY)
- Mobile Wallet (MWALLET)
- Over The Counter (OTC)
- Direct Debit / Internet Banking (DD)
- MasterCard (MIGS)

**Response Codes (important ones):**
| Code | Meaning |
|------|---------|
| 000 | Success |
| 112 | Transaction cancelled by user |
| 116 | Transaction expired |
| 124 | OTC order placed, waiting for payment at outlet |
| 157 | Transaction pending (MWallet/MIGS) |
| 199 | System error |
| 999 | Transaction failed (technical issue) |

---

### Frontend Payment Integration Pattern

Sab gateways ke liye same pattern hai:

```javascript
// 1. Purchase call
const res = await api.post('/credits/purchase/jazzcash', {
  textChars: 5000,
  audioMins: 10
});

// 2. paymentFormHtml ko new window mein render karo
if (res.data.data.paymentFormHtml) {
  const win = window.open('', '_blank', 'width=600,height=700');
  win.document.write(res.data.data.paymentFormHtml);
}

// 3. Stripe ke liye checkoutUrl pe redirect karo
if (res.data.data.checkoutUrl) {
  window.location.href = res.data.data.checkoutUrl;
}

// 4. Payment complete hone pe user /billing?payment=success pe aayega
// 5. Verify karo:
const verify = await api.get(`/credits/verify/${res.data.data.paymentId}`);
```

---

## 🧾 Invoices

```
GET /invoices?page=1&pageSize=20        [Authorize]
→ My invoices list

GET /invoices/{id}/download             [Authorize]
→ Download invoice PDF

GET /admin/invoices?page=1&pageSize=20  [Admin]
→ All invoices (admin view)

GET /admin/invoices/stats               [Admin]
→ Revenue statistics

GET /admin/invoices/purchases           [Admin]
→ All payment transactions
```

---

## 👨‍💼 Admin APIs

### Settings
```
GET  /admin/settings                    → Get all settings
PUT  /admin/settings/website            → { isOnline, footer }
PUT  /admin/settings/business           → { businessName, businessNature, ... }
PUT  /admin/settings/roles              → { consultantRoleId, clientRoleId, chatUrl }
```

### Organization
```
GET  /admin/setup/organization          → Get org details
PUT  /admin/setup/organization          → Update org
POST /admin/setup/organization/logo     → Upload logo (multipart)
```

### Locations
```
GET    /admin/setup/locations
POST   /admin/setup/locations
PUT    /admin/setup/locations/{id}
DELETE /admin/setup/locations/{id}
```

### Roles & Permissions
```
GET    /admin/users/roles
POST   /admin/users/roles
PUT    /admin/users/roles/{id}
DELETE /admin/users/roles/{id}

GET    /admin/users/roles/{roleId}/modules    → Role ke modules
PUT    /admin/users/roles/{roleId}/modules    → Module permissions set karo

GET    /admin/users/roles/{roleId}/menus      → Role ke menus
PUT    /admin/users/roles/{roleId}/menus      → Menu access set karo
```

### Users
```
GET    /admin/users/registrations
GET    /admin/users/registrations/{id}
POST   /admin/users/registrations
PUT    /admin/users/registrations/{id}
DELETE /admin/users/registrations/{id}
PUT    /admin/users/registrations/{id}/reset-password
POST   /admin/users/registrations/{id}/avatar

GET    /admin/users/{userId}/permissions
PUT    /admin/users/{userId}/permissions
GET    /admin/users/{userId}/menu
```

### Consultant Config (Admin Side)
```
GET /admin/consultant-config/{userId}   → Consultant ki settings
PUT /admin/consultant-config/{userId}   → Update (services, pricing, currency, gateways)
```

### Error Logs
```
GET    /admin/system/error-logs
GET    /admin/system/error-logs/{id}
DELETE /admin/system/error-logs/{id}
```

### Fiscal Years
```
GET    /admin/setup/fiscal-years
POST   /admin/setup/fiscal-years
PUT    /admin/setup/fiscal-years/{id}
DELETE /admin/setup/fiscal-years/{id}
PUT    /admin/setup/fiscal-years/{id}/set-current
```

### Data Constants (CRUD for each)
Each follows same pattern: `GET (list)` + `POST` + `PUT /{id}` + `DELETE /{id}`

```
/admin/data/countries
/admin/data/provinces
/admin/data/districts
/admin/data/tehsils
/admin/data/cities
/admin/data/areas
/admin/data/currencies
/admin/data/location-types
/admin/data/document-types
/admin/data/client-areas
/admin/data/client-categories
/admin/data/control-types
/admin/data/control-categories
/admin/data/criteria-types
/admin/data/criteria-subtypes
/admin/data/reviews
```

### Document Movements (Auto-numbering)
```
GET    /admin/data/document-movements
POST   /admin/data/document-movements
PUT    /admin/data/document-movements/{id}
DELETE /admin/data/document-movements/{id}
POST   /admin/data/document-movements/{id}/next     → Get next number (CLTPTY-0001)
POST   /admin/data/document-movements/generate      → { "prefix": "CLTPTY" }
```

---

## 🔔 Notifications (Admin)

### Templates (CRUD for each channel)
```
/admin/notifications/templates/wa       → WhatsApp templates
/admin/notifications/templates/sms      → SMS templates
/admin/notifications/templates/email    → Email templates
/admin/notifications/templates/web      → Web push templates
```
Each: `GET` (list) + `POST` + `PUT /{id}` + `DELETE /{id}` + `POST /{id}/preview` + `PATCH /{id}/status`

### Send Notifications
```
POST /admin/notifications/wa/send                   → Send WhatsApp
POST /admin/notifications/sms/send                  → Send SMS
POST /admin/notifications/email/send                → Send Email
POST /admin/notifications/email/bulk                → Bulk email

POST /admin/notifications/app/broadcast             → App notification to all
POST /admin/notifications/app/send-to-user/{userId} → To specific user
```

### Web Push
```
POST /admin/notifications/webpush/register   → Register push token
POST /admin/notifications/webpush/send       → Send push notification
```

---

## 👨‍⚕️ Consultant APIs

```
GET  /consultant/profile                → My profile
PUT  /consultant/profile                → Update (bio, specialization, hourlyRate, slug, ...)
POST /consultant/profile/avatar         → Upload avatar
PUT  /consultant/profile/online         → Set online/offline

GET  /consultant/clients?page=1&pageSize=20     → My clients
GET  /consultant/clients/requests               → Pending connection requests
PUT  /consultant/clients/requests/{id}/accept   → Accept
PUT  /consultant/clients/requests/{id}/reject   → Reject

GET  /consultant/messages?page=1&pageSize=20              → Conversations list
GET  /consultant/messages/{conversationId}?page=1         → Messages in conversation
POST /consultant/messages/{conversationId}                → Send message
PUT  /consultant/messages/{conversationId}/read           → Mark read

GET  /consultant/availability                → Schedule
PUT  /consultant/availability/{id}           → Update slot

GET  /consultant/notifications               → My notifications
PUT  /consultant/notifications/read-all      → Mark all read
PUT  /consultant/notifications/{id}/read     → Mark one read
```

---

## 👤 User (Client) APIs

```
GET  /user/profile                      → My profile
PUT  /user/profile                      → Update (bio, companyName, industry, ...)
POST /user/profile/avatar               → Upload avatar

GET  /user/consultants?page=1&pageSize=20       → Browse consultants
GET  /user/consultants/{id}                      → Consultant detail
GET  /user/consultants/by-slug/{slug}            → Consultant by URL slug
POST /user/consultants/{id}/connect              → Request connection

GET  /user/consultants/{consultantUserId}/availability  → Consultant ke available slots
GET  /user/consultants/{consultantUserId}/reviews       → Reviews
POST /user/consultants/{consultantUserId}/reviews       → Write review

GET  /user/messages?page=1&pageSize=20           → Conversations list
GET  /user/messages/{conversationId}?page=1      → Messages
POST /user/messages/{conversationId}             → Send message (deducts credits)
PUT  /user/messages/{conversationId}/read        → Mark read
POST /user/messages/char-count                   → Check credit cost before sending

GET  /user/notifications                → My notifications
PUT  /user/notifications/read-all       → Mark all read
PUT  /user/notifications/{id}/read      → Mark one read
```

---

## 🔌 SignalR (Real-time)

**Hub URL:** `ws://localhost:5000/hubs/chat?access_token=JWT_TOKEN`

**Events (Server → Client):**
- `ReceiveMessage` → New message in conversation
- `MessageRead` → Messages marked as read
- `UserOnline` / `UserOffline` → Presence updates
- `NewNotification` → Push notification
- `ConnectionAccepted` → Connection request accepted

---

## ⚙️ Notification Services Config

### SendGrid (Email)
```json
"SendGrid": {
  "ApiKey": "SG.xxxxxxxxxxxxx",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "10X Convo"
}
```

### Twilio (SMS + WhatsApp)
```json
"Twilio": {
  "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "AuthToken": "your_auth_token",
  "FromNumber": "+1234567890",
  "WhatsAppFrom": "whatsapp:+14155238886"
}
```

### Web Push (VAPID)
Generate: `npx web-push generate-vapid-keys`
```json
"WebPush": {
  "VapidPublicKey": "base64url_public_key",
  "VapidPrivateKey": "base64url_private_key",
  "VapidSubject": "mailto:admin@yourdomain.com"
}
```

### Redis (Multi-Server SignalR)
```json
"Redis": {
  "Enabled": true,
  "ConnectionString": "redis-host:6379,password=xxx",
  "InstanceName": "TenXConvo_Prod_"
}
```

---

## 🔒 Rate Limiting

| Endpoint | Limit | Window |
|----------|-------|--------|
| `POST /auth/login/step1` | 10 | 1 min |
| `POST /auth/login/step2` | 5 | 1 min |
| `POST /auth/forgot-password` | 3 | 5 min |
| All other endpoints | 120 | 1 min |

Response when limited: `429 Too Many Requests`

---

## 🛡️ Authorization Policies

| Policy | Roles |
|--------|-------|
| `AdminOnly` | Admin Role |
| `ConsultantOnly` | Consultant Role |
| `ClientOnly` | Client Role, Web Role |
| `AdminOrConsultant` | Admin Role, Consultant Role |
| `AnyAuthenticated` | All roles |

---

## 📦 Payment Gateway Comparison Table

| Feature | Stripe | PayFast | EasyPaisa | JazzCash |
|---------|--------|---------|-----------|----------|
| Currency | USD/Global | PKR | PKR | PKR |
| Debit Card | ✅ | ✅ | ❌ | ✅ |
| Credit Card | ✅ | ✅ | ❌ | ✅ |
| Mobile Wallet | ❌ | ✅ | ✅ | ✅ |
| Bank / RAAST | ❌ | ✅ | ❌ | ✅ (DD) |
| OTC (Cash) | ❌ | ❌ | ✅ | ✅ |
| Hash | HMAC (Webhook) | SHA-256 | AES-ECB | HMAC-SHA256 |
| Sandbox Toggle | Test keys | BaseUrl | `Sandbox` flag | `Sandbox` flag |
| URLs Hardcoded? | N/A | Manual BaseUrl | ❌ Auto | ❌ Auto |
