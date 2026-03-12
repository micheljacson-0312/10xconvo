# 10X Convo — Full Solution Architecture

## System Overview
One backend. One database. Three frontend portals served from different subdomains.

```
┌─────────────────────────────────────────────────────────────────────┐
│                         FRONTEND LAYER                              │
│                                                                     │
│  admin.10xdigitalventures.com    Admin Management Portal (ADM)      │
│  consultant.10xdigitalventures.com  Consultant Portal (MHM/10x)    │
│  user.10xdigitalventures.com     Customer / User Portal             │
└───────────────────┬─────────────────────┬───────────────────────────┘
                    │  HTTP/HTTPS (JWT)   │
                    ▼                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      TenXConvo.API  (ONE API)                       │
│                                                                     │
│  /api/admin/...         ← Only Admin Role can access                │
│  /api/consultant/...    ← Only Consultant Role can access           │
│  /api/user/...          ← Client Role / Web Role / public           │
│  /api/auth/...          ← All portals share same auth               │
│                                                                     │
│  CORS configured per subdomain origin                               │
│  Role-based authorization per route group                           │
│  Same JWT — role claim decides what you can do                      │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ EF Core
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│              ONE DATABASE  (SQL Server / PostgreSQL)                │
│                                                                     │
│  Connection switching: QA ←→ Production  (login step 2 dropdown)   │
│  Location scoping: Head Office / Multan Office  (login step 2)      │
│  FiscalYear scoping: Financial Year 2026-2027  (login step 2)       │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Solution Structure

```
TenXConvo.sln
│
├── src/
│   ├── TenXConvo.Domain/                  ← Pure entities, no dependencies
│   │   └── Entities/
│   │       ├── AuthEntities.cs            ← AppUser, AppRole, RefreshToken, FiscalYear
│   │       ├── AdminEntities.cs           ← ErrorLog, Config, WebPushToken, Org, Location...
│   │       ├── ConsultantEntities.cs      ← ConsultantProfile, ClientSettings, Messaging...
│   │       └── UserEntities.cs            ← Customer profiles, connections, bookings...
│   │
│   ├── TenXConvo.Application/             ← Business logic, no EF/HTTP dependencies
│   │   ├── DTOs/
│   │   │   ├── AuthDtos.cs
│   │   │   ├── AdminDtos.cs
│   │   │   ├── ConsultantDtos.cs
│   │   │   └── UserDtos.cs
│   │   └── Interfaces/
│   │       ├── IAuthService.cs
│   │       ├── IAdminService.cs
│   │       ├── IConsultantService.cs
│   │       └── IUserService.cs
│   │
│   ├── TenXConvo.Infrastructure/          ← EF Core, external services
│   │   ├── Data/
│   │   │   └── AppDbContext.cs            ← All DbSets + seed data
│   │   └── Services/
│   │       ├── AuthService.cs
│   │       ├── NotificationService.cs     ← WA/SMS/Email/Web/App
│   │       └── FileService.cs             ← Logo/Avatar uploads
│   │
│   └── TenXConvo.API/                     ← ASP.NET Core Web API
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Middleware/
│       │   ├── ErrorHandlingMiddleware.cs ← Auto-logs to ErrorLog table
│       │   └── TenantMiddleware.cs        ← Reads Connection + FiscalYear from JWT claims
│       ├── Filters/
│       │   └── PortalAuthFilter.cs        ← Enforces portal-level role restrictions
│       └── Controllers/
│           ├── Auth/
│           │   └── AuthController.cs      ← /api/auth/** — shared by all portals
│           │
│           ├── Admin/                     ← /api/admin/** — Admin Role only
│           │   ├── SystemController.cs    ← ErrorLog, Config, WebPushToken
│           │   ├── DataConstantController.cs ← ControlType, ControlCategory, ClientArea...
│           │   ├── DataMappingController.cs  ← DocumentType, DocumentMovement, Criteria...
│           │   ├── DataGeoController.cs      ← Country, Province, City, District, Tehsil, Area
│           │   ├── SetupController.cs        ← Organization, Location
│           │   ├── UserMgmtController.cs     ← Role, RoleModule, RoleMenu, Users, Permissions
│           │   ├── TemplateController.cs     ← WA/SMS/Email/Web Templates
│           │   ├── NotificationController.cs ← WA/SMS/Email/Web/App Notifications
│           │   └── SettingsController.cs     ← Website/Business/Roles settings (global gear)
│           │
│           ├── Consultant/                ← /api/consultant/** — Consultant Role only
│           │   ├── ProfileController.cs   ← Consultant profile, bio, online status
│           │   ├── ClientsController.cs   ← Consultant's client list
│           │   ├── MessagingController.cs ← Chat / messages
│           │   └── DashboardController.cs ← Consultant dashboard stats
│           │
│           └── User/                      ← /api/user/** — Client/Web Role + public
│               ├── ConsultantsController.cs ← Browse consultant cards (public)
│               ├── ConnectController.cs     ← Connect ↗ button
│               ├── ProfileController.cs     ← Customer profile
│               └── DashboardController.cs   ← Customer dashboard
```

---

## Portal Access Matrix

| Route Prefix | Portal | Allowed Roles | Subdomain |
|-------------|--------|---------------|-----------|
| `/api/auth/**` | All | Public (login) | All 3 |
| `/api/admin/**` | Admin | `Admin Role` | admin.10xdigitalventures.com |
| `/api/consultant/**` | Consultant | `Consultant Role` | consultant.10xdigitalventures.com |
| `/api/user/**` | User/Customer | `Client Role`, `Web Role`, Anonymous | user.10xdigitalventures.com |

---

## Authentication Flow

```
ALL 3 PORTALS use the same /api/auth endpoints:

Step 1:  POST /api/auth/login/step1   { email }
         ← Validates email exists
         ← Returns: available Locations, Connections, FiscalYears

Step 2:  POST /api/auth/login/step2   { email, password, locationId, connection, fiscalYearId }
         ← Validates password
         ← Issues JWT with claims: userId, role, locationId, connection, fiscalYear
         ← Role in JWT determines which portal routes are accessible

JWT Claims:
  sub         = userId (Guid)
  role        = "Admin Role" | "Consultant Role" | "Client Role" | "Web Role"
  location    = locationId
  connection  = "QA" | "Production"
  fiscalYear  = fiscalYearId
  exp         = expiry
```

---

## CORS Configuration (per subdomain)

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminPortal", policy =>
        policy.WithOrigins("https://admin.10xdigitalventures.com")
              .AllowAnyMethod().AllowAnyHeader().AllowCredentials());

    options.AddPolicy("ConsultantPortal", policy =>
        policy.WithOrigins("https://consultant.10xdigitalventures.com")
              .AllowAnyMethod().AllowAnyHeader().AllowCredentials());

    options.AddPolicy("UserPortal", policy =>
        policy.WithOrigins("https://user.10xdigitalventures.com")
              .AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});
```

---

## Deployment Notes

- **One API** deployed to Windows hosting (IIS or Azure App Service)
- **Three frontend apps** (React/Next.js) deployed separately — each subdomain points to same API
- **Firebase** can be used for frontend hosting of all 3 subdomains
- **Connection switching** (QA/Production) is handled by TenantMiddleware reading the `connection` JWT claim and selecting the correct connection string at runtime
- **Environment:** UAT uses `uat-` prefix subdomains for staging
