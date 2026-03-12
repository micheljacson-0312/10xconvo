using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenXConvo.Infrastructure.Migrations;

// ═══════════════════════════════════════════════════════════════════════════
//  INITIAL MIGRATION — Creates all database tables
//
//  To run:
//    cd src/TenXConvo.API
//    dotnet ef database update --project ../TenXConvo.Infrastructure
//
//  Or let it run automatically on startup (EnsureCreated in Program.cs)
//
//  To generate fresh migrations yourself:
//    dotnet ef migrations add InitialCreate --project ../TenXConvo.Infrastructure --startup-project .
// ═══════════════════════════════════════════════════════════════════════════

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── FISCAL YEARS ──────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "FiscalYears",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                Name = t.Column<string>(maxLength: 100, nullable: false),
                StartDate = t.Column<DateTime>(nullable: false),
                EndDate = t.Column<DateTime>(nullable: false),
                IsActive = t.Column<bool>(nullable: false, defaultValue: true),
                IsCurrent = t.Column<bool>(nullable: false, defaultValue: false),
                CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: t => t.PrimaryKey("PK_FiscalYears", x => x.Id));

        // ── ROLES ─────────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Roles",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                RoleName = t.Column<string>(maxLength: 100, nullable: false),
                CreatedOn = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: t => t.PrimaryKey("PK_Roles", x => x.Id));

        migrationBuilder.CreateIndex("IX_Roles_RoleName", "Roles", "RoleName", unique: true);

        // ── LOCATION TYPES ────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "LocationTypes",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                LocationTypeName = t.Column<string>(maxLength: 100, nullable: false),
                ShortName = t.Column<string>(maxLength: 20, nullable: false),
                CreatedOn = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: t => t.PrimaryKey("PK_LocationTypes", x => x.Id));

        // ── LOCATIONS ─────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Locations",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                LocationName = t.Column<string>(maxLength: 200, nullable: false),
                LocationTypeId = t.Column<Guid>(nullable: false),
                LocationAddress = t.Column<string>(maxLength: 500, nullable: true),
                IsActive = t.Column<bool>(nullable: false, defaultValue: true),
                CreatedOn = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = t.Column<DateTime>(nullable: true)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Locations", x => x.Id);
                t.ForeignKey("FK_Locations_LocationTypes_LocationTypeId", x => x.LocationTypeId, "LocationTypes", "Id", onDelete: ReferentialAction.Restrict);
            });

        // ── USERS ─────────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Users",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                UserName = t.Column<string>(maxLength: 200, nullable: false),
                LoginId = t.Column<string>(maxLength: 100, nullable: false),
                Email = t.Column<string>(maxLength: 200, nullable: false),
                PasswordHash = t.Column<string>(nullable: false),
                CellNo = t.Column<string>(maxLength: 20, nullable: true),
                ImageUrl = t.Column<string>(maxLength: 500, nullable: true),
                RoleId = t.Column<Guid>(nullable: true),
                IsActive = t.Column<bool>(nullable: false, defaultValue: true),
                CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = t.Column<DateTime>(nullable: true)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Users", x => x.Id);
                t.ForeignKey("FK_Users_Roles_RoleId", x => x.RoleId, "Roles", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_Users_Email", "Users", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Users_LoginId", "Users", "LoginId", unique: true);
        migrationBuilder.CreateIndex("IX_Users_RoleId", "Users", "RoleId");

        // ── REFRESH TOKENS ────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                UserId = t.Column<Guid>(nullable: false),
                Token = t.Column<string>(maxLength: 200, nullable: false),
                ExpiresAt = t.Column<DateTime>(nullable: false),
                IsRevoked = t.Column<bool>(nullable: false, defaultValue: false),
                IpAddress = t.Column<string>(maxLength: 50, nullable: true),
                CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_RefreshTokens", x => x.Id);
                t.ForeignKey("FK_RefreshTokens_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_RefreshTokens_Token", "RefreshTokens", "Token", unique: true);
        migrationBuilder.CreateIndex("IX_RefreshTokens_UserId", "RefreshTokens", "UserId");
        // ── PASSWORD RESET TOKENS ─────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "PasswordResetTokens",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                UserId = t.Column<Guid>(nullable: false),
                Token = t.Column<string>(maxLength: 10, nullable: false),
                ExpiresAt = t.Column<DateTime>(nullable: false),
                IsUsed = t.Column<bool>(nullable: false, defaultValue: false),
                IpAddress = t.Column<string>(maxLength: 45, nullable: true),
                CreatedAt = t.Column<DateTime>(nullable: false),
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                t.ForeignKey("FK_PasswordResetTokens_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_PasswordResetTokens_Token", "PasswordResetTokens", "Token");
        migrationBuilder.CreateIndex("IX_PasswordResetTokens_UserId_IsUsed", "PasswordResetTokens", new[] { "UserId", "IsUsed" });



        // ── USER LOGIN PREFERENCES ────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "LoginPreferences",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                UserId = t.Column<Guid>(nullable: false),
                LocationId = t.Column<Guid>(nullable: true),
                FiscalYearId = t.Column<Guid>(nullable: true),
                Connection = t.Column<string>(maxLength: 20, nullable: true),
                RememberMe = t.Column<bool>(nullable: false, defaultValue: false),
                UpdatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_LoginPreferences", x => x.Id);
                t.ForeignKey("FK_LoginPreferences_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_LoginPreferences_UserId", "LoginPreferences", "UserId", unique: true);

        // ── ORGANIZATIONS ─────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Organizations",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                ClientName = t.Column<string>(maxLength: 200, nullable: false),
                ClientArea = t.Column<string>(maxLength: 100, nullable: true),
                ClientGroup = t.Column<string>(maxLength: 100, nullable: true),
                Currency = t.Column<string>(maxLength: 50, nullable: true),
                CurrencySymbol = t.Column<string>(maxLength: 10, nullable: true),
                Email = t.Column<string>(maxLength: 200, nullable: true),
                ContactPerson = t.Column<string>(maxLength: 200, nullable: true),
                CellNo = t.Column<string>(maxLength: 20, nullable: true),
                Website = t.Column<string>(maxLength: 300, nullable: true),
                NTN = t.Column<string>(maxLength: 50, nullable: true),
                STRN = t.Column<string>(maxLength: 50, nullable: true),
                LogoUrl = t.Column<string>(maxLength: 500, nullable: true),
                UpdatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: t => t.PrimaryKey("PK_Organizations", x => x.Id));

        // ── CLIENT SETTINGS ───────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "ClientSettings",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                IsWebsiteOnline = t.Column<bool>(nullable: false, defaultValue: false),
                FooterDescription = t.Column<string>(nullable: true),
                BusinessName = t.Column<string>(maxLength: 200, nullable: true),
                BusinessNature = t.Column<string>(maxLength: 200, nullable: true),
                BusinessProvince = t.Column<string>(maxLength: 100, nullable: true),
                FbrToken = t.Column<string>(maxLength: 100, nullable: true),
                ValidationToken = t.Column<string>(maxLength: 100, nullable: true),
                ConsultantDefaultRoleId = t.Column<Guid>(nullable: true),
                ClientDefaultRoleId = t.Column<Guid>(nullable: true),
                ChatLinkUrl = t.Column<string>(maxLength: 500, nullable: true),
                UpdatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_ClientSettings", x => x.Id);
                t.ForeignKey("FK_ClientSettings_Roles_ConsultantRole", x => x.ConsultantDefaultRoleId, "Roles", "Id", onDelete: ReferentialAction.SetNull);
                t.ForeignKey("FK_ClientSettings_Roles_ClientRole", x => x.ClientDefaultRoleId, "Roles", "Id", onDelete: ReferentialAction.SetNull);
            });

        // ── ERROR LOGS ────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "ErrorLogs",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                ActionName = t.Column<string>(maxLength: 200, nullable: false),
                ControllerName = t.Column<string>(maxLength: 200, nullable: false),
                Code = t.Column<int>(nullable: false),
                ErrorMessage = t.Column<string>(nullable: false),
                StackTrace = t.Column<string>(nullable: true),
                RequestPath = t.Column<string>(maxLength: 500, nullable: true),
                UserId = t.Column<string>(maxLength: 100, nullable: true),
                CreatedOn = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: t => t.PrimaryKey("PK_ErrorLogs", x => x.Id));

        // ── CONSULTANT PROFILES ───────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "ConsultantProfiles",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                UserId = t.Column<Guid>(nullable: false),
                Bio = t.Column<string>(maxLength: 500, nullable: true),
                AvatarUrl = t.Column<string>(maxLength: 500, nullable: true),
                IsOnline = t.Column<bool>(nullable: false, defaultValue: false),
                IsPublic = t.Column<bool>(nullable: false, defaultValue: true),
                JoinedDate = t.Column<DateTime>(nullable: false),
                Specialization = t.Column<string>(maxLength: 300, nullable: true),
                Experience = t.Column<string>(maxLength: 300, nullable: true),
                HourlyRate = t.Column<decimal>(nullable: true),
                Timezone = t.Column<string>(maxLength: 100, nullable: true),
                UpdatedAt = t.Column<DateTime>(nullable: true)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_ConsultantProfiles", x => x.Id);
                t.ForeignKey("FK_ConsultantProfiles_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_ConsultantProfiles_UserId", "ConsultantProfiles", "UserId", unique: true);

        // ── CUSTOMER PROFILES ─────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "CustomerProfiles",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                UserId = t.Column<Guid>(nullable: false),
                AvatarUrl = t.Column<string>(maxLength: 500, nullable: true),
                Bio = t.Column<string>(maxLength: 500, nullable: true),
                CompanyName = t.Column<string>(maxLength: 200, nullable: true),
                Industry = t.Column<string>(maxLength: 100, nullable: true),
                CityName = t.Column<string>(maxLength: 100, nullable: true),
                IsActive = t.Column<bool>(nullable: false, defaultValue: true),
                JoinedDate = t.Column<DateTime>(nullable: false),
                UpdatedAt = t.Column<DateTime>(nullable: true)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_CustomerProfiles", x => x.Id);
                t.ForeignKey("FK_CustomerProfiles_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_CustomerProfiles_UserId", "CustomerProfiles", "UserId", unique: true);

        // ── CLIENT CONNECTIONS ────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "ClientConnections",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                ConsultantId = t.Column<Guid>(nullable: false),
                CustomerId = t.Column<Guid>(nullable: false),
                Status = t.Column<string>(maxLength: 20, nullable: false, defaultValue: "pending"),
                RequestedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                AcceptedAt = t.Column<DateTime>(nullable: true)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_ClientConnections", x => x.Id);
                t.ForeignKey("FK_ClientConnections_ConsultantProfiles", x => x.ConsultantId, "ConsultantProfiles", "Id", onDelete: ReferentialAction.Cascade);
                t.ForeignKey("FK_ClientConnections_CustomerProfiles", x => x.CustomerId, "CustomerProfiles", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_ClientConnections_ConsultantId", "ClientConnections", "ConsultantId");
        migrationBuilder.CreateIndex("IX_ClientConnections_CustomerId", "ClientConnections", "CustomerId");

        // ── CONVERSATIONS ─────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Conversations",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                ConsultantId = t.Column<Guid>(nullable: false),
                CustomerId = t.Column<Guid>(nullable: false),
                CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                LastMessageAt = t.Column<DateTime>(nullable: true),
                IsActive = t.Column<bool>(nullable: false, defaultValue: true)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Conversations", x => x.Id);
                t.ForeignKey("FK_Conversations_ConsultantProfiles", x => x.ConsultantId, "ConsultantProfiles", "Id", onDelete: ReferentialAction.Cascade);
                t.ForeignKey("FK_Conversations_CustomerProfiles", x => x.CustomerId, "CustomerProfiles", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Conversations_ConsultantId", "Conversations", "ConsultantId");
        migrationBuilder.CreateIndex("IX_Conversations_CustomerId", "Conversations", "CustomerId");

        // ── MESSAGES ──────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Messages",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                ConversationId = t.Column<Guid>(nullable: false),
                SenderId = t.Column<Guid>(nullable: false),
                Body = t.Column<string>(nullable: false),
                MessageType = t.Column<string>(maxLength: 20, nullable: false, defaultValue: "text"),
                AttachmentUrl = t.Column<string>(maxLength: 500, nullable: true),
                IsRead = t.Column<bool>(nullable: false, defaultValue: false),
                SentAt = t.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                ReadAt = t.Column<DateTime>(nullable: true),
                DeletedAt = t.Column<DateTime>(nullable: true)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Messages", x => x.Id);
                t.ForeignKey("FK_Messages_Conversations_ConversationId", x => x.ConversationId, "Conversations", "Id", onDelete: ReferentialAction.Cascade);
                t.ForeignKey("FK_Messages_Users_SenderId", x => x.SenderId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Messages_ConversationId", "Messages", "ConversationId");
        migrationBuilder.CreateIndex("IX_Messages_SenderId", "Messages", "SenderId");
        migrationBuilder.CreateIndex("IX_Messages_SentAt", "Messages", "SentAt");

        // ── ROLE MODULES ──────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "RoleModules",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                RoleId = t.Column<Guid>(nullable: false),
                LocationId = t.Column<Guid>(nullable: false),
                CanView = t.Column<bool>(nullable: false, defaultValue: true),
                CanCreate = t.Column<bool>(nullable: false, defaultValue: false),
                CanEdit = t.Column<bool>(nullable: false, defaultValue: false),
                CanDelete = t.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_RoleModules", x => x.Id);
                t.ForeignKey("FK_RoleModules_Roles_RoleId", x => x.RoleId, "Roles", "Id", onDelete: ReferentialAction.Cascade);
                t.ForeignKey("FK_RoleModules_Locations_LocationId", x => x.LocationId, "Locations", "Id", onDelete: ReferentialAction.Cascade);
            });

        // ── ROLE MENU ENTRIES ─────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "RoleMenuEntries",
            columns: t => new
            {
                Id = t.Column<Guid>(nullable: false),
                RoleId = t.Column<Guid>(nullable: false),
                LocationId = t.Column<Guid>(nullable: true),
                MenuKey = t.Column<string>(maxLength: 100, nullable: false),
                MenuLabel = t.Column<string>(maxLength: 200, nullable: true),
                MenuIcon = t.Column<string>(maxLength: 100, nullable: true),
                MenuPath = t.Column<string>(maxLength: 300, nullable: true),
                MenuOrder = t.Column<int>(nullable: false, defaultValue: 0),
                ParentKey = t.Column<string>(maxLength: 100, nullable: true),
                IsVisible = t.Column<bool>(nullable: false, defaultValue: true),
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_RoleMenuEntries", x => x.Id);
                t.ForeignKey("FK_RoleMenuEntries_Roles_RoleId", x => x.RoleId, "Roles", "Id", onDelete: ReferentialAction.Cascade);
                t.ForeignKey("FK_RoleMenuEntries_Locations_LocationId", x => x.LocationId, "Locations", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_RoleMenuEntries_RoleId", "RoleMenuEntries", "RoleId");
        migrationBuilder.CreateIndex("IX_RoleMenuEntries_LocationId", "RoleMenuEntries", "LocationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop in reverse order (respecting FKs)
        migrationBuilder.DropTable("Messages");
        migrationBuilder.DropTable("Conversations");
        migrationBuilder.DropTable("ClientConnections");
        migrationBuilder.DropTable("CustomerProfiles");
        migrationBuilder.DropTable("ConsultantProfiles");
        migrationBuilder.DropTable("RoleMenuEntries");
        migrationBuilder.DropTable("RoleModules");
        migrationBuilder.DropTable("ErrorLogs");
        migrationBuilder.DropTable("ClientSettings");
        migrationBuilder.DropTable("Organizations");
        migrationBuilder.DropTable("LoginPreferences");
        migrationBuilder.DropTable("PasswordResetTokens");
        migrationBuilder.DropTable("RefreshTokens");
        migrationBuilder.DropTable("Users");
        migrationBuilder.DropTable("Locations");
        migrationBuilder.DropTable("LocationTypes");
        migrationBuilder.DropTable("Roles");
        migrationBuilder.DropTable("FiscalYears");
    }
}

