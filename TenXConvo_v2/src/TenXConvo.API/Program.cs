using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using TenXConvo.API.Controllers.Admin;
using TenXConvo.Application.Validators;
using TenXConvo.API.Hubs;
using TenXConvo.Application.Interfaces;
using TenXConvo.Infrastructure.Data;
using TenXConvo.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;
var env     = builder.Environment;



// ── DATABASE ──────────────────────────────────────────────────────────────────
var dbProvider = config["DatabaseProvider"] ?? "SQLite";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider == "SQLite")
        options.UseSqlite(config.GetConnectionString("Default"));
    else
        options.UseSqlServer(config.GetConnectionString("Default"));
});

// ── SERVICES ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthService,       AuthService>();
builder.Services.AddScoped<IAdminService,      AdminService>();
builder.Services.AddScoped<IConsultantService, ConsultantService>();
builder.Services.AddScoped<IUserService,       UserService>();

// ── NOTIFICATION DISPATCHER ──────────────────────────────────────────────────
builder.Services.AddScoped<NotificationDispatcher>();

// ── CREDIT / WALLET SERVICE ─────────────────────────────────────────────────
builder.Services.AddScoped<CreditService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddHttpClient("PayFast");
builder.Services.AddScoped<PayFastService>();
builder.Services.AddHttpClient("EasyPaisa");
builder.Services.AddScoped<EasyPaisaService>();
builder.Services.AddHttpClient("JazzCash");
builder.Services.AddScoped<JazzCashService>();

// ── FLUENT VALIDATION (manual — FluentValidation.AspNetCore deprecated) ─────
// Validators registered for DI injection; controllers call ValidateAsync() explicitly.
// AddFluentValidationAutoValidation() removed — it's in the abandoned .AspNetCore package.
builder.Services.AddValidatorsFromAssembly(
    typeof(TenXConvo.Application.Validators.LoginStep1Validator).Assembly,
    includeInternalTypes: true);

