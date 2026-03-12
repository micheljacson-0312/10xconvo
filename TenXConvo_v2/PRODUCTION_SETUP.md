# TenXConvo — Production Setup Guide

## ⚡ Quick Start (Local Dev)

```bash
cd TenXConvo_v2/src/TenXConvo.API
dotnet run
```

✅ On first run, Program.cs automatically:
- Creates `tenxconvo.db` (SQLite) with all tables
- Seeds admin/consultant/client users
- **Re-hashes seed passwords using real BCrypt** (fixes the hardcoded hash issue)
- Creates `wwwroot/uploads/avatars`, `wwwroot/uploads/documents`, `wwwroot/uploads/logos`

**Test credentials after first run:**
| Email | Password | Portal |
|-------|----------|--------|
| admin@htag.mhm | Admin@123 | localhost:3000 |
| ali@htag.mhm   | Test@123  | localhost:3002 |
| sara@htag.mhm  | Test@123  | localhost:3004 |

---

## 🚀 Production Deployment Checklist

### 1. Generate a Strong JWT Key

**Option A — PowerShell (Windows):**
```powershell
[System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

**Option B — OpenSSL (Linux/Mac):**
```bash
openssl rand -base64 48
```

**Option C — dotnet:**
```csharp
Console.WriteLine(Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)));
```

Copy the output — it will look like:
`abc123XYZ...` (64 chars, base64)

### 2. Update appsettings.json

Replace `REPLACE_WITH_64_CHAR_SECRET__use_openssl_rand_base64_48` with your generated key:

```json
{
  "Jwt": {
    "Key": "YOUR_GENERATED_64_CHAR_KEY_HERE",
    "Issuer": "https://api.10xdigitalventures.com",
    "Audience": "10xdigitalventures"
  }
}
```

⚠️ **NEVER commit the real JWT key to git.** Use environment variables or Azure Key Vault instead.

### 3. Environment Variables (Recommended over appsettings)

```bash
# Linux / Docker
export Jwt__Key="your-64-char-key"
export ConnectionStrings__Default="Server=...;Database=TenXConvo_PROD;..."

# Windows
setx Jwt__Key "your-64-char-key"
```

ASP.NET Core automatically reads `Jwt__Key` env var as `Jwt:Key` in config.

### 4. HTTPS / SSL Certificate

**Option A — IIS (Windows Server):**
1. Buy SSL cert or use Let's Encrypt (win-acme)
2. Bind cert to site in IIS Manager
3. API will auto-redirect HTTP → HTTPS (UseHsts + UseHttpsRedirection in production)

**Option B — Nginx reverse proxy (Linux):**
```nginx
server {
    listen 443 ssl;
    server_name api.10xdigitalventures.com;

    ssl_certificate     /etc/letsencrypt/live/api.10xdigitalventures.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.10xdigitalventures.com/privkey.pem;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
    }

    # SignalR WebSocket support
    location /hubs {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_set_header   Host $host;
    }
}

# Redirect HTTP → HTTPS
server {
    listen 80;
    server_name api.10xdigitalventures.com;
    return 301 https://$host$request_uri;
}
```

Then run certbot:
```bash
sudo certbot --nginx -d api.10xdigitalventures.com
```

**Option C — Docker + Traefik (recommended for cloud):**
Add Traefik labels to your docker-compose.yml for automatic Let's Encrypt.

### 5. Switch to SQL Server (Production)

In `appsettings.json`:
```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "Default": "Server=YOUR_SQL_SERVER;Database=TenXConvo_PROD;User Id=sa;Password=xxx;TrustServerCertificate=True;"
  }
}
```

Run migrations:
```bash
dotnet ef database update --project ../TenXConvo.Infrastructure
```

### 6. Upload Directory Permissions (Linux)

```bash
# Create uploads dir with write permission for the API process
mkdir -p /var/www/tenxconvo/wwwroot/uploads/{avatars,documents,logos}
chown -R www-data:www-data /var/www/tenxconvo/wwwroot/uploads
chmod -R 755 /var/www/tenxconvo/wwwroot/uploads
```

### 7. Run as a Service (Linux systemd)

```ini
# /etc/systemd/system/tenxconvo.service
[Unit]
Description=10X Convo API
After=network.target

[Service]
WorkingDirectory=/var/www/tenxconvo
ExecStart=/usr/bin/dotnet TenXConvo.API.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=Jwt__Key=YOUR_64_CHAR_KEY
Environment=ConnectionStrings__Default=Server=...

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable tenxconvo
sudo systemctl start tenxconvo
sudo systemctl status tenxconvo
```

### 8. Final Security Checklist

- [ ] JWT Key is at least 48 bytes (64 chars base64), randomly generated
- [ ] JWT Key is NOT in any git repository
- [ ] HTTPS is enabled and HTTP redirects to HTTPS
- [ ] CORS origins are set to your actual domains (not localhost)
- [ ] Database connection string uses a dedicated DB user (not sa/admin)
- [ ] `wwwroot/uploads` directory exists with proper write permissions
- [ ] Swagger UI is disabled in production (or password-protected)
- [ ] Error details are off in production (`ASPNETCORE_ENVIRONMENT=Production`)
- [ ] Database backups are scheduled

---

## 🔧 Troubleshooting

### "Email not found" on first login
→ DB not seeded yet. Run `dotnet run` once — BCrypt re-hash runs on startup automatically.

### "Invalid JWT" errors
→ Check that `Jwt:Key` in appsettings matches what was used to generate the token.
→ After changing the key, all existing tokens become invalid (users must re-login).

### SignalR won't connect
→ Make sure Nginx has `proxy_http_version 1.1` and WebSocket upgrade headers set.
→ Check CORS — SignalR needs `AllowCredentials()` which requires explicit origins (no wildcard).

### Upload images not showing
→ Check `wwwroot/uploads` folder exists and API process has write permission.
→ Check `FileStorage:BaseUrl` matches your actual domain.
