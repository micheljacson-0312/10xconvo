namespace TenXConvo.Application.DTOs;

// ═══════════════════════════════════════════════════════════════════════════
//  COMMON
// ═══════════════════════════════════════════════════════════════════════════

public record PagedResult<T>(List<T> Items, int TotalRecords, int Page, int PageSize);

public record ApiResponse<T>(bool Success, string? Message, T? Data);

// ═══════════════════════════════════════════════════════════════════════════
//  AUTH — 2-Step Login
// ═══════════════════════════════════════════════════════════════════════════

// Step 1: enter email → returns user exists + available locations/connections/fiscal years
public record LoginStep1Request(string Email);
public record LoginStep1Response(string UserName, string LoginId, List<LocationDto> Locations, List<string> Connections, List<FiscalYearDto> FiscalYears);

// Step 2: enter password + select Location + Connection + FiscalYear
public record LoginStep2Request(string Email, string Password, Guid LocationId, string Connection, Guid FiscalYearId, bool RememberMe);
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserProfileDto User);

public record FiscalYearDto(Guid Id, string Name, DateTime StartDate, DateTime EndDate, bool IsCurrent);
public record UserProfileDto(Guid Id, string UserName, string LoginId, string Email, string? CellNo, string? ImageUrl, string? RoleName);
public record RefreshTokenRequest(string RefreshToken);

// ═══════════════════════════════════════════════════════════════════════════
//  SYSTEM
// ═══════════════════════════════════════════════════════════════════════════

// Error Log
public record ErrorLogDto(Guid Id, string ActionName, string ControllerName, int Code, string ErrorMessage, DateTime CreatedOn);
public record ErrorLogDetailDto(Guid Id, string ActionName, string ControllerName, int Code, string ErrorMessage, string? StackTrace, string? RequestPath, string? UserId, DateTime CreatedOn);

// Configuration
public record ConfigurationDto(string Key, string Value, string? Category, string? Notes);
public record UpdateConfigRequest(string Value, string? Notes);

// Web Push Token
public record WebPushTokenDto(Guid Id, string UserName, string LoginId, string DeviceId, string Platform, bool IsActive, DateTime CreatedOn);
public record SendPushMessageRequest(Guid TokenId, string Title, string Body);

// ═══════════════════════════════════════════════════════════════════════════
//  DATA → CONSTANT
// ═══════════════════════════════════════════════════════════════════════════

// Control Type: Sr, Control Type Name, Control Type Prefix, Created On
public record ControlTypeDto(Guid Id, string ControlTypeName, string ControlTypePrefix, DateTime CreatedOn);
public record CreateControlTypeRequest(string ControlTypeName, string ControlTypePrefix);
public record UpdateControlTypeRequest(string ControlTypeName, string ControlTypePrefix);

// Control Category: Sr, Control Type, Control Category, Control Prefix, Created On
public record ControlCategoryDto(Guid Id, string ControlTypeName, string ControlCategoryName, string ControlPrefix, DateTime CreatedOn);
public record CreateControlCategoryRequest(Guid ControlTypeId, string ControlCategoryName, string ControlPrefix);
public record UpdateControlCategoryRequest(Guid ControlTypeId, string ControlCategoryName, string ControlPrefix);

// Client Area: Sr, Control Area Name, Control Area Prefix, Created On
public record ClientAreaDto(Guid Id, string ControlAreaName, string ControlAreaPrefix, DateTime CreatedOn);
public record CreateClientAreaRequest(string ControlAreaName, string ControlAreaPrefix);
public record UpdateClientAreaRequest(string ControlAreaName, string ControlAreaPrefix);

// Client Category: Sr, Control Category Name, Control Category Prefix, Created On
public record ClientCategoryDto(Guid Id, string ControlCategoryName, string ControlCategoryPrefix, DateTime CreatedOn);
public record CreateClientCategoryRequest(string ControlCategoryName, string ControlCategoryPrefix);
public record UpdateClientCategoryRequest(string ControlCategoryName, string ControlCategoryPrefix);

// Location Type: Sr, Location Type Name, Short Name, Created On
public record LocationTypeDto(Guid Id, string LocationTypeName, string ShortName, DateTime CreatedOn);
public record CreateLocationTypeRequest(string LocationTypeName, string ShortName);
public record UpdateLocationTypeRequest(string LocationTypeName, string ShortName);

// ═══════════════════════════════════════════════════════════════════════════
//  DATA → MAPPING
// ═══════════════════════════════════════════════════════════════════════════

