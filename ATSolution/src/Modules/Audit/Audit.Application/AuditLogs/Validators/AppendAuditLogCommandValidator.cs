using Audit.Application.AuditLogs;
using Audit.Domain.AuditLogs;
using FluentValidation;

namespace Audit.Application.AuditLogs.Validators;

public sealed class AppendAuditLogCommandValidator : AbstractValidator<AppendAuditLogCommand>
{
    private static readonly string[] AllowedSeverities =
    [
        AuditSeverities.Info,
        AuditSeverities.Warning,
        AuditSeverities.Critical,
    ];

    public AppendAuditLogCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Action).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Entity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Details).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(s => AllowedSeverities.Contains(s))
            .WithMessage("Severity must be info, warning, or critical.");
        RuleFor(x => x.EntityLabel).MaximumLength(300).When(x => x.EntityLabel is not null);
        RuleFor(x => x.IpAddress).MaximumLength(100).When(x => x.IpAddress is not null);
        RuleFor(x => x.UserAgent).MaximumLength(500).When(x => x.UserAgent is not null);
    }
}
