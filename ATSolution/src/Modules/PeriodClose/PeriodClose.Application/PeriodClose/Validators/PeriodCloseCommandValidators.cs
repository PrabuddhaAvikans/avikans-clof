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

public sealed class ReopenPeriodCommandValidator : AbstractValidator<ReopenPeriodCommand>
{
    public ReopenPeriodCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class CreatePeriodAdjustmentCommandValidator : AbstractValidator<CreatePeriodAdjustmentCommand>
{
    public CreatePeriodAdjustmentCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostingBusinessDate).NotEmpty().MaximumLength(10);
        RuleFor(x => x.EntityType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdjustmentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class AssertWritableCommandValidator : AbstractValidator<AssertWritableCommand>
{
    public AssertWritableCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BusinessDate).NotEmpty().MaximumLength(10);
    }
}