// Document Type: Sr, Document Type Name, Short Name, Created On
public record DocumentTypeDto(Guid Id, string DocumentTypeName, string ShortName, DateTime CreatedOn);
public record CreateDocumentTypeRequest(string DocumentTypeName, string ShortName);
public record UpdateDocumentTypeRequest(string DocumentTypeName, string ShortName);

// Document Movement: Sr, Document Movement Name, Prefix, Prefix No
public record DocumentMovementDto(Guid Id, string DocumentMovementName, string Prefix, int PrefixNo, DateTime CreatedOn);
public record CreateDocumentMovementRequest(string DocumentMovementName, string Prefix, int PrefixNo);
public record UpdateDocumentMovementRequest(string DocumentMovementName, string Prefix, int PrefixNo);

// Criteria Type: Sr, Criteria Type Name, Criteria Type Prefix, Created On
public record CriteriaTypeDto(Guid Id, string CriteriaTypeName, string CriteriaTypePrefix, DateTime CreatedOn);
public record CreateCriteriaTypeRequest(string CriteriaTypeName, string CriteriaTypePrefix);
public record UpdateCriteriaTypeRequest(string CriteriaTypeName, string CriteriaTypePrefix);

// Criteria Sub Type: Sr, Criteria Type, Sub Criteria Name, Prefix
public record CriteriaSubTypeDto(Guid Id, string CriteriaTypeName, string SubCriteriaName, string Prefix, DateTime CreatedOn);
public record CreateCriteriaSubTypeRequest(Guid CriteriaTypeId, string SubCriteriaName, string Prefix);
public record UpdateCriteriaSubTypeRequest(Guid CriteriaTypeId, string SubCriteriaName, string Prefix);

// Currency: Sr, Country Name, Currency Name, Symbol
public record CurrencyDto(Guid Id, string CountryName, string CurrencyName, string Symbol, DateTime CreatedOn);
public record CreateCurrencyRequest(string CountryName, string CurrencyName, string Symbol);
public record UpdateCurrencyRequest(string CountryName, string CurrencyName, string Symbol);

// ═══════════════════════════════════════════════════════════════════════════
//  DATA → GEOGRAPHY
// ═══════════════════════════════════════════════════════════════════════════

// Country: Sr, Country Name, Code (+92/N/A), Prefix
public record CountryDto(Guid Id, string CountryName, string? Code, string Prefix, DateTime CreatedOn);
public record CreateCountryRequest(string CountryName, string? Code, string Prefix);
public record UpdateCountryRequest(string CountryName, string? Code, string Prefix);

// Province: Sr, Country, Province, Prefix
public record ProvinceDto(Guid Id, string CountryName, string ProvinceName, string? Prefix, DateTime CreatedOn);
public record CreateProvinceRequest(Guid CountryId, string ProvinceName, string? Prefix);
public record UpdateProvinceRequest(Guid CountryId, string ProvinceName, string? Prefix);

// City: Sr, Country, Province, City
public record CityDto(Guid Id, string CountryName, string ProvinceName, string CityName, DateTime CreatedOn);
public record CreateCityRequest(Guid ProvinceId, string CityName);
public record UpdateCityRequest(Guid ProvinceId, string CityName);

// District: Sr, Country, Province, City, District
public record DistrictDto(Guid Id, string CountryName, string ProvinceName, string CityName, string DistrictName, DateTime CreatedOn);
public record CreateDistrictRequest(Guid CityId, string DistrictName);
public record UpdateDistrictRequest(Guid CityId, string DistrictName);

// Tehsil: Sr, Country, Province, City, District, Tehsil
public record TehsilDto(Guid Id, string CountryName, string ProvinceName, string CityName, string DistrictName, string TehsilName, DateTime CreatedOn);
public record CreateTehsilRequest(Guid DistrictId, string TehsilName);
public record UpdateTehsilRequest(Guid DistrictId, string TehsilName);

// Area: Sr, Province, City, Area
public record AreaDto(Guid Id, string ProvinceName, string CityName, string AreaName, DateTime CreatedOn);
public record CreateAreaRequest(Guid CityId, string AreaName);
public record UpdateAreaRequest(Guid CityId, string AreaName);

// ═══════════════════════════════════════════════════════════════════════════
//  SETUP
// ═══════════════════════════════════════════════════════════════════════════

