using FluentValidation;
using PeriodClose.Domain.Common;

namespace PeriodClose.Application.PeriodClose.Validators;

public sealed class ReopenPeriodCommandValidator : AbstractValidator<ReopenPeriodCommand>
{
    public ReopenPeriodCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}
