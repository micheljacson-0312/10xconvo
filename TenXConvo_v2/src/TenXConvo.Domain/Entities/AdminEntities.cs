namespace TenXConvo.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════════
//  ADMIN PORTAL ENTITIES
//  Portal: admin.10xdigitalventures.com
//  Role:   Admin Role
//  URL:    /ADM/...
// ═══════════════════════════════════════════════════════════════════════════

// ── SYSTEM ──────────────────────────────────────────────────────────────────

// ErrorLog: Sr, Action Name, Controller Name, Code, Error Message, Created On
// Auto-populated by ErrorHandlingMiddleware
public class ErrorLog
{
    public Guid     Id             { get; set; } = Guid.NewGuid();
    public string   ActionName     { get; set; } = string.Empty;
    public string   ControllerName { get; set; } = string.Empty;
    public int      Code           { get; set; } = 500;
    public string   ErrorMessage   { get; set; } = string.Empty;
    public string?  StackTrace     { get; set; }
    public string?  RequestPath    { get; set; }
    public string?  UserId         { get; set; }
    public DateTime CreatedOn      { get; set; } = DateTime.UtcNow;
}

// Configuration: key-value settings store
public class Configuration
{
    public string   Key       { get; set; } = string.Empty;  // PK
    public string   Value     { get; set; } = string.Empty;
    public string?  Category  { get; set; }
    public string?  Notes     { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// WebPushToken: Sr, User, Login ID, Device ID, Platform (WEB), Status, Send Message action
public class WebPushToken
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public Guid     UserId    { get; set; }
    public string   LoginId   { get; set; } = string.Empty;
    public string   DeviceId  { get; set; } = string.Empty;
    public string   Platform  { get; set; } = "WEB";
    public string   Token     { get; set; } = string.Empty;
    public bool     IsActive  { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public AppUser  User      { get; set; } = null!;
}

// ── DATA → CONSTANT ──────────────────────────────────────────────────────────

// ControlType: Voucher/VCHCTL, Cash/CSHCTL, Bank/BNKCTL, Student/STDCTL, Party/PTYCTL, Employee/EMPCTL
public class ControlType
{
    public Guid     Id                { get; set; } = Guid.NewGuid();
    public string   ControlTypeName   { get; set; } = string.Empty;
    public string   ControlTypePrefix { get; set; } = string.Empty;
    public DateTime CreatedOn         { get; set; } = DateTime.UtcNow;
    public ICollection<ControlCategory> Categories { get; set; } = new List<ControlCategory>();
}

// ControlCategory: Party→Client/CLTPTY, Bank→Card/CRDBCT, Voucher→Advance/ADVVCH...
public class ControlCategory
{
    public Guid        Id                  { get; set; } = Guid.NewGuid();
    public Guid        ControlTypeId       { get; set; }
    public string      ControlCategoryName { get; set; } = string.Empty;
    public string      ControlPrefix       { get; set; } = string.Empty;
    public DateTime    CreatedOn           { get; set; } = DateTime.UtcNow;
    public ControlType ControlType         { get; set; } = null!;
}

// ClientArea: Packaging Trading/PTD, Florists/FLR, Gaming/GME, Software House/SFT...
public class ClientArea
{
    public Guid     Id                { get; set; } = Guid.NewGuid();
    public string   ControlAreaName   { get; set; } = string.Empty;
    public string   ControlAreaPrefix { get; set; } = string.Empty;
    public DateTime CreatedOn         { get; set; } = DateTime.UtcNow;
}

// ClientCategory: Online Trading/OT, Distributer/DST, Retailer/RTL, Manufacturer/MNF...
public class ClientCategory
{
    public Guid     Id                    { get; set; } = Guid.NewGuid();
    public string   ControlCategoryName   { get; set; } = string.Empty;
    public string   ControlCategoryPrefix { get; set; } = string.Empty;
    public DateTime CreatedOn             { get; set; } = DateTime.UtcNow;
}

// LocationType: Head Office/HO, Regional Head Office/RHO, Branch/BRH, Stores/STR...
public class LocationType
{
    public Guid     Id               { get; set; } = Guid.NewGuid();
    public string   LocationTypeName { get; set; } = string.Empty;
    public string   ShortName        { get; set; } = string.Empty;
    public DateTime CreatedOn        { get; set; } = DateTime.UtcNow;
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}

// ── DATA → MAPPING ───────────────────────────────────────────────────────────

public class DocumentType
{
    public Guid     Id               { get; set; } = Guid.NewGuid();
    public string   DocumentTypeName { get; set; } = string.Empty;
    public string   ShortName        { get; set; } = string.Empty;
    public DateTime CreatedOn        { get; set; } = DateTime.UtcNow;
}

// DocumentMovement: prefix counter for auto-generating doc numbers (CLTPTY-0001)
public class DocumentMovement
{
    public Guid     Id                   { get; set; } = Guid.NewGuid();
    public string   DocumentMovementName { get; set; } = string.Empty;
    public string   Prefix               { get; set; } = string.Empty;
    public int      PrefixNo             { get; set; } = 1;
    public DateTime CreatedOn            { get; set; } = DateTime.UtcNow;
}

// CriteriaType: Notification Type/NTFTYP, Ticket Priority/TKTPTY, Bank/BNKTYP...
public class CriteriaType
{
    public Guid     Id                 { get; set; } = Guid.NewGuid();
    public string   CriteriaTypeName   { get; set; } = string.Empty;
    public string   CriteriaTypePrefix { get; set; } = string.Empty;
    public DateTime CreatedOn          { get; set; } = DateTime.UtcNow;
    public ICollection<CriteriaSubType> SubTypes { get; set; } = new List<CriteriaSubType>();
}

// CriteriaSubType: Bank→Summit Bank/SMTBNK, Bank→HBL Islamic/HBLIBNK... (247 total)
public class CriteriaSubType
{
    public Guid         Id              { get; set; } = Guid.NewGuid();
    public Guid         CriteriaTypeId  { get; set; }
    public string       SubCriteriaName { get; set; } = string.Empty;
    public string       Prefix          { get; set; } = string.Empty;
    public DateTime     CreatedOn       { get; set; } = DateTime.UtcNow;
    public CriteriaType CriteriaType    { get; set; } = null!;
}

// Currency: Kuwait/Kwacha/ZMK, USA/Euro/EUR, Pakistan/Pakistani Rupee/PKR
public class Currency
{
    public Guid     Id           { get; set; } = Guid.NewGuid();
    public string   CountryName  { get; set; } = string.Empty;
    public string   CurrencyName { get; set; } = string.Empty;
    public string   Symbol       { get; set; } = string.Empty;
    public DateTime CreatedOn    { get; set; } = DateTime.UtcNow;
}

// ── DATA → GEOGRAPHY (6-level cascade) ──────────────────────────────────────

public class Country
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public string   CountryName { get; set; } = string.Empty;
    public string?  Code        { get; set; }   // +92 for Pakistan
    public string   Prefix      { get; set; } = string.Empty;  // PAK, USA, CHN
    public DateTime CreatedOn   { get; set; } = DateTime.UtcNow;
    public ICollection<Province> Provinces { get; set; } = new List<Province>();
}

public class Province
{
    public Guid     Id           { get; set; } = Guid.NewGuid();
    public Guid     CountryId    { get; set; }
    public string   ProvinceName { get; set; } = string.Empty;
    public string?  Prefix       { get; set; }
    public DateTime CreatedOn    { get; set; } = DateTime.UtcNow;
    public Country  Country      { get; set; } = null!;
    public ICollection<City> Cities { get; set; } = new List<City>();
}

public class City
{
    public Guid     Id         { get; set; } = Guid.NewGuid();
    public Guid     ProvinceId { get; set; }
    public string   CityName   { get; set; } = string.Empty;
    public DateTime CreatedOn  { get; set; } = DateTime.UtcNow;
    public Province Province   { get; set; } = null!;
    public ICollection<District> Districts { get; set; } = new List<District>();
    public ICollection<Area>     Areas     { get; set; } = new List<Area>();
}

public class District
{
    public Guid     Id           { get; set; } = Guid.NewGuid();
    public Guid     CityId       { get; set; }
    public string   DistrictName { get; set; } = string.Empty;
    public DateTime CreatedOn    { get; set; } = DateTime.UtcNow;
    public City     City         { get; set; } = null!;
    public ICollection<Tehsil> Tehsils { get; set; } = new List<Tehsil>();
}

public class Tehsil
{
    public Guid     Id         { get; set; } = Guid.NewGuid();
    public Guid     DistrictId { get; set; }
    public string   TehsilName { get; set; } = string.Empty;
    public DateTime CreatedOn  { get; set; } = DateTime.UtcNow;
    public District District   { get; set; } = null!;
}

// Area: shows Province+City only in list (113 records, mostly Multan)
public class Area
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public Guid     CityId    { get; set; }
    public string   AreaName  { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public City     City      { get; set; } = null!;
}

// ── SETUP ────────────────────────────────────────────────────────────────────

// Organization: single-record company profile (HTAG Private Limited)
public class Organization
{
    public Guid     Id             { get; set; } = Guid.NewGuid();
    public string   ClientName     { get; set; } = string.Empty;
    public string?  ClientArea     { get; set; }
    public string?  ClientGroup    { get; set; }
    public string?  Currency       { get; set; }
    public string?  CurrencySymbol { get; set; }
    public string?  Email          { get; set; }
    public string?  ContactPerson  { get; set; }
    public string?  CellNo         { get; set; }
    public string?  Website        { get; set; }
    public string?  NTN            { get; set; }
    public string?  STRN           { get; set; }
    public string?  LogoUrl        { get; set; }
    public DateTime UpdatedAt      { get; set; } = DateTime.UtcNow;
}

// Location: used in Login Step 2 dropdown (Head Office, Multan Office)
public class Location
{
    public Guid         Id              { get; set; } = Guid.NewGuid();
    public string       LocationName    { get; set; } = string.Empty;
    public Guid         LocationTypeId  { get; set; }
    public string?      LocationAddress { get; set; }
    public bool         IsActive        { get; set; } = true;
    public DateTime     CreatedOn       { get; set; } = DateTime.UtcNow;
    public DateTime?    UpdatedAt       { get; set; }
    public LocationType LocationType    { get; set; } = null!;
}

// ── USER MANAGEMENT ──────────────────────────────────────────────────────────

// RoleModule: which locations a role can access (Total Locations column)
public class RoleModule
{
    public Guid     Id         { get; set; } = Guid.NewGuid();