// Organization (single record — Company Profile form)
public record OrganizationDto(
    Guid Id, string ClientName, string? ClientArea, string? ClientGroup,
    string? Currency, string? CurrencySymbol,
    string? Email, string? ContactPerson, string? CellNo, string? Website,
    string? NTN, string? STRN, string? LogoUrl, DateTime UpdatedAt);

public record UpdateOrganizationRequest(
    string ClientName, string? ClientArea, string? ClientGroup,
    string? Currency, string? CurrencySymbol,
    string? Email, string? ContactPerson, string? CellNo, string? Website,
    string? NTN, string? STRN);

// Location: Sr, Location Name, Location Type Name, Location Address
public record LocationDto(Guid Id, string LocationName, string LocationTypeName, string? LocationAddress, bool IsActive, DateTime CreatedOn);
public record CreateLocationRequest(string LocationName, Guid LocationTypeId, string? LocationAddress);
public record UpdateLocationRequest(string LocationName, Guid LocationTypeId, string? LocationAddress, bool IsActive);

// ═══════════════════════════════════════════════════════════════════════════
//  USER
// ═══════════════════════════════════════════════════════════════════════════

// Role: Sr, Role, Created On, Action (edit+delete)
public record RoleDto(Guid Id, string RoleName, DateTime CreatedOn, int TotalLocations);
public record CreateRoleRequest(string RoleName);
public record UpdateRoleRequest(string RoleName);

// Role Module: Sr, Role, Total Locations
public record RoleModuleDto(Guid RoleId, string RoleName, int TotalLocations, List<RoleModuleDetailDto> Details);
public record RoleModuleDetailDto(Guid Id, string LocationName, bool CanView, bool CanCreate, bool CanEdit, bool CanDelete);
public record UpsertRoleModuleRequest(Guid RoleId, List<RoleModuleItemRequest> Modules);
public record RoleModuleItemRequest(Guid LocationId, bool CanView, bool CanCreate, bool CanEdit, bool CanDelete);

// Role Menu: Sr, Role, Total Locations
public record RoleMenuDto(Guid RoleId, string RoleName, int TotalLocations, List<RoleMenuDetailDto> Details);
public record RoleMenuDetailDto(Guid Id, string LocationName, string MenuKey, bool IsVisible);
public record UpsertRoleMenuRequest(Guid RoleId, List<RoleMenuItemRequest> Menus);
public record RoleMenuItemRequest(Guid LocationId, string MenuKey, bool IsVisible);

// User Registration: Sr, Image, User Name, Login ID, Email, Cell No, Action
public record UserDto(Guid Id, string? ImageUrl, string UserName, string LoginId, string Email, string? CellNo, string? RoleName, bool IsActive, DateTime CreatedAt);
public record CreateUserRequest(string UserName, string LoginId, string Email, string Password, string? CellNo, Guid? RoleId);
public record UpdateUserRequest(string UserName, string Email, string? CellNo, Guid? RoleId, bool IsActive);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ResetPasswordRequest(string NewPassword);

// User Permission (complex form — per user per menu key)
public record UserPermissionDto(Guid Id, string MenuKey, string? LocationName, bool CanView, bool CanCreate, bool CanEdit, bool CanDelete);
public record UpsertUserPermissionsRequest(Guid UserId, List<UserPermissionItemRequest> Permissions);
public record UserPermissionItemRequest(string MenuKey, Guid? LocationId, bool CanView, bool CanCreate, bool CanEdit, bool CanDelete);

// ═══════════════════════════════════════════════════════════════════════════
//  TEMPLATE
//  Columns: Sr, Template Name, Template Title, Activity, Status, Action
// ═══════════════════════════════════════════════════════════════════════════

public record WaTemplateDto(Guid Id, string TemplateName, string TemplateTitle, string? Activity, string Status, DateTime CreatedOn);
public record WaTemplateDetailDto(Guid Id, string TemplateName, string TemplateTitle, string? Activity, string Body, string? Variables, string Status);
public record CreateWaTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Body, string? Variables);
public record UpdateWaTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Body, string? Variables, string Status);

public record SmsTemplateDto(Guid Id, string TemplateName, string TemplateTitle, string? Activity, string Status, DateTime CreatedOn);
public record SmsTemplateDetailDto(Guid Id, string TemplateName, string TemplateTitle, string? Activity, string Body, string Status);
public record CreateSmsTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Body);
public record UpdateSmsTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Body, string Status);

