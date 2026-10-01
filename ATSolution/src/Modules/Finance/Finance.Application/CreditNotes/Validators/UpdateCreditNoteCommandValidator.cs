using Finance.Domain.Common;
using FluentValidation;

namespace Finance.Application.CreditNotes.Validators;

public sealed class UpdateCreditNoteCommandValidator : AbstractValidator<UpdateCreditNoteCommand>
{
    public UpdateCreditNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason)
            .Must(r => CreditNoteReasons.All.Contains(r!))
            .When(x => !string.IsNullOrWhiteSpace(x.Reason))
            .WithMessage("Invalid credit note reason.");
        RuleFor(x => x.LineItems).NotEmpty().When(x => x.LineItems is not null);
        RuleForEach(x => x.LineItems!).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.TaxPercent).GreaterThanOrEqualTo(0);
        }).When(x => x.LineItems is not null);
    }
}
