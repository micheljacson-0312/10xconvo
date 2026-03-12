namespace TenXConvo.Application.Interfaces;

// ═══════════════════════════════════════════════════════════════════════════
//  AUTH SERVICE INTERFACE
// ═══════════════════════════════════════════════════════════════════════════
public interface IAuthService
{
    Task<LoginStep1Result>  LoginStep1Async(string email);
    Task<LoginStep2Result>  LoginStep2Async(LoginStep2Input input);
    Task<TokenResult>       RefreshTokenAsync(string refreshToken);
    Task                    LogoutAsync(string refreshToken);
    Task<UserProfileResult> GetMeAsync(Guid userId);
    Task                    ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    // OAuth / External Login
    Task<LoginStep2Result>  ExternalLoginAsync(ExternalLoginInput input);
    Task<List<ExternalLoginInfo>> GetLinkedProvidersAsync(Guid userId);
    Task                    LinkExternalLoginAsync(Guid userId, ExternalLoginInput input);
    Task                    UnlinkExternalLoginAsync(Guid userId, string provider);
}

// ═══════════════════════════════════════════════════════════════════════════
//  ADMIN SERVICE INTERFACE
// ═══════════════════════════════════════════════════════════════════════════
public interface IAdminService
{
    // Settings
    Task<ClientSettingsResult>  GetSettingsAsync();
    Task                        UpdateWebsiteSettingsAsync(bool isOnline, string? footer);
    Task                        UpdateBusinessSettingsAsync(BusinessSettingsInput input);
    Task                        UpdateRolesSettingsAsync(Guid? consultantRoleId, Guid? clientRoleId, string? chatUrl);

    // Organization
    Task<OrganizationResult>    GetOrganizationAsync();
    Task                        UpdateOrganizationAsync(OrganizationInput input);
    Task<string>                UploadLogoAsync(Stream fileStream, string fileName);

    // Locations
    Task<PagedResult<LocationResult>>   GetLocationsAsync(int page, int pageSize, string? search);
    Task<LocationResult>                CreateLocationAsync(LocationInput input);
    Task<LocationResult>                UpdateLocationAsync(Guid id, LocationInput input);
    Task                                DeleteLocationAsync(Guid id);

    // Roles
    Task<PagedResult<RoleResult>>       GetRolesAsync(int page, int pageSize, string? search);
    Task<RoleResult>                    CreateRoleAsync(string roleName);
    Task<RoleResult>                    UpdateRoleAsync(Guid id, string roleName);
    Task                                DeleteRoleAsync(Guid id);

    // Users
    Task<PagedResult<UserResult>>       GetUsersAsync(int page, int pageSize, string? search);
    Task<UserResult>                    GetUserByIdAsync(Guid id);
    Task<UserResult>                    CreateUserAsync(CreateUserInput input);
    Task<UserResult>                    UpdateUserAsync(Guid id, UpdateUserInput input);
    Task                                DeleteUserAsync(Guid id);
    Task                                ResetPasswordAsync(Guid id, string newPassword);
    Task<string>                        UploadAvatarAsync(Guid userId, Stream fileStream, string fileName);

    // Error Logs
    Task<PagedResult<ErrorLogResult>>   GetErrorLogsAsync(int page, int pageSize, string? search);
    Task<ErrorLogResult>                GetErrorLogByIdAsync(Guid id);
    Task                                DeleteErrorLogAsync(Guid id);
}

// ═══════════════════════════════════════════════════════════════════════════
//  CONSULTANT SERVICE INTERFACE
// ═══════════════════════════════════════════════════════════════════════════
public interface IConsultantService
{
    Task<ConsultantProfileResult>               GetMyProfileAsync(Guid userId);
    Task<ConsultantProfileResult>               UpdateProfileAsync(Guid userId, ConsultantProfileInput input);
    Task<string>                                UploadAvatarAsync(Guid userId, Stream fileStream, string fileName);
    Task                                        SetOnlineStatusAsync(Guid userId, bool isOnline);

    Task<PagedResult<ClientResult>>             GetMyClientsAsync(Guid consultantUserId, int page, int pageSize, string? search);
    Task<List<ConnectionRequestResult>>         GetPendingRequestsAsync(Guid consultantUserId);
    Task                                        AcceptRequestAsync(Guid connectionId, Guid consultantUserId);
    Task                                        RejectRequestAsync(Guid connectionId, Guid consultantUserId);

    Task<PagedResult<ConversationResult>>       GetConversationsAsync(Guid consultantUserId, int page, int pageSize);
    Task<PagedResult<MessageResult>>            GetMessagesAsync(Guid conversationId, Guid requestingUserId, int page, int pageSize);
    Task<MessageResult>                         SendMessageAsync(Guid conversationId, Guid senderUserId, string body, string messageType);
    Task                                        MarkConversationReadAsync(Guid conversationId, Guid userId);
}

// ═══════════════════════════════════════════════════════════════════════════
//  USER SERVICE INTERFACE
// ═══════════════════════════════════════════════════════════════════════════
public interface IUserService
{
    Task<PagedResult<ConsultantCardResult>>     GetConsultantsAsync(int page, int pageSize, string? search);
    Task<ConsultantCardResult>                  GetConsultantByIdAsync(Guid consultantUserId);
    Task<ConsultantCardResult>                  GetConsultantBySlugAsync(string slug);
    Task<ConnectionResult>                      ConnectAsync(Guid customerUserId, Guid consultantUserId);

