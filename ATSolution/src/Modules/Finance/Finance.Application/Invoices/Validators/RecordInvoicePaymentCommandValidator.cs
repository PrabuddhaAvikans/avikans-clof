using Finance.Domain.Common;
using FluentValidation;

namespace Finance.Application.Invoices.Validators;

public sealed class RecordInvoicePaymentCommandValidator : AbstractValidator<RecordInvoicePaymentCommand>
{
    public RecordInvoicePaymentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
