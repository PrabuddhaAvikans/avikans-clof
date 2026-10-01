using Finance.Domain.Common;
using FluentValidation;

namespace Finance.Application.CreditNotes.Validators;

public sealed class IssueCreditNoteCommandValidator : AbstractValidator<IssueCreditNoteCommand>
{
    public IssueCreditNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
