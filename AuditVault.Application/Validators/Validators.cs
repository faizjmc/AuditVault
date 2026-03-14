using AirMark.AuditVault.Application.DTOs;
using FluentValidation;

namespace AirMark.AuditVault.Application.Validators;

public class CreateLogRequestValidator : AbstractValidator<CreateLogRequest>
{
    public CreateLogRequestValidator()
    {
        RuleFor(x => x.EventType)
            .NotEmpty().WithMessage("EventType is required.")
            .MaximumLength(100).WithMessage("EventType must not exceed 100 characters.");

        RuleFor(x => x.Payload)
            .NotNull().WithMessage("Payload is required.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}