// ── RATE LIMITING — built-in .NET 7+ (no external package needed) ───────────
// Brute force protection on auth endpoints
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = 429;

    // Login Step1 — 10 requests per minute per IP
    opts.AddPolicy("login-step1", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "global",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Login Step2 — 5 requests per minute per IP
    opts.AddPolicy("login-step2", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "global",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Forgot password — 3 per 5 minutes per IP
    opts.AddPolicy("forgot-password", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "global",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(5),
                PermitLimit = 3,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Global default — 120 per minute
    opts.AddPolicy("global", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "global",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 120,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
// ── SIGNALR ───────────────────────────────────────────────────────────────────
var signalRBuilder = builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors       = env.IsDevelopment();
    o.MaximumReceiveMessageSize  = 32 * 1024;
    o.ClientTimeoutInterval      = TimeSpan.FromSeconds(60);
    o.KeepAliveInterval          = TimeSpan.FromSeconds(15);
});


var adminOrigins      = config.GetSection("Cors:AdminOrigins").Get<string[]>() ?? [];
var consultantOrigins = config.GetSection("Cors:ConsultantOrigins").Get<string[]>() ?? [];
var userOrigins       = config.GetSection("Cors:UserOrigins").Get<string[]>() ?? [];
var allOrigins        = adminOrigins.Concat(consultantOrigins).Concat(userOrigins).Distinct().ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminPortal", policy =>
    {
        policy.WithOrigins(adminOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    options.AddPolicy("ConsultantPortal", policy =>
    {
        policy.WithOrigins(consultantOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    options.AddPolicy("UserPortal", policy =>
    {
        policy.WithOrigins(userOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:3002",
            "http://localhost:3004"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
var redisConn = config["Redis:ConnectionString"];
var redisEnabled = config.GetValue<bool>("Redis:Enabled");

if (redisEnabled && !string.IsNullOrEmpty(redisConn))
{
    signalRBuilder.AddStackExchangeRedis(redisConn, opts =>
    {
        opts.Configuration.ChannelPrefix =
            RedisChannel.Literal(config["Redis:InstanceName"] ?? "TenXConvo_");
    });

    builder.Services.AddStackExchangeRedisCache(opts =>
    {
        opts.Configuration = redisConn;
        opts.InstanceName  = config["Redis:InstanceName"] ?? "TenXConvo_";
    });
}

// ── JWT + OAUTH ──────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer      = config["Jwt:Issuer"],
            ValidAudience    = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
            ClockSkew        = TimeSpan.FromSeconds(30), // tighter than default 5 min
        };

        // SignalR sends JWT in query string (not header) — handle that here
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path        = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    })
    // ── Google OAuth 2.0 (optional — enable via appsettings) ─────────────────
    .AddGoogle("Google", o =>
    {
        o.ClientId     = config["OAuth:Google:ClientId"]     ?? "NOT_SET";
        o.ClientSecret = config["OAuth:Google:ClientSecret"] ?? "NOT_SET";
        o.SaveTokens   = true;
    })
    // ── Microsoft OAuth 2.0 (optional — enable via appsettings) ──────────────
    .AddMicrosoftAccount("Microsoft", o =>
    {
        o.ClientId     = config["OAuth:Microsoft:ClientId"]     ?? "NOT_SET";
        o.ClientSecret = config["OAuth:Microsoft:ClientSecret"] ?? "NOT_SET";
        o.SaveTokens   = true;
    });

// ── AUTHORIZATION ─────────────────────────────────────────────────────────────
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminOnly",         p => p.RequireRole("Admin Role"));
    o.AddPolicy("ConsultantOnly",    p => p.RequireRole("Consultant Role"));
    o.AddPolicy("ClientOnly",        p => p.RequireRole("Client Role", "Web Role"));
    o.AddPolicy("AdminOrConsultant", p => p.RequireRole("Admin Role", "Consultant Role"));
    o.AddPolicy("AnyAuthenticated",  p => p.RequireRole("Admin Role","Consultant Role","Client Role","Web Role"));
});

// ── RESPONSE COMPRESSION ──────────────────────────────────────────────────────
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults
        .MimeTypes.Concat(["application/json", "text/plain"]);
});

// ── HEALTH CHECKS ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// ── AUTO DB CREATE + SEED ─────────────────────────────────────────────────────
{
    using var scope = app.Services.CreateScope();
    var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Production: apply pending migrations (creates DB + tables if needed)
    // Development: EnsureCreated for quick setup without migration files
    db.Database.EnsureCreated();

    // ── Fix BCrypt seed passwords at first run ──────────────────────────────
    // EF HasData seeds a hardcoded hash. At first startup we re-hash using the
    // real BCrypt library so every machine gets the correct hash.
    var seedEmails = new[] { "admin@htag.mhm", "ali@htag.mhm", "sara@htag.mhm" };
    var seedPasswords = new Dictionary<string, string>
    {
        ["admin@htag.mhm"] = "Admin@123",
        ["ali@htag.mhm"]   = "Test@123",
        ["sara@htag.mhm"]  = "Test@123",
    };

    bool anyFixed = false;
    foreach (var email in seedEmails)
    {
        var user = db.Users.FirstOrDefault(u => u.Email == email);
        if (user is null) continue;

        // BCrypt hashes always start with "$2a$" or "$2b$". The dummy hash
        // we seeded starts with "$2a$11$rBnz..." — re-hash on first real run.
        bool needsRehash = !BCrypt.Net.BCrypt.Verify(seedPasswords[email], user.PasswordHash);
        if (needsRehash)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPasswords[email], workFactor: 11);
            anyFixed = true;
            log.LogInformation("✅ Seed password re-hashed for {Email}", email);
        }
    }
    if (anyFixed) db.SaveChanges();
}

// ── wwwroot/uploads auto-create ───────────────────────────────────────────────
{
    var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads");
    Directory.CreateDirectory(uploadsPath);   // no-op if already exists
    Directory.CreateDirectory(Path.Combine(uploadsPath, "avatars"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "documents"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "logos"));
}

// ── JWT Key guard ─────────────────────────────────────────────────────────────
{
    var jwtKey = app.Configuration["Jwt:Key"] ?? "";
    if (jwtKey.Length < 32 || jwtKey.Contains("SECRET") || jwtKey.Contains("your-"))
        app.Logger.LogWarning("⚠️  JWT Key is weak or default! Set a strong key in appsettings before production.");
}



// ── MIDDLEWARE PIPELINE ───────────────────────────────────────────────────────
app.MapScalarApiReference(options =>
{
    options.Title           = "10X Convo API";
    options.Theme           = ScalarTheme.DeepSpace;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    options.Authentication = new ScalarAuthenticationOptions
{
    PreferredSecuritySchemes = ["Bearer"]
};
});  // UI at http://localhost:5000/scalar/v1

// ── HTTPS redirect (production only) ────────────────────────────────────────
if (!env.IsDevelopment())
{
    app.UseHsts();
    // app.UseHttpsRedirection();
}

// ── SECURITY HEADERS ─────────────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"]  = "nosniff";
    headers["X-Frame-Options"]         = "DENY";
    headers["X-XSS-Protection"]        = "1; mode=block";
    headers["Referrer-Policy"]         = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"]      = "camera=(), microphone=(), geolocation=()";
    if (!env.IsDevelopment())
        headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
    await next();
});

// ── STATIC FILES (with cache headers for production) ─────────────────────────
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache uploaded files (avatars, documents, logos) for 7 days
        // Browsers won't re-download them on every page load
        ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});
app.UseRouting();
app.UseCors(env.IsDevelopment() ? "Development" : "AllPortals");  // AllPortals = all 3 subdomains
app.UseRateLimiter();
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── HEALTH CHECK ──────────────────────────────────────────────────────────────
app.MapHealthChecks("/health");

// ── SIGNALR HUB ───────────────────────────────────────────────────────────────
// Frontend connects to: ws://localhost:5000/hubs/chat
app.MapHub<ChatHub>("/hubs/chat");

app.MapOpenApi();


app.Run();