    public Guid     RoleId     { get; set; }

    public Guid?    LocationId { get; set; }   // make nullable

    public string   ModuleKey  { get; set; } = string.Empty;

    public string   ModuleName { get; set; } = string.Empty;

    public bool     CanView    { get; set; } = true;

    public bool     CanCreate  { get; set; } = false;

    public bool     CanEdit    { get; set; } = false;

    public bool     CanDelete  { get; set; } = false;

    public bool     CanExport  { get; set; } = false;

    public AppRole  Role       { get; set; } = null!;

    public Location? Location  { get; set; }
}

// RoleMenuEntry: which menu items a role can see per location
public class RoleMenuEntry
{
    public Guid      Id         { get; set; } = Guid.NewGuid();
    public Guid      RoleId     { get; set; }
    public Guid?     LocationId { get; set; }
    public string    MenuKey    { get; set; } = string.Empty;
    public string?   MenuLabel  { get; set; }
    public string?   MenuIcon   { get; set; }
    public string?   MenuPath   { get; set; }
    public int       MenuOrder  { get; set; } = 0;
    public string?   ParentKey  { get; set; }
    public bool      IsVisible  { get; set; } = true;
    public AppRole   Role       { get; set; } = null!;
    public Location? Location   { get; set; }
}

// UserPermission: per-user per-menu overrides
public class UserPermission
{
    public Guid      Id         { get; set; } = Guid.NewGuid();
    public Guid      UserId     { get; set; }
    public Guid?     LocationId { get; set; }
    public string    MenuKey    { get; set; } = string.Empty;
    public bool      CanView    { get; set; } = true;
    public bool      CanCreate  { get; set; } = false;
    public bool      CanEdit    { get; set; } = false;
    public bool      CanDelete  { get; set; } = false;
    public AppUser   User       { get; set; } = null!;
    public Location? Location   { get; set; }
}

// ── TEMPLATES ────────────────────────────────────────────────────────────────

public class WaTemplate
{
    public Guid      Id            { get; set; } = Guid.NewGuid();
    public string    TemplateName  { get; set; } = string.Empty;
    public string    TemplateTitle { get; set; } = string.Empty;
    public string?   Activity      { get; set; }
    public string    Body          { get; set; } = string.Empty;
    public string?   Variables     { get; set; }
    public string    Status        { get; set; } = "active";
    public DateTime  CreatedOn     { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt     { get; set; }
}

public class SmsTemplate
{
    public Guid      Id            { get; set; } = Guid.NewGuid();
    public string    TemplateName  { get; set; } = string.Empty;
    public string    TemplateTitle { get; set; } = string.Empty;
    public string?   Activity      { get; set; }
    public string    Body          { get; set; } = string.Empty;
    public string    Status        { get; set; } = "active";
    public DateTime  CreatedOn     { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt     { get; set; }
}

public class EmailTemplate
{
    public Guid      Id            { get; set; } = Guid.NewGuid();
    public string    TemplateName  { get; set; } = string.Empty;
    public string    TemplateTitle { get; set; } = string.Empty;
    public string?   Activity      { get; set; }
    public string    Subject       { get; set; } = string.Empty;
    public string    Body          { get; set; } = string.Empty;
    public string    Status        { get; set; } = "active";
    public DateTime  CreatedOn     { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt     { get; set; }
}

public class WebTemplate
{
    public Guid      Id            { get; set; } = Guid.NewGuid();
    public string    TemplateName  { get; set; } = string.Empty;
    public string    TemplateTitle { get; set; } = string.Empty;
    public string?   Activity      { get; set; }
    public string    Body          { get; set; } = string.Empty;
    public string    Status        { get; set; } = "active";
    public DateTime  CreatedOn     { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt     { get; set; }
}

// ── NOTIFICATIONS ────────────────────────────────────────────────────────────

public class WaNotification
{
    public Guid      Id         { get; set; } = Guid.NewGuid();
    public string    SendNo     { get; set; } = string.Empty;
    public string    Message    { get; set; } = string.Empty;
    public Guid?     TemplateId { get; set; }
    public string?   SentBy     { get; set; }
    public string    Status     { get; set; } = "queued";
    public string?   ErrorMsg   { get; set; }
    public DateTime  SubmitOn   { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt     { get; set; }
}

public class SmsNotification
{
    public Guid      Id          { get; set; } = Guid.NewGuid();
    public string    PhoneNumber { get; set; } = string.Empty;
    public string    Message     { get; set; } = string.Empty;
    public Guid?     TemplateId  { get; set; }
    public string?   SentBy      { get; set; }
    public string    Status      { get; set; } = "queued";
    public string?   ErrorMsg    { get; set; }
    public DateTime  SubmitDate  { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt      { get; set; }
}

public class EmailNotification
{
    public Guid      Id         { get; set; } = Guid.NewGuid();
    public string    ToEmail    { get; set; } = string.Empty;
    public string    Subject    { get; set; } = string.Empty;
    public string    Body       { get; set; } = string.Empty;
    public Guid?     TemplateId { get; set; }
    public string?   SentBy     { get; set; }
    public string    Status     { get; set; } = "queued";
    public string?   ErrorMsg   { get; set; }
    public DateTime  SubmitDate { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt     { get; set; }
}

public class WebNotification
{
    public Guid      Id        { get; set; } = Guid.NewGuid();
    public Guid?     UserId    { get; set; }
    public string    Title     { get; set; } = string.Empty;
    public string    Body      { get; set; } = string.Empty;
    public string?   Url       { get; set; }
    public bool      IsRead    { get; set; } = false;
    public DateTime  CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt    { get; set; }
}

// AppNotification: Title, Message, Type (Holiday/General), Sender, Target Users (avatar stack)
public class AppNotification
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public string   Title     { get; set; } = string.Empty;
    public string   Message   { get; set; } = string.Empty;
    public string   Type      { get; set; } = "General";  // Holiday | General
    public string   Sender    { get; set; } = string.Empty;
    public string?  SentBy    { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public ICollection<AppNotificationTarget> Targets { get; set; } = new List<AppNotificationTarget>();
}

public class AppNotificationTarget
{
    public Guid            Id             { get; set; } = Guid.NewGuid();
    public Guid            NotificationId { get; set; }
    public Guid            UserId         { get; set; }
    public bool            IsRead         { get; set; } = false;
    public DateTime?       ReadAt         { get; set; }
    public AppNotification Notification   { get; set; } = null!;
    public AppUser         User           { get; set; } = null!;
}

// ── GLOBAL SETTINGS (gear icon — available across entire ADM portal) ──────────

// 3-tab settings modal: Website | Business | Roles
public class ClientSettings
{
    public Guid     Id                      { get; set; } = Guid.NewGuid();
    // Website tab
    public bool     IsWebsiteOnline         { get; set; } = false;
    public string?  FooterDescription       { get; set; }  // rich HTML
    // Business tab
    public string?  BusinessName            { get; set; }
    public string?  BusinessNature          { get; set; }
    public string?  BusinessProvince        { get; set; }
    public string?  FbrToken                { get; set; }
    public string?  ValidationToken         { get; set; }
    // Roles tab
    public Guid?    ConsultantDefaultRoleId { get; set; }
    public Guid?    ClientDefaultRoleId     { get; set; }
    public string?  ChatLinkUrl             { get; set; }  // http://consultant.10xdigitalventures.com/login
    public DateTime UpdatedAt               { get; set; } = DateTime.UtcNow;
    public AppRole? ConsultantDefaultRole   { get; set; }
    public AppRole? ClientDefaultRole       { get; set; }
}
