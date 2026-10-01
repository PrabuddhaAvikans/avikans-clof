using Finance.Domain.Common;
using FluentValidation;

namespace Finance.Application.CreditNotes.Validators;

public sealed class VoidCreditNoteCommandValidator : AbstractValidator<VoidCreditNoteCommand>
{
    public VoidCreditNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
