using Microsoft.EntityFrameworkCore;
using TenXConvo.Domain.Entities;

namespace TenXConvo.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // AUTH
    public DbSet<AppUser>             Users            { get; set; }
    public DbSet<RefreshToken>         RefreshTokens       { get; set; }
    public DbSet<ExternalLogin>        ExternalLogins      { get; set; }
    
    // ── Credit & Payment System ──
    public DbSet<CreditBalance>        CreditBalances      { get; set; }
    public DbSet<CreditTransaction>    CreditTransactions  { get; set; }
    public DbSet<PaymentTransaction>   PaymentTransactions { get; set; }
    public DbSet<MessagePricing>       MessagePricings     { get; set; }
    public DbSet<ConsultantServiceConfig> ConsultantServiceConfigs { get; set; }
    public DbSet<Invoice>              Invoices            { get; set; }
    public DbSet<PasswordResetToken>   PasswordResetTokens { get; set; }
    public DbSet<UserLoginPreference> LoginPreferences { get; set; }
    public DbSet<FiscalYear>          FiscalYears      { get; set; }

    // SYSTEM
    public DbSet<ErrorLog>    ErrorLogs      { get; set; }
    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<WebPushToken>  WebPushTokens  { get; set; }

    // DATA → CONSTANT
    public DbSet<ControlType>     ControlTypes      { get; set; }
    public DbSet<ControlCategory> ControlCategories { get; set; }
    public DbSet<ClientArea>      ClientAreas       { get; set; }
    public DbSet<ClientCategory>  ClientCategories  { get; set; }
    public DbSet<LocationType>    LocationTypes     { get; set; }

    // DATA → MAPPING
    public DbSet<DocumentType>    DocumentTypes    { get; set; }
    public DbSet<DocumentMovement> DocumentMovements { get; set; }
    public DbSet<CriteriaType>    CriteriaTypes    { get; set; }
    public DbSet<CriteriaSubType> CriteriaSubTypes { get; set; }
    public DbSet<Currency>        Currencies       { get; set; }

    // DATA → GEOGRAPHY
    public DbSet<Country>  Countries { get; set; }
    public DbSet<Province> Provinces { get; set; }
    public DbSet<City>     Cities    { get; set; }
    public DbSet<District> Districts { get; set; }
    public DbSet<Tehsil>   Tehsils   { get; set; }
    public DbSet<Area>     Areas     { get; set; }

    // SETUP
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Location>     Locations     { get; set; }

    // USER MANAGEMENT
    public DbSet<AppRole>        Roles           { get; set; }
    public DbSet<RoleModule>     RoleModules     { get; set; }
    public DbSet<RoleMenuEntry>  RoleMenus       { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    // TEMPLATES
    public DbSet<WaTemplate>    WaTemplates    { get; set; }
    public DbSet<SmsTemplate>   SmsTemplates   { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<WebTemplate>   WebTemplates   { get; set; }

    // NOTIFICATIONS
    public DbSet<WaNotification>        WaNotifications        { get; set; }
    public DbSet<SmsNotification>       SmsNotifications       { get; set; }
    public DbSet<EmailNotification>     EmailNotifications     { get; set; }
    public DbSet<WebNotification>       WebNotifications       { get; set; }
    public DbSet<AppNotification>       AppNotifications       { get; set; }
    public DbSet<AppNotificationTarget> AppNotificationTargets { get; set; }

    // SETTINGS
    public DbSet<ClientSettings> ClientSettings { get; set; }

    // CONSULTANT + CUSTOMER PORTAL
    public DbSet<ConsultantProfile>      ConsultantProfiles   { get; set; }
    public DbSet<ConsultantAvailability> ConsultantAvailabilities { get; set; }
    public DbSet<CustomerProfile>        CustomerProfiles     { get; set; }
    public DbSet<ClientConnection>       ClientConnections    { get; set; }
    public DbSet<Conversation>           Conversations        { get; set; }
    public DbSet<Message>                Messages             { get; set; }
    public DbSet<ConsultantReview>       ConsultantReviews    { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ══════════════════════════════════════════════════════════════════════
        //  EF CONFIGURATIONS — FK relationships
        // ══════════════════════════════════════════════════════════════════════

        // ── AUTH ──────────────────────────────────────────────────────────────
        mb.Entity<AppUser>(e => {
            e.HasIndex(x => x.LoginId).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.Users)
             .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<RefreshToken>(e => {
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ExternalLogin>(e => {
            e.HasOne(x => x.User).WithMany(u => u.ExternalLogins)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.Provider, x.ProviderKey }).IsUnique();
        });

        mb.Entity<UserLoginPreference>(e => {
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Location).WithMany()
             .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.FiscalYear).WithMany()
             .HasForeignKey(x => x.FiscalYearId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── SYSTEM ────────────────────────────────────────────────────────────
        mb.Entity<Configuration>(e => { e.HasKey(x => x.Key); });

        mb.Entity<WebPushToken>(e => {
            e.HasOne(x => x.User).WithMany(u => u.PushTokens)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── DATA → CONSTANT ───────────────────────────────────────────────────
        mb.Entity<ControlCategory>(e => {
            e.HasOne(x => x.ControlType).WithMany(t => t.Categories)
             .HasForeignKey(x => x.ControlTypeId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<LocationType>(e => {
            e.HasMany(x => x.Locations).WithOne(l => l.LocationType)
             .HasForeignKey(l => l.LocationTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── DATA → MAPPING ────────────────────────────────────────────────────
        mb.Entity<CriteriaSubType>(e => {
            e.HasOne(x => x.CriteriaType).WithMany(t => t.SubTypes)
             .HasForeignKey(x => x.CriteriaTypeId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── DATA → GEOGRAPHY ──────────────────────────────────────────────────
        mb.Entity<Province>(e => {
            e.HasOne(x => x.Country).WithMany(c => c.Provinces)
             .HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<City>(e => {
            e.HasOne(x => x.Province).WithMany(p => p.Cities)
             .HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<District>(e => {
            e.HasOne(x => x.City).WithMany(c => c.Districts)
             .HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<Tehsil>(e => {
            e.HasOne(x => x.District).WithMany(d => d.Tehsils)
             .HasForeignKey(x => x.DistrictId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<Area>(e => {
            e.HasOne(x => x.City).WithMany(c => c.Areas)
             .HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── USER MANAGEMENT ───────────────────────────────────────────────────
        mb.Entity<RoleModule>(e => {
            e.HasOne(x => x.Role).WithMany(r => r.Modules)
             .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Location).WithMany()
             .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<RoleMenuEntry>(e => {
            e.HasOne(x => x.Role).WithMany(r => r.MenuEntries)
             .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Location).WithMany()
             .HasForeignKey(x => x.LocationId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });
        mb.Entity<UserPermission>(e => {
            e.HasOne(x => x.User).WithMany(u => u.Permissions)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Location).WithMany()
             .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── NOTIFICATIONS ─────────────────────────────────────────────────────
        mb.Entity<AppNotificationTarget>(e => {
            e.HasOne(x => x.Notification).WithMany(n => n.Targets)
             .HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── CLIENT SETTINGS ───────────────────────────────────────────────────
        mb.Entity<ClientSettings>(e => {
            e.HasOne(x => x.ConsultantDefaultRole).WithMany()
             .HasForeignKey(x => x.ConsultantDefaultRoleId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ClientDefaultRole).WithMany()
             .HasForeignKey(x => x.ClientDefaultRoleId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── CONSULTANT PROFILE ────────────────────────────────────────────────
        mb.Entity<ConsultantProfile>(e => {
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(100);
        });

        // ── CONSULTANT AVAILABILITY ───────────────────────────────────────────
        mb.Entity<ConsultantAvailability>(e => {
            e.HasOne(x => x.Consultant).WithMany(c => c.Availabilities)
             .HasForeignKey(x => x.ConsultantId).OnDelete(DeleteBehavior.Cascade);
            // SQLite has no native TimeOnly type — store as "HH:mm" string
            e.Property(x => x.StartTime)
             .HasConversion(
                v => v.ToString("HH:mm"),
                v => TimeOnly.Parse(v));
            e.Property(x => x.EndTime)
             .HasConversion(
                v => v.ToString("HH:mm"),
                v => TimeOnly.Parse(v));
        });

        // ── CUSTOMER PROFILE ──────────────────────────────────────────────────
        mb.Entity<CustomerProfile>(e => {
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId).IsUnique();
        });

        // ── CLIENT CONNECTION ─────────────────────────────────────────────────
        mb.Entity<ClientConnection>(e => {
            e.HasOne(x => x.Consultant).WithMany(c => c.Connections)
             .HasForeignKey(x => x.ConsultantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany(c => c.Connections)
             .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ConsultantId, x.CustomerId });
        });

        // ── CONVERSATION ──────────────────────────────────────────────────────
        mb.Entity<Conversation>(e => {
            e.HasOne(x => x.Consultant).WithMany(c => c.Conversations)
             .HasForeignKey(x => x.ConsultantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany(c => c.Conversations)
             .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── MESSAGE ───────────────────────────────────────────────────────────
        mb.Entity<Message>(e => {
            e.HasOne(x => x.Conversation).WithMany(c => c.Messages)
             .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Sender).WithMany()
             .HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ConversationId);
            e.HasIndex(x => x.SentAt);
        });

        // ── CONSULTANT REVIEW ─────────────────────────────────────────────────
        mb.Entity<ConsultantReview>(e => {
            e.HasOne(x => x.Consultant).WithMany()
             .HasForeignKey(x => x.ConsultantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany(c => c.Reviews)
             .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ConsultantId, x.CustomerId }).IsUnique();
        });

        // ══════════════════════════════════════════════════════════════════════
        //  SEED DATA
        // ══════════════════════════════════════════════════════════════════════
        // PasswordResetToken
        mb.Entity<PasswordResetToken>(e => {
            e.HasKey(t => t.Id);
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => t.Token);
            e.HasIndex(t => new { t.UserId, t.IsUsed });
        });

        SeedData(mb);
    }

    private static void SeedData(ModelBuilder mb)
    {
        // ── FISCAL YEARS ──────────────────────────────────────────────────────
        var fy1 = new FiscalYear { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), Name = "Financial Year 2025-2026", StartDate = new DateTime(2025,7,1), EndDate = new DateTime(2026,6,30), IsActive = true,  IsCurrent = false, CreatedAt = new DateTime(2025,7,1) };
        var fy2 = new FiscalYear { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), Name = "Financial Year 2026-2027", StartDate = new DateTime(2026,7,1), EndDate = new DateTime(2027,6,30), IsActive = true,  IsCurrent = true,  CreatedAt = new DateTime(2026,1,1) };
        mb.Entity<FiscalYear>().HasData(fy1, fy2);

        // ── ROLES ─────────────────────────────────────────────────────────────
        var roleAdmin      = new AppRole { Id = Guid.Parse("22222222-0000-0000-0000-000000000001"), RoleName = "Admin Role",      CreatedOn = new DateTime(2025,6,25) };
        var roleConsultant = new AppRole { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), RoleName = "Consultant Role", CreatedOn = new DateTime(2025,6,25) };
        var roleClient     = new AppRole { Id = Guid.Parse("22222222-0000-0000-0000-000000000003"), RoleName = "Client Role",     CreatedOn = new DateTime(2025,6,25) };
        var roleWeb        = new AppRole { Id = Guid.Parse("22222222-0000-0000-0000-000000000004"), RoleName = "Web Role",        CreatedOn = new DateTime(2025,6,25) };
        mb.Entity<AppRole>().HasData(roleAdmin, roleConsultant, roleClient, roleWeb);

        // ── LOCATION TYPES ────────────────────────────────────────────────────
        var ltHO  = new LocationType { Id = Guid.Parse("33333333-0000-0000-0000-000000000001"), LocationTypeName = "Head Office",          ShortName = "HO",  CreatedOn = new DateTime(2025,5,23) };
        var ltRHO = new LocationType { Id = Guid.Parse("33333333-0000-0000-0000-000000000002"), LocationTypeName = "Regional Head Office", ShortName = "RHO", CreatedOn = new DateTime(2025,5,23) };
        var ltBRH = new LocationType { Id = Guid.Parse("33333333-0000-0000-0000-000000000003"), LocationTypeName = "Branch",               ShortName = "BRH", CreatedOn = new DateTime(2025,5,23) };
        var ltSTR = new LocationType { Id = Guid.Parse("33333333-0000-0000-0000-000000000004"), LocationTypeName = "Stores",               ShortName = "STR", CreatedOn = new DateTime(2025,5,23) };
        var ltIND = new LocationType { Id = Guid.Parse("33333333-0000-0000-0000-000000000005"), LocationTypeName = "Individual",           ShortName = "IND", CreatedOn = new DateTime(2025,5,23) };
        var ltSL  = new LocationType { Id = Guid.Parse("33333333-0000-0000-0000-000000000006"), LocationTypeName = "Site Loc",             ShortName = "SL",  CreatedOn = new DateTime(2024,9,18) };
        mb.Entity<LocationType>().HasData(ltHO, ltRHO, ltBRH, ltSTR, ltIND, ltSL);

        // ── LOCATIONS (Login Step 2 dropdown) ────────────────────────────────
        var locHead   = new Location { Id = Guid.Parse("44444444-0000-0000-0000-000000000001"), LocationName = "Head Office",   LocationTypeId = ltHO.Id,  LocationAddress = "Address, Lahore, Pakistan", IsActive = true, CreatedOn = new DateTime(2025,5,23) };
        var locMultan = new Location { Id = Guid.Parse("44444444-0000-0000-0000-000000000002"), LocationName = "Multan Office", LocationTypeId = ltRHO.Id, LocationAddress = "Nawa Sheher, Multan",       IsActive = true, CreatedOn = new DateTime(2025,5,23) };
        mb.Entity<Location>().HasData(locHead, locMultan);

        // ══════════════════════════════════════════════════════════════════════
        //  ADMIN USER SEED
        //  Email    : admin@htag.mhm
        //  Password : Admin@123
        //  Role     : Admin Role
        //  !! Change password after first login !!
        // ══════════════════════════════════════════════════════════════════════
        var adminUserId = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000001");
        mb.Entity<AppUser>().HasData(new AppUser
        {
            Id           = adminUserId,
            UserName     = "System Administrator",
            LoginId      = "admin@htag.mhm",
            Email        = "admin@htag.mhm",
            // BCrypt hash of "Admin@123"
            PasswordHash = "$2a$11$rBnzgDSuFJDgBXJzxl4aVOzQqRaW6YhXJvDPFi9GVQ0g7TlFp2vJq",
            CellNo       = "+923248999135",
            RoleId       = roleAdmin.Id,
            IsActive     = true,
            CreatedAt    = new DateTime(2025,6,25)
        });

        // ── CONSULTANT USER (for testing consultant portal) ───────────────────
        var consultantUserId = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000002");
        mb.Entity<AppUser>().HasData(new AppUser
        {
            Id           = consultantUserId,
            UserName     = "Ali Consultant",
            LoginId      = "ali@htag.mhm",
            Email        = "ali@htag.mhm",
            // BCrypt hash of "Test@123"
            PasswordHash = "$2a$11$rBnzgDSuFJDgBXJzxl4aVOzQqRaW6YhXJvDPFi9GVQ0g7TlFp2vJq",
            CellNo       = "+923001234567",
            RoleId       = roleConsultant.Id,
            IsActive     = true,
            CreatedAt    = new DateTime(2025,6,25)
        });

        // ── CLIENT USER (for testing user portal) ─────────────────────────────
        var clientUserId = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000003");
        mb.Entity<AppUser>().HasData(new AppUser
        {
            Id           = clientUserId,
            UserName     = "Sara Client",
            LoginId      = "sara@htag.mhm",
            Email        = "sara@htag.mhm",
            // BCrypt hash of "Test@123"
            PasswordHash = "$2a$11$rBnzgDSuFJDgBXJzxl4aVOzQqRaW6YhXJvDPFi9GVQ0g7TlFp2vJq",
            CellNo       = "+923007654321",
            RoleId       = roleClient.Id,
            IsActive     = true,
            CreatedAt    = new DateTime(2025,6,25)
        });

        // ── CONSULTANT PROFILE (for testing public directory) ─────────────────
        mb.Entity<ConsultantProfile>().HasData(new ConsultantProfile
        {
            Id             = Guid.Parse("BBBBBBBB-0000-0000-0000-000000000001"),
            UserId         = consultantUserId,
            Slug           = "ali-khan",
            Bio            = "Strategic Business Consulting — 10+ years of experience helping businesses grow.",
            IsOnline       = false,
            IsPublic       = true,
            JoinedDate     = new DateTime(2025,6,25),
            Specialization = "Business Strategy",
            Experience     = "10+ years",
            HourlyRate     = 5000,
            Timezone       = "PKT (UTC+5)"
        });

        // ── CREDIT & PAYMENT SYSTEM ─────────────────────────────────────────────

        mb.Entity<CreditBalance>(e => {
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId).IsUnique();
        });

        mb.Entity<CreditTransaction>(e => {
            e.HasOne(x => x.Balance).WithMany(b => b.Transactions)
             .HasForeignKey(x => x.CreditBalanceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Payment).WithMany(p => p.CreditTransactions)
             .HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.CreatedAt);
        });

        mb.Entity<PaymentTransaction>(e => {
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TransactionRef).IsUnique();
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });

        mb.Entity<MessagePricing>(e => {
            e.HasIndex(x => x.MessageType).IsUnique();
            e.Property(x => x.PricePerUnit).HasPrecision(18, 2);
        });

        mb.Entity<ConsultantServiceConfig>(e => {
            e.HasOne(x => x.Consultant).WithMany()
             .HasForeignKey(x => x.ConsultantUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ConsultantUserId).IsUnique();
            e.Property(x => x.TextRate).HasPrecision(18, 2);
            e.Property(x => x.AudioRate).HasPrecision(18, 2);
            e.Property(x => x.VideoRate).HasPrecision(18, 2);
            e.Property(x => x.ImageRate).HasPrecision(18, 2);
            e.Property(x => x.FileRate).HasPrecision(18, 2);
        });

        mb.Entity<Invoice>(e => {
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Payment).WithMany()
             .HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.Tax).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
        });

        // Default pricing seed data — admin can change from dashboard
        mb.Entity<MessagePricing>().HasData(
            new MessagePricing { Id = Guid.Parse("CCCC0001-0000-0000-0000-000000000001"), MessageType = "text",  UnitType = "characters", PricePerUnit = 5.00m,  UnitSize = 250, Currency = "USD", Description = "Text: $5 per 250 characters" },
            new MessagePricing { Id = Guid.Parse("CCCC0001-0000-0000-0000-000000000002"), MessageType = "audio", UnitType = "minutes",    PricePerUnit = 10.00m, UnitSize = 1,   Currency = "USD", Description = "Audio: $10 per minute" },
            new MessagePricing { Id = Guid.Parse("CCCC0001-0000-0000-0000-000000000003"), MessageType = "video", UnitType = "minutes",    PricePerUnit = 15.00m, UnitSize = 1,   Currency = "USD", Description = "Video: $15 per minute" },
            new MessagePricing { Id = Guid.Parse("CCCC0001-0000-0000-0000-000000000004"), MessageType = "image", UnitType = "count",      PricePerUnit = 10.00m, UnitSize = 1,   Currency = "USD", Description = "Image: $10 per image" },
            new MessagePricing { Id = Guid.Parse("CCCC0001-0000-0000-0000-000000000005"), MessageType = "file",  UnitType = "count",      PricePerUnit = 5.00m,  UnitSize = 1,   Currency = "USD", Description = "File: $5 per file" }
        );

        // ── ORGANIZATION ──────────────────────────────────────────────────────
        mb.Entity<Organization>().HasData(new Organization {
            Id             = Guid.Parse("55555555-0000-0000-0000-000000000001"),
            ClientName     = "HTAG Private Limited",
            ClientArea     = "Software House",
            ClientGroup    = "HTAG Groups of Company",
            Currency       = "Pakistani Rupee",
            CurrencySymbol = "PKR",
            Email          = "hamidsense@hotmail.com",
            ContactPerson  = "Abdul Hamid",
            CellNo         = "+923248999135",
            Website        = "htagsol.com",
            NTN            = "A447939",
            STRN           = "32-77-8763-092-73",
            UpdatedAt      = new DateTime(2026,3,5)
        });

        // ── CONTROL TYPES ─────────────────────────────────────────────────────
        var ctVoucher  = new ControlType { Id = Guid.Parse("66660001-0000-0000-0000-000000000001"), ControlTypeName = "Voucher",  ControlTypePrefix = "VCHCTL", CreatedOn = new DateTime(2025,6,25) };
        var ctCash     = new ControlType { Id = Guid.Parse("66660001-0000-0000-0000-000000000002"), ControlTypeName = "Cash",     ControlTypePrefix = "CSHCTL", CreatedOn = new DateTime(2025,6,25) };
        var ctBank     = new ControlType { Id = Guid.Parse("66660001-0000-0000-0000-000000000003"), ControlTypeName = "Bank",     ControlTypePrefix = "BNKCTL", CreatedOn = new DateTime(2025,6,25) };
        var ctStudent  = new ControlType { Id = Guid.Parse("66660001-0000-0000-0000-000000000004"), ControlTypeName = "Student",  ControlTypePrefix = "STDCTL", CreatedOn = new DateTime(2025,6,25) };
        var ctParty    = new ControlType { Id = Guid.Parse("66660001-0000-0000-0000-000000000005"), ControlTypeName = "Party",    ControlTypePrefix = "PTYCTL", CreatedOn = new DateTime(2025,6,25) };
        var ctEmployee = new ControlType { Id = Guid.Parse("66660001-0000-0000-0000-000000000006"), ControlTypeName = "Employee", ControlTypePrefix = "EMPCTL", CreatedOn = new DateTime(2025,6,25) };
        mb.Entity<ControlType>().HasData(ctVoucher, ctCash, ctBank, ctStudent, ctParty, ctEmployee);

        // ── CONTROL CATEGORIES ────────────────────────────────────────────────
        mb.Entity<ControlCategory>().HasData(
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000001"), ControlTypeId = ctParty.Id,   ControlCategoryName = "Client",           ControlPrefix = "CLTPTY", CreatedOn = new DateTime(2026,2,25) },
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000002"), ControlTypeId = ctBank.Id,    ControlCategoryName = "Card",             ControlPrefix = "CRDBCT", CreatedOn = new DateTime(2026,2,25) },
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000003"), ControlTypeId = ctBank.Id,    ControlCategoryName = "Wallet / Mobile",  ControlPrefix = "WALBCT", CreatedOn = new DateTime(2026,2,25) },
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000004"), ControlTypeId = ctVoucher.Id, ControlCategoryName = "Advance",          ControlPrefix = "ADVVCH", CreatedOn = new DateTime(2025,9,1) },
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000005"), ControlTypeId = ctVoucher.Id, ControlCategoryName = "Scholarships",     ControlPrefix = "SLSVCH", CreatedOn = new DateTime(2025,8,17) },
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000006"), ControlTypeId = ctStudent.Id, ControlCategoryName = "Corporate Student", ControlPrefix = "CRPSTD", CreatedOn = new DateTime(2025,8,17) },
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000007"), ControlTypeId = ctVoucher.Id, ControlCategoryName = "Security",         ControlPrefix = "SECVCH", CreatedOn = new DateTime(2025,6,25) },
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000008"), ControlTypeId = ctVoucher.Id, ControlCategoryName = "Tuition",          ControlPrefix = "TUTVCH", CreatedOn = new DateTime(2025,6,25) },
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000009"), ControlTypeId = ctCash.Id,    ControlCategoryName = "Cash On Delivery",  ControlPrefix = "CODCCT", CreatedOn = new DateTime(2025,6,25) },
            new ControlCategory { Id = Guid.Parse("66660002-0000-0000-0000-000000000010"), ControlTypeId = ctCash.Id,    ControlCategoryName = "Cash on Counter",   ControlPrefix = "CSHCCT", CreatedOn = new DateTime(2025,6,25) }
        );

        // ── CLIENT AREAS ──────────────────────────────────────────────────────
        mb.Entity<ClientArea>().HasData(
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000001"), ControlAreaName = "Packaging Trading",           ControlAreaPrefix = "PTD", CreatedOn = new DateTime(2025,11,27) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000002"), ControlAreaName = "Florists & Decoration",        ControlAreaPrefix = "FLR", CreatedOn = new DateTime(2025,11,12) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000003"), ControlAreaName = "Halwajat & Traditional Sweets", ControlAreaPrefix = "HAL", CreatedOn = new DateTime(2025,11,12) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000004"), ControlAreaName = "Handicrafts & Cultural Goods",  ControlAreaPrefix = "HND", CreatedOn = new DateTime(2025,11,12) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000005"), ControlAreaName = "Religious Items & Services",    ControlAreaPrefix = "REL", CreatedOn = new DateTime(2025,11,12) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000006"), ControlAreaName = "Luxury & Premium Goods",       ControlAreaPrefix = "LUP", CreatedOn = new DateTime(2025,11,12) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000007"), ControlAreaName = "Gaming & Esports",             ControlAreaPrefix = "GME", CreatedOn = new DateTime(2025,11,12) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000008"), ControlAreaName = "Media & Broadcasting",         ControlAreaPrefix = "MDB", CreatedOn = new DateTime(2025,11,12) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000009"), ControlAreaName = "Fisheries & Aquaculture",      ControlAreaPrefix = "FSA", CreatedOn = new DateTime(2025,11,12) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000010"), ControlAreaName = "Poultry & Meat Processing",    ControlAreaPrefix = "PLA", CreatedOn = new DateTime(2025,11,12) },
            new ClientArea { Id = Guid.Parse("66660003-0000-0000-0000-000000000011"), ControlAreaName = "Software House",               ControlAreaPrefix = "SFT", CreatedOn = new DateTime(2025,11,12) }
        );

        // ── CLIENT CATEGORIES ─────────────────────────────────────────────────
        mb.Entity<ClientCategory>().HasData(
            new ClientCategory { Id = Guid.Parse("66660004-0000-0000-0000-000000000001"), ControlCategoryName = "Online Trading", ControlCategoryPrefix = "OT",      CreatedOn = new DateTime(2025,10,22) },
            new ClientCategory { Id = Guid.Parse("66660004-0000-0000-0000-000000000002"), ControlCategoryName = "Shaheer",       ControlCategoryPrefix = "Shaheer",  CreatedOn = new DateTime(2025,6,14) },
            new ClientCategory { Id = Guid.Parse("66660004-0000-0000-0000-000000000003"), ControlCategoryName = "Distributer",   ControlCategoryPrefix = "DST",      CreatedOn = new DateTime(2025,4,5) },
            new ClientCategory { Id = Guid.Parse("66660004-0000-0000-0000-000000000004"), ControlCategoryName = "Wholesaler",    ControlCategoryPrefix = "WSL",      CreatedOn = new DateTime(2024,8,31) },
            new ClientCategory { Id = Guid.Parse("66660004-0000-0000-0000-000000000005"), ControlCategoryName = "Traders",       ControlCategoryPrefix = "TRD",      CreatedOn = new DateTime(2024,8,31) },
            new ClientCategory { Id = Guid.Parse("66660004-0000-0000-0000-000000000006"), ControlCategoryName = "Retailer",      ControlCategoryPrefix = "RTL",      CreatedOn = new DateTime(2024,8,31) },
            new ClientCategory { Id = Guid.Parse("66660004-0000-0000-0000-000000000007"), ControlCategoryName = "Manufacturer",  ControlCategoryPrefix = "MNF",      CreatedOn = new DateTime(2024,8,31) },
            new ClientCategory { Id = Guid.Parse("66660004-0000-0000-0000-000000000008"), ControlCategoryName = "Service Provider", ControlCategoryPrefix = "SRV",   CreatedOn = new DateTime(2024,8,31) }
        );

        // ── DOCUMENT TYPES ────────────────────────────────────────────────────
        mb.Entity<DocumentType>().HasData(
            new DocumentType { Id = Guid.Parse("66660005-0000-0000-0000-000000000001"), DocumentTypeName = "Product Web Link", ShortName = "PRDWEL", CreatedOn = new DateTime(2025,8,16) },
            new DocumentType { Id = Guid.Parse("66660005-0000-0000-0000-000000000002"), DocumentTypeName = "Inward",          ShortName = "INWTYP", CreatedOn = new DateTime(2025,6,25) },
            new DocumentType { Id = Guid.Parse("66660005-0000-0000-0000-000000000003"), DocumentTypeName = "Service Pricing", ShortName = "SERPRC", CreatedOn = new DateTime(2025,6,25) },
            new DocumentType { Id = Guid.Parse("66660005-0000-0000-0000-000000000004"), DocumentTypeName = "Product Reg",     ShortName = "PRDREG", CreatedOn = new DateTime(2025,6,25) },
            new DocumentType { Id = Guid.Parse("66660005-0000-0000-0000-000000000005"), DocumentTypeName = "Product Model",   ShortName = "PRDMOD", CreatedOn = new DateTime(2025,6,25) },
            new DocumentType { Id = Guid.Parse("66660005-0000-0000-0000-000000000006"), DocumentTypeName = "Employee",        ShortName = "EMPTYP", CreatedOn = new DateTime(2025,6,25) },
            new DocumentType { Id = Guid.Parse("66660005-0000-0000-0000-000000000007"), DocumentTypeName = "Product Area",    ShortName = "PRDARA", CreatedOn = new DateTime(2025,6,25) }
        );

        // ── CRITERIA TYPES ────────────────────────────────────────────────────
        var ctNtfTyp = new CriteriaType { Id = Guid.Parse("66660007-0000-0000-0000-000000000001"), CriteriaTypeName = "Notification Type",       CriteriaTypePrefix = "NTFTYP", CreatedOn = new DateTime(2026,2,23) };
        var ctNtgTyp = new CriteriaType { Id = Guid.Parse("66660007-0000-0000-0000-000000000002"), CriteriaTypeName = "Notification Target Type", CriteriaTypePrefix = "NTGTYP", CreatedOn = new DateTime(2026,2,23) };
        var ctBank2  = new CriteriaType { Id = Guid.Parse("66660007-0000-0000-0000-000000000010"), CriteriaTypeName = "Bank",                     CriteriaTypePrefix = "BNKTYP", CreatedOn = new DateTime(2025,6,25) };
        mb.Entity<CriteriaType>().HasData(ctNtfTyp, ctNtgTyp, ctBank2);

        // ── CRITERIA SUB TYPES (Banks) ────────────────────────────────────────
        mb.Entity<CriteriaSubType>().HasData(
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000001"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "Summit Bank",         Prefix = "SMTBNK",  CreatedOn = new DateTime(2025,6,25) },
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000002"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "National Bank of Pakistan", Prefix = "NBIBNK", CreatedOn = new DateTime(2025,6,25) },
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000003"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "Bank Alfalah",        Prefix = "BAFIBNK", CreatedOn = new DateTime(2025,6,25) },
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000004"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "HBL",                 Prefix = "HBLIBNK", CreatedOn = new DateTime(2025,6,25) },
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000005"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "UBL",                 Prefix = "UBLIBNK", CreatedOn = new DateTime(2025,6,25) },
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000006"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "MCB Bank",            Prefix = "MCBIBNK", CreatedOn = new DateTime(2025,6,25) },
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000007"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "Bank of Punjab",      Prefix = "BOPIBNK", CreatedOn = new DateTime(2025,6,25) },
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000008"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "Meezan Bank",         Prefix = "MEZBNK",  CreatedOn = new DateTime(2025,6,25) },
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000009"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "Faysal Bank",         Prefix = "FAYBNK",  CreatedOn = new DateTime(2025,6,25) },
            new CriteriaSubType { Id = Guid.Parse("66660008-0000-0000-0000-000000000010"), CriteriaTypeId = ctBank2.Id, SubCriteriaName = "Sindh Bank Limited",  Prefix = "SNDBNK",  CreatedOn = new DateTime(2025,6,25) }
        );

        // ── CURRENCIES ────────────────────────────────────────────────────────
        mb.Entity<Currency>().HasData(
            new Currency { Id = Guid.Parse("66660009-0000-0000-0000-000000000001"), CountryName = "Kuwait",                   CurrencyName = "Kuwaiti Dinar",   Symbol = "KWD", CreatedOn = new DateTime(2025,6,25) },
            new Currency { Id = Guid.Parse("66660009-0000-0000-0000-000000000002"), CountryName = "United States of America", CurrencyName = "US Dollar",       Symbol = "USD", CreatedOn = new DateTime(2025,6,25) },
            new Currency { Id = Guid.Parse("66660009-0000-0000-0000-000000000003"), CountryName = "European Union",          CurrencyName = "Euro",            Symbol = "EUR", CreatedOn = new DateTime(2025,6,25) },
            new Currency { Id = Guid.Parse("66660009-0000-0000-0000-000000000004"), CountryName = "Pakistan",                CurrencyName = "Pakistani Rupee", Symbol = "PKR", CreatedOn = new DateTime(2025,6,25) },
            new Currency { Id = Guid.Parse("66660009-0000-0000-0000-000000000005"), CountryName = "United Arab Emirates",    CurrencyName = "UAE Dirham",      Symbol = "AED", CreatedOn = new DateTime(2025,6,25) },
            new Currency { Id = Guid.Parse("66660009-0000-0000-0000-000000000006"), CountryName = "Saudi Arabia",            CurrencyName = "Saudi Riyal",     Symbol = "SAR", CreatedOn = new DateTime(2025,6,25) }
        );

        // ── GEOGRAPHY ─────────────────────────────────────────────────────────
        var cPak = new Country { Id = Guid.Parse("77770001-0000-0000-0000-000000000001"), CountryName = "Pakistan",                Code = "+92",  Prefix = "PAK", CreatedOn = new DateTime(2025,6,25) };
        var cUsa = new Country { Id = Guid.Parse("77770001-0000-0000-0000-000000000002"), CountryName = "United States of America", Code = "+1",   Prefix = "USA", CreatedOn = new DateTime(2025,6,25) };
        var cUae = new Country { Id = Guid.Parse("77770001-0000-0000-0000-000000000003"), CountryName = "United Arab Emirates",     Code = "+971", Prefix = "UAE", CreatedOn = new DateTime(2025,6,25) };
        mb.Entity<Country>().HasData(cPak, cUsa, cUae);

        var provPunjab = new Province { Id = Guid.Parse("77770002-0000-0000-0000-000000000001"), CountryId = cPak.Id, ProvinceName = "Punjab",               Prefix = "PNJ", CreatedOn = new DateTime(2025,6,25) };
        var provSindh  = new Province { Id = Guid.Parse("77770002-0000-0000-0000-000000000002"), CountryId = cPak.Id, ProvinceName = "Sindh",                Prefix = "SND", CreatedOn = new DateTime(2025,6,25) };
        var provKpk    = new Province { Id = Guid.Parse("77770002-0000-0000-0000-000000000003"), CountryId = cPak.Id, ProvinceName = "Khyber Pakhtunkhwa",   Prefix = "KPK", CreatedOn = new DateTime(2025,6,25) };
        var provBaloch = new Province { Id = Guid.Parse("77770002-0000-0000-0000-000000000004"), CountryId = cPak.Id, ProvinceName = "Balochistan",          Prefix = "BLC", CreatedOn = new DateTime(2025,6,25) };
        mb.Entity<Province>().HasData(provPunjab, provSindh, provKpk, provBaloch);

        var cityMultan  = new City { Id = Guid.Parse("77770003-0000-0000-0000-000000000001"), ProvinceId = provPunjab.Id, CityName = "Multan",    CreatedOn = new DateTime(2025,6,25) };
        var cityLahore  = new City { Id = Guid.Parse("77770003-0000-0000-0000-000000000002"), ProvinceId = provPunjab.Id, CityName = "Lahore",    CreatedOn = new DateTime(2025,6,25) };
        var cityKarachi = new City { Id = Guid.Parse("77770003-0000-0000-0000-000000000003"), ProvinceId = provSindh.Id,  CityName = "Karachi",   CreatedOn = new DateTime(2025,6,25) };
        var cityFsd     = new City { Id = Guid.Parse("77770003-0000-0000-0000-000000000004"), ProvinceId = provPunjab.Id, CityName = "Faisalabad", CreatedOn = new DateTime(2025,6,25) };
        var cityIsb     = new City { Id = Guid.Parse("77770003-0000-0000-0000-000000000005"), ProvinceId = provPunjab.Id, CityName = "Islamabad", CreatedOn = new DateTime(2025,6,25) };
        mb.Entity<City>().HasData(cityMultan, cityLahore, cityKarachi, cityFsd, cityIsb);

        mb.Entity<District>().HasData(
            new District { Id = Guid.Parse("77770004-0000-0000-0000-000000000001"), CityId = cityLahore.Id,  DistrictName = "Sheikhupura", CreatedOn = new DateTime(2025,6,25) },
            new District { Id = Guid.Parse("77770004-0000-0000-0000-000000000002"), CityId = cityLahore.Id,  DistrictName = "Lahore",      CreatedOn = new DateTime(2025,6,25) },
            new District { Id = Guid.Parse("77770004-0000-0000-0000-000000000003"), CityId = cityMultan.Id,  DistrictName = "Multan",      CreatedOn = new DateTime(2025,6,25) },
            new District { Id = Guid.Parse("77770004-0000-0000-0000-000000000004"), CityId = cityKarachi.Id, DistrictName = "Karachi",     CreatedOn = new DateTime(2025,6,25) }
        );

        mb.Entity<Tehsil>().HasData(
            new Tehsil { Id = Guid.Parse("77770005-0000-0000-0000-000000000001"), DistrictId = Guid.Parse("77770004-0000-0000-0000-000000000002"), TehsilName = "Lahore",      CreatedOn = new DateTime(2025,6,25) },
            new Tehsil { Id = Guid.Parse("77770005-0000-0000-0000-000000000002"), DistrictId = Guid.Parse("77770004-0000-0000-0000-000000000003"), TehsilName = "Multan City", CreatedOn = new DateTime(2025,6,25) }
        );

        mb.Entity<Area>().HasData(
            new Area { Id = Guid.Parse("77770006-0000-0000-0000-000000000001"), CityId = cityMultan.Id, AreaName = "TATEPUR",           CreatedOn = new DateTime(2025,6,25) },
            new Area { Id = Guid.Parse("77770006-0000-0000-0000-000000000002"), CityId = cityMultan.Id, AreaName = "SURAJ MIANI",       CreatedOn = new DateTime(2025,6,25) },
            new Area { Id = Guid.Parse("77770006-0000-0000-0000-000000000003"), CityId = cityMultan.Id, AreaName = "NEW MULTAN",        CreatedOn = new DateTime(2025,6,25) },
            new Area { Id = Guid.Parse("77770006-0000-0000-0000-000000000004"), CityId = cityMultan.Id, AreaName = "SECONDARY BOARD",   CreatedOn = new DateTime(2025,6,25) },
            new Area { Id = Guid.Parse("77770006-0000-0000-0000-000000000005"), CityId = cityLahore.Id, AreaName = "DHA Lahore",        CreatedOn = new DateTime(2025,6,25) },
            new Area { Id = Guid.Parse("77770006-0000-0000-0000-000000000006"), CityId = cityLahore.Id, AreaName = "Gulberg",           CreatedOn = new DateTime(2025,6,25) },
            new Area { Id = Guid.Parse("77770006-0000-0000-0000-000000000007"), CityId = cityKarachi.Id, AreaName = "Clifton",          CreatedOn = new DateTime(2025,6,25) },
            new Area { Id = Guid.Parse("77770006-0000-0000-0000-000000000008"), CityId = cityKarachi.Id, AreaName = "Saddar",           CreatedOn = new DateTime(2025,6,25) }
        );

        // ── CLIENT SETTINGS ───────────────────────────────────────────────────
        mb.Entity<ClientSettings>().HasData(new ClientSettings {
            Id                      = Guid.Parse("88880001-0000-0000-0000-000000000001"),
            IsWebsiteOnline         = true,
            FooterDescription       = "<p>Welcome to 10X Digital Ventures — connecting businesses with expert consultants.</p>",
            BusinessName            = "HTAG Private Limited",
            BusinessNature          = "Software House",
            BusinessProvince        = "Punjab",
            FbrToken                = "f0f725c3-e996-3083-990e-0ea630ce7d72",
            ValidationToken         = "75437efd-6e54-3f5d-b77a-039ade271ec4",
            ConsultantDefaultRoleId = roleConsultant.Id,
            ClientDefaultRoleId     = roleClient.Id,
            ChatLinkUrl             = "https://consultant.10xdigitalventures.com/login",
            UpdatedAt               = new DateTime(2026,3,5)
        });
    }
}
