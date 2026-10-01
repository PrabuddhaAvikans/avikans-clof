using Finance.Domain.Common;
using FluentValidation;

namespace Finance.Application.CreditNotes.Validators;

public sealed class ApplyCreditNoteCommandValidator : AbstractValidator<ApplyCreditNoteCommand>
{
    public ApplyCreditNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
