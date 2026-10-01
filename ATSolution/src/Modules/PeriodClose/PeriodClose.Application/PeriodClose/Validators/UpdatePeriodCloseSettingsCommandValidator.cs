using FluentValidation;
using PeriodClose.Domain.Common;

namespace PeriodClose.Application.PeriodClose.Validators;

public sealed class UpdatePeriodCloseSettingsCommandValidator : AbstractValidator<UpdatePeriodCloseSettingsCommand>
{
    public UpdatePeriodCloseSettingsCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WorkerSessionCloseRule)
            .Must(r => r is null
                or WorkerSessionCloseRules.PauseAndCheckpoint
                or WorkerSessionCloseRules.AllowCrossDate
                or WorkerSessionCloseRules.RequireSupervisorConfirm)
            .WithMessage("Invalid worker session close rule.");
        RuleFor(x => x.FiscalYearStartMonth).InclusiveBetween(1, 12).When(x => x.FiscalYearStartMonth.HasValue);
        RuleFor(x => x.RequiredDailyWorkMinutes).GreaterThanOrEqualTo(0).When(x => x.RequiredDailyWorkMinutes.HasValue);
        RuleFor(x => x.DoubleOvertimeAfterMinutes).GreaterThanOrEqualTo(0).When(x => x.DoubleOvertimeAfterMinutes.HasValue);
    }
}