    Task<CustomerProfileResult>                 GetMyProfileAsync(Guid userId);
    Task<CustomerProfileResult>                 UpdateProfileAsync(Guid userId, CustomerProfileInput input);

    Task<PagedResult<ConversationResult>>       GetConversationsAsync(Guid customerUserId, int page, int pageSize);
    Task<PagedResult<MessageResult>>            GetMessagesAsync(Guid conversationId, Guid requestingUserId, int page, int pageSize);
    Task<MessageResult>                         SendMessageAsync(Guid conversationId, Guid senderUserId, string body);
}

// ═══════════════════════════════════════════════════════════════════════════
//  RESULT / INPUT MODELS (simple classes used by services)
// ═══════════════════════════════════════════════════════════════════════════

// Auth
public record LoginStep1Result(bool Found, string UserName, string LoginId, List<LocationItem> Locations, List<string> Connections, List<FiscalYearItem> FiscalYears);
public record LocationItem(Guid Id, string LocationName, string LocationTypeName);
public record FiscalYearItem(Guid Id, string Name, bool IsCurrent);
public record LoginStep2Input(string Email, string Password, Guid LocationId, string Connection, Guid FiscalYearId, bool RememberMe);
public record LoginStep2Result(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserProfileResult User);
public record TokenResult(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record UserProfileResult(Guid Id, string UserName, string LoginId, string Email, string? CellNo, string? ImageUrl, string RoleName);

// OAuth / External Login
public record ExternalLoginInput(string Provider, string ProviderKey, string Email, string? DisplayName, string? AvatarUrl, Guid? LocationId, string? Connection, Guid? FiscalYearId);
public record ExternalLoginInfo(string Provider, string Email, DateTime LinkedAt);

// Generic
public record PagedResult<T>(List<T> Items, int TotalRecords, int Page, int PageSize);

// Admin — Settings
public record ClientSettingsResult(bool IsWebsiteOnline, string? FooterDescription, string? BusinessName, string? BusinessNature, string? BusinessProvince, string? FbrToken, string? ValidationToken, Guid? ConsultantDefaultRoleId, string? ConsultantDefaultRoleName, Guid? ClientDefaultRoleId, string? ClientDefaultRoleName, string? ChatLinkUrl);
public record BusinessSettingsInput(string? BusinessName, string? BusinessNature, string? BusinessProvince, string? FbrToken, string? ValidationToken);

// Admin — Organization
public record OrganizationResult(Guid Id, string ClientName, string? ClientArea, string? ClientGroup, string? Currency, string? CurrencySymbol, string? Email, string? ContactPerson, string? CellNo, string? Website, string? NTN, string? STRN, string? LogoUrl);
public record OrganizationInput(string ClientName, string? ClientArea, string? ClientGroup, string? Currency, string? CurrencySymbol, string? Email, string? ContactPerson, string? CellNo, string? Website, string? NTN, string? STRN);

// Admin — Location
public record LocationResult(Guid Id, string LocationName, string LocationTypeName, string? LocationAddress, bool IsActive, DateTime CreatedOn);
public record LocationInput(string LocationName, Guid LocationTypeId, string? LocationAddress, bool IsActive = true);

// Admin — Role
public record RoleResult(Guid Id, string RoleName, DateTime CreatedOn, int TotalLocations);

// Admin — User
public record UserResult(Guid Id, string? ImageUrl, string UserName, string LoginId, string Email, string? CellNo, string? RoleName, bool IsActive, DateTime CreatedAt);
public record CreateUserInput(string UserName, string LoginId, string Email, string Password, string? CellNo, Guid? RoleId);
public record UpdateUserInput(string UserName, string Email, string? CellNo, Guid? RoleId, bool IsActive);

// Admin — ErrorLog
public record ErrorLogResult(Guid Id, string ActionName, string ControllerName, int Code, string ErrorMessage, string? StackTrace, string? RequestPath, DateTime CreatedOn);

// Consultant
public record ConsultantProfileResult(Guid UserId, string UserName, string? Bio, string? AvatarUrl, bool IsOnline, bool IsPublic, DateTime JoinedDate, string? Specialization, string? Experience, decimal? HourlyRate, string? Slug, string? ShareableLink);
public record ConsultantProfileInput(string? Bio, string? Specialization, string? Experience, decimal? HourlyRate, bool IsOnline, bool IsPublic, string? Slug);
public record ClientResult(Guid UserId, string UserName, string? AvatarUrl, DateTime ConnectedAt);
public record ConnectionRequestResult(Guid ConnectionId, Guid CustomerUserId, string CustomerName, string? CustomerAvatarUrl, DateTime RequestedAt);
public record ConnectionResult(Guid ConnectionId, string Status);

// User
public record ConsultantCardResult(Guid UserId, string UserName, string? Bio, string? AvatarUrl, bool IsOnline, DateTime JoinedDate, string? Slug, string? Specialization, string? Experience, decimal? HourlyRate);
public record CustomerProfileResult(Guid UserId, string UserName, string? AvatarUrl, string? Bio, string? CompanyName, string? Industry, DateTime JoinedDate);
public record CustomerProfileInput(string? Bio, string? CompanyName, string? Industry, string? CityName);

// Messaging
public record ConversationResult(Guid ConversationId, Guid OtherUserId, string OtherUserName, string? OtherUserAvatar, string? LastMessage, DateTime? LastMessageAt, int UnreadCount);
public record MessageResult(Guid MessageId, Guid SenderId, string SenderName, string Body, string MessageType, string? AttachmentUrl, bool IsRead, DateTime SentAt);
