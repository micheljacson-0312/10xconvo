using FluentValidation;
using TenXConvo.Application.DTOs;   // ← ye line add karo

namespace TenXConvo.Application.Validators;

// ═══════════════════════════════════════════════════════════════════════════
//  FLUENT VALIDATORS — All request DTOs validated here
//  Registered in Program.cs via AddFluentValidationAutoValidation()
//  When validation fails → 400 with field-level errors (no controller code needed)
// ═══════════════════════════════════════════════════════════════════════════

// ── AUTH ──────────────────────────────────────────────────────────────────────

public class LoginStep1Validator : AbstractValidator<LoginStep1Request>
{
    public LoginStep1Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email too long.");
    }
}

public class LoginStep2Validator : AbstractValidator<LoginStep2Request>
{
    public LoginStep2Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("Location is required.");

        RuleFor(x => x.FiscalYearId)
            .NotEmpty().WithMessage("Fiscal Year is required.");

        RuleFor(x => x.Connection)
            .NotEmpty()
            .Must(c => new[] { "Default", "QA", "Production" }.Contains(c))
            .WithMessage("Connection must be Default, QA, or Production.");
    }
}

public class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MinimumLength(32).WithMessage("Invalid refresh token.");
    }
}

// ── USERS ─────────────────────────────────────────────────────────────────────

public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
            .MaximumLength(100);

        RuleFor(x => x.LoginId)
            .NotEmpty().WithMessage("Login ID is required.")
            .EmailAddress().WithMessage("Login ID must be a valid email.")
            .MaximumLength(256);

        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.CellNo)
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-]+$").When(x => !string.IsNullOrEmpty(x.CellNo))
            .WithMessage("Invalid phone number format.");
    }
}

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().MinimumLength(2).MaximumLength(100);

        RuleFor(x => x.CellNo)
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-]+$").When(x => !string.IsNullOrEmpty(x.CellNo))
            .WithMessage("Invalid phone number format.");
    }
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Must contain uppercase.")
            .Matches(@"[0-9]").WithMessage("Must contain digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain special character.");
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from current.");
    }
}

// ── ROLES ─────────────────────────────────────────────────────────────────────

public class CreateRoleValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.")
            .MinimumLength(3).MaximumLength(50);
    }
}

// ── LOCATIONS ─────────────────────────────────────────────────────────────────

public class CreateLocationValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationValidator()
    {
        RuleFor(x => x.LocationName)
            .NotEmpty().MinimumLength(2).MaximumLength(100);

        RuleFor(x => x.LocationAddress)
            .MaximumLength(300);
    }
}

// ── NOTIFICATIONS ─────────────────────────────────────────────────────────────

public class SendWaValidator : AbstractValidator<SendWaMessageRequest>
{
    public SendWaValidator()
    {
        RuleFor(x => x.SendNo)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9]{7,15}$").WithMessage("Invalid phone number (e.g. +923001234567).");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(4096).WithMessage("WhatsApp message max 4096 chars.");
    }
}

public class SendSmsValidator : AbstractValidator<SendSmsRequest>
{
    public SendSmsValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+?[0-9]{7,15}$").WithMessage("Invalid phone number.");

        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(1600).WithMessage("SMS max 1600 chars.");
    }
}

public class SendEmailValidator : AbstractValidator<SendEmailRequest>
{
    public SendEmailValidator()
    {
        RuleFor(x => x.ToEmail)
            .NotEmpty().EmailAddress();

        RuleFor(x => x.Subject)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Body)
            .NotEmpty();
    }
}

// ── CONSULTANT PROFILE ────────────────────────────────────────────────────────

public class UpdateConsultantProfileValidator : AbstractValidator<UpdateConsultantProfileRequest>
{
    public UpdateConsultantProfileValidator()
    {
        RuleFor(x => x.Bio)
            .MaximumLength(1000).When(x => x.Bio != null);
    }
}

// ── SETTINGS ──────────────────────────────────────────────────────────────────

public class UpdateWebsiteSettingsValidator : AbstractValidator<UpdateWebsiteSettingsRequest>
{
    public UpdateWebsiteSettingsValidator()
    {
        RuleFor(x => x.FooterDescription)
            .MaximumLength(5000).When(x => x.FooterDescription != null);
    }
}

public class UpdateBusinessSettingsValidator : AbstractValidator<UpdateBusinessSettingsRequest>
{
    public UpdateBusinessSettingsValidator()
    {
        RuleFor(x => x.BusinessName).MaximumLength(200);
        RuleFor(x => x.BusinessNature).MaximumLength(200);
        RuleFor(x => x.FbrToken).MaximumLength(100);
        RuleFor(x => x.ValidationToken).MaximumLength(100);
    }
}
