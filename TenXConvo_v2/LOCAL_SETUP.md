# 🚀 LOCAL SETUP GUIDE — TenXConvo v7

## Stack
- **Backend:** .NET 10 + EF Core 10 + SignalR + SQLite (dev) / SQL Server (prod)
- **Frontend:** React 19 + Vite 7 (3 portals: Admin, Consultant, User)
- **Payments:** Stripe (International) + PayFast + EasyPaisa + JazzCash (Pakistan)
- **Auth:** JWT + Google OAuth + Microsoft OAuth
- **Realtime:** SignalR (WebSocket) + optional Redis backplane
- **Notifications:** Email (SendGrid) + SMS (Twilio) + WhatsApp (Twilio) + Web Push (VAPID)

---

## STEP 1 — Prerequisites Install Karo (sirf ek baar)

### 1. .NET 10 SDK
```
https://dotnet.microsoft.com/download/dotnet/10.0
```
"SDK x64" download karo → Install → PC Restart karo

### 2. Node.js 22+
```
https://nodejs.org
```
LTS version install karo

### 3. VS Code + Extensions
```
https://code.visualstudio.com
```
Extensions: `C# Dev Kit` (Microsoft), `SQLite Viewer` (optional)

### 4. Verify
```bash
dotnet --version     # 10.0.xxx
node --version       # v22+
```

---

## STEP 2 — Backend Run Karo

```bash
cd TenXConvo_v2/src/TenXConvo.API
dotnet restore
dotnet run
```

**Verify:** http://localhost:5000/health
**API Docs:** http://localhost:5000/scalar/v1

> Pehli baar run pe SQLite DB + seed users auto-create ho jayenge.

---

## STEP 3 — Frontend Portals Run Karo

Har portal ke liye alag terminal:

```bash
# Admin Portal → http://localhost:3000
cd tenx-frontend/admin-portal && npm install && npm run dev

# Consultant Portal → http://localhost:3002
cd tenx-frontend/consultant-portal && npm install && npm run dev

# User Portal → http://localhost:3004
cd tenx-frontend/user-portal && npm install && npm run dev
```

---

## STEP 4 — Test Accounts (Auto Seeded)

| Role | Email | Password | Portal |
|------|-------|----------|--------|
| Admin | admin@htag.mhm | Admin@123 | localhost:3000 |
| Consultant | ali@htag.mhm | Test@123 | localhost:3002 |
| Client | sara@htag.mhm | Test@123 | localhost:3004 |

---

## Database

**Dev (SQLite — zero config):** `tenxconvo.db` auto-created hota hai.

**Prod (SQL Server):** `appsettings.json` mein `"DatabaseProvider": "SqlServer"` set karo.

**EF Migrations:**
```bash
cd TenXConvo_v2
dotnet ef migrations add MigrationName -s src/TenXConvo.API -p src/TenXConvo.Infrastructure
dotnet ef database update -s src/TenXConvo.API -p src/TenXConvo.Infrastructure
```

---

## OAuth / Social Login Setup

Poori detail `API_REFERENCE.md` mein hai — yahan quick setup:

### Google OAuth
1. https://console.cloud.google.com/apis/credentials
2. Create OAuth 2.0 Client → Web application
3. Redirect URI: `http://localhost:5000/signin-google`
4. `appsettings.json` mein ClientId + ClientSecret daalo

### Microsoft OAuth
1. https://portal.azure.com → App Registrations → New
2. Redirect URI: `http://localhost:5000/signin-microsoft`
3. Certificates & secrets → New client secret
4. `appsettings.json` mein ClientId + ClientSecret daalo

---

## Payment Gateways — Quick Config

| Gateway | Config Section | Sandbox Toggle | Callback URL |
|---------|---------------|----------------|--------------|
| Stripe | `Stripe` | N/A (use sk_test keys) | `/api/credits/callback/stripe` |
| PayFast | `PayFast` | `BaseUrl` change karo | `/api/credits/payfast/callback` |
| EasyPaisa | `EasyPaisa` | `"Sandbox": true/false` | `/api/credits/easypaisa/callback` |
| JazzCash | `JazzCash` | `"Sandbox": true/false` | `/api/credits/jazzcash/callback` |

Poori detail `API_REFERENCE.md` ke Payment Gateways section mein hai.

---

## Common Errors

| Error | Fix |
|-------|-----|
| `dotnet: command not found` | .NET 10 SDK install karo |
| `Port 5000 in use` | `dotnet run --urls http://localhost:5001` |
| `CORS blocked` | appsettings.Development.json origins check karo |
| `PayFast token null` | MerchantId + SecuredKey verify karo |
| `JazzCash hash mismatch` | HashKey (integrity salt) verify karo |
| `OAuth redirect mismatch` | Google/Azure portal mein URI match karo |
| `npm install fails` | node_modules + lock file delete, retry |

---

## Production Checklist

- [ ] `appsettings.Production.json` created (example se copy)
- [ ] JWT Key strong (64+ chars)
- [ ] CORS origins = production domains
- [ ] SQL Server connection string set
- [ ] All payment gateways → Sandbox OFF / live keys
- [ ] SendGrid API key set
- [ ] VAPID keys generated
- [ ] HTTPS configured
