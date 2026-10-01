using Finance.Domain.Common;
using FluentValidation;

namespace Finance.Application.Invoices.Validators;

public sealed class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
{
    public IssueInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