public record EmailTemplateDto(Guid Id, string TemplateName, string TemplateTitle, string? Activity, string Status, DateTime CreatedOn);
public record EmailTemplateDetailDto(Guid Id, string TemplateName, string TemplateTitle, string? Activity, string Subject, string Body, string Status);
public record CreateEmailTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Subject, string Body);
public record UpdateEmailTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Subject, string Body, string Status);

public record WebTemplateDto(Guid Id, string TemplateName, string TemplateTitle, string? Activity, string Status, DateTime CreatedOn);
public record WebTemplateDetailDto(Guid Id, string TemplateName, string TemplateTitle, string? Activity, string Body, string Status);
public record CreateWebTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Body);
public record UpdateWebTemplateRequest(string TemplateName, string TemplateTitle, string? Activity, string Body, string Status);

// ═══════════════════════════════════════════════════════════════════════════
//  NOTIFICATION
// ═══════════════════════════════════════════════════════════════════════════

// WA Notification: Sr, Submit On, Send No, Message, Action
public record WaNotificationDto(Guid Id, string SendNo, string Message, string Status, DateTime SubmitOn);
public record SendWaMessageRequest(string SendNo, string Message, Guid? TemplateId);

// SMS Notification: Sr, Phone Number, Message, Submit Date, Action
public record SmsNotificationDto(Guid Id, string PhoneNumber, string Message, string Status, DateTime SubmitDate);
public record SendSmsRequest(string PhoneNumber, string Message, Guid? TemplateId);

// Email Notification
public record EmailNotificationDto(Guid Id, string ToEmail, string Subject, string Status, DateTime SubmitDate);
public record SendEmailRequest(string ToEmail, string Subject, string Body, Guid? TemplateId);

// Web Notification
public record WebNotificationDto(Guid Id, string Title, string Body, bool IsRead, DateTime CreatedOn);
public record SendWebNotificationRequest(Guid? UserId, string Title, string Body, string? Url);

// App Notification: Sr, Title, Message, Type, Sender, Target Users (count), Created On
public record AppNotificationDto(Guid Id, string Title, string Message, string Type, string Sender, int TargetCount, DateTime CreatedOn);
public record AppNotificationDetailDto(Guid Id, string Title, string Message, string Type, string Sender, List<AppNotificationTargetDto> Targets, DateTime CreatedOn);
public record AppNotificationTargetDto(Guid UserId, string UserName, string? ImageUrl, bool IsRead, DateTime? ReadAt);
public record CreateAppNotificationRequest(string Title, string Message, string Type, List<Guid> TargetUserIds);

// ═══════════════════════════════════════════════════════════════════════════
//  CLIENT SETTINGS — Settings modal (gear icon in MHM/Messaging portal)
//  3 tabs: Website | Business | Roles
// ═══════════════════════════════════════════════════════════════════════════

// Website tab
public record ClientSettingsWebsiteDto(bool IsWebsiteOnline, string? FooterDescription);
public record UpdateWebsiteSettingsRequest(bool IsWebsiteOnline, string? FooterDescription);

// Business tab
public record ClientSettingsBusinessDto(string? BusinessName, string? BusinessNature, string? BusinessProvince, string? FbrToken, string? ValidationToken);
public record UpdateBusinessSettingsRequest(string? BusinessName, string? BusinessNature, string? BusinessProvince, string? FbrToken, string? ValidationToken);

// Roles tab
public record ClientSettingsRolesDto(Guid? ConsultantDefaultRoleId, string? ConsultantDefaultRoleName, Guid? ClientDefaultRoleId, string? ClientDefaultRoleName, string? ChatLinkUrl);
public record UpdateRolesSettingsRequest(Guid? ConsultantDefaultRoleId, Guid? ClientDefaultRoleId, string? ChatLinkUrl);

// Full settings (all tabs combined)
public record ClientSettingsDto(ClientSettingsWebsiteDto Website, ClientSettingsBusinessDto Business, ClientSettingsRolesDto Roles);

// ═══════════════════════════════════════════════════════════════════════════
//  CONSULTANT PROFILE — Public card on uat-10x.htagsol.com/Consultants
//  Card: Avatar (online dot), Name, Bio/tagline, Joined date, Connect button
// ═══════════════════════════════════════════════════════════════════════════

public record ConsultantCardDto(
    Guid   UserId,
    string UserName,
    string? AvatarUrl,
    bool   IsOnline,
    string? Bio,
    DateTime JoinedDate
);

public record UpdateConsultantProfileRequest(string? Bio, bool IsOnline, bool IsPublic);
