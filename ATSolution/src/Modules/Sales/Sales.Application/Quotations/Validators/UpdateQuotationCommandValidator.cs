using FluentValidation;
using Sales.Application.Quotations;
using Sales.Domain.Common;

namespace Sales.Application.Quotations.Validators;

public sealed class UpdateQuotationCommandValidator : AbstractValidator<UpdateQuotationCommand>
{
    public UpdateQuotationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LineItems).NotEmpty().When(x => x.LineItems is not null);
        RuleFor(x => x.RejectionReason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A rejection reason is required.")
            .MinimumLength(3)
            .WithMessage("Rejection reason must be at least 3 characters.")
            .MaximumLength(2000)
            .When(x => string.Equals(x.Status, QuotationStatuses.Rejected, StringComparison.OrdinalIgnoreCase));
    }
}
