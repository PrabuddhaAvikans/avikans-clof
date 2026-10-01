using Finance.Domain.Common;
using FluentValidation;

namespace Finance.Application.Invoices.Validators;

public sealed class VoidInvoiceCommandValidator : AbstractValidator<VoidInvoiceCommand>
{
    public VoidInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
