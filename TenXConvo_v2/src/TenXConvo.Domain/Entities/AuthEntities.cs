namespace TenXConvo.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════════
//  AUTH ENTITIES — Shared across all 3 portals
//  admin.10xdigitalventures.com  |  consultant.10xdigitalventures.com  |  user.10xdigitalventures.com
//
//  2-step login:
//    Step 1 → email
//    Step 2 → password + Location + Connection (QA/Production) + FiscalYear
//  JWT role claim determines which portal routes are accessible
// ═══════════════════════════════════════════════════════════════════════════

public class AppUser
{
    public Guid      Id           { get; set; } = Guid.NewGuid();
    public string    UserName     { get; set; } = string.Empty;   // display name
    public string    LoginId      { get; set; } = string.Empty;   // name@htag.mhm
    public string    Email        { get; set; } = string.Empty;
    public string    PasswordHash { get; set; } = string.Empty;
    public string?   CellNo       { get; set; }
    public string?   ImageUrl     { get; set; }
    public Guid?     RoleId       { get; set; }
    public bool      IsActive     { get; set; } = true;
    public DateTime  CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt    { get; set; }

    // Navigation
    public AppRole?                    Role              { get; set; }
    public ConsultantProfile?          ConsultantProfile { get; set; }
    public CustomerProfile?            CustomerProfile   { get; set; }
    public ICollection<RefreshToken>   RefreshTokens     { get; set; } = new List<RefreshToken>();
    public ICollection<WebPushToken>   PushTokens        { get; set; } = new List<WebPushToken>();
    public ICollection<UserPermission> Permissions       { get; set; } = new List<UserPermission>();
    public ICollection<ExternalLogin>  ExternalLogins    { get; set; } = new List<ExternalLogin>();
}

// External OAuth logins linked to an AppUser (Google, Microsoft, etc.)
public class ExternalLogin
{
    public Guid     Id           { get; set; } = Guid.NewGuid();
    public Guid     UserId       { get; set; }
    public string   Provider     { get; set; } = string.Empty;   // "Google" | "Microsoft"
    public string   ProviderKey  { get; set; } = string.Empty;   // external user id
    public string?  Email        { get; set; }
    public string?  DisplayName  { get; set; }
    public string?  AvatarUrl    { get; set; }
    public DateTime LinkedAt     { get; set; } = DateTime.UtcNow;
    public AppUser  User         { get; set; } = null!;
}

// Roles: Consultant Role | Client Role | Web Role | Admin Role
public class AppRole
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public string   RoleName  { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public ICollection<AppUser>       Users       { get; set; } = new List<AppUser>();
    public ICollection<RoleModule>    Modules     { get; set; } = new List<RoleModule>();
    public ICollection<RoleMenuEntry> MenuEntries { get; set; } = new List<RoleMenuEntry>();
}

public class RefreshToken
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public Guid     UserId    { get; set; }
    public string   Token     { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool     IsRevoked { get; set; } = false;
    public string?  IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AppUser  User      { get; set; } = null!;
}

// Login Step 2 — user's remembered selections
public class UserLoginPreference
{
    public Guid      Id           { get; set; } = Guid.NewGuid();
    public Guid      UserId       { get; set; }
    public Guid?     LocationId   { get; set; }
    public Guid?     FiscalYearId { get; set; }
    public string?   Connection   { get; set; }  // "QA" | "Production"
    public bool      RememberMe   { get; set; } = false;
    public DateTime  UpdatedAt    { get; set; } = DateTime.UtcNow;
    public AppUser   User         { get; set; } = null!;
    public Location? Location     { get; set; }
    public FiscalYear? FiscalYear { get; set; }
}

// Financial Year 2026-2027 — selected at login step 2
public class FiscalYear
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public string   Name      { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate   { get; set; }
    public bool     IsActive  { get; set; } = true;
    public bool     IsCurrent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Password reset token — emailed to user, expires in 15 minutes
public class PasswordResetToken
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public Guid     UserId    { get; set; }
    public string   Token     { get; set; } = string.Empty;   // 6-digit OTP or UUID
    public DateTime ExpiresAt { get; set; }
    public bool     IsUsed    { get; set; } = false;
    public string?  IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AppUser  User      { get; set; } = null!;
}
