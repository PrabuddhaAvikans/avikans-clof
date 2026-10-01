using FluentValidation;
using PeriodClose.Domain.Common;

namespace PeriodClose.Application.PeriodClose.Validators;

public sealed class AssertWritableCommandValidator : AbstractValidator<AssertWritableCommand>
{
    public AssertWritableCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BusinessDate).NotEmpty().MaximumLength(10);
    }
}
