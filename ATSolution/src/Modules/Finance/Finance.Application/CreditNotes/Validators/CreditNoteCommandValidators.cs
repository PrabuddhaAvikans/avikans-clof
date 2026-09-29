using Finance.Domain.Common;
using FluentValidation;

namespace Finance.Application.CreditNotes.Validators;

public sealed class CreateCreditNoteCommandValidator : AbstractValidator<CreateCreditNoteCommand>
{
    public CreateCreditNoteCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Reason).NotEmpty()
            .Must(r => CreditNoteReasons.All.Contains(r))
            .WithMessage("Invalid credit note reason.");
        RuleFor(x => x.LineItems).NotEmpty();
        RuleForEach(x => x.LineItems).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.TaxPercent).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Currency).MaximumLength(10).When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Notes).MaximumLength(4000).When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}

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

public sealed class IssueCreditNoteCommandValidator : AbstractValidator<IssueCreditNoteCommand>
{
    public IssueCreditNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class VoidCreditNoteCommandValidator : AbstractValidator<VoidCreditNoteCommand>
{
    public VoidCreditNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

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
