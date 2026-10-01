using Finance.Domain.Common;
using FluentValidation;

namespace Finance.Application.Invoices.Validators;

public sealed class UpdateInvoiceCommandValidator : AbstractValidator<UpdateInvoiceCommand>
{
    public UpdateInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LineItems).NotEmpty().When(x => x.LineItems is not null);
        RuleForEach(x => x.LineItems!).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.ProductSku).NotEmpty().MaximumLength(100);
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.TaxPercent).GreaterThanOrEqualTo(0);
        }).When(x => x.LineItems is not null);
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate!.Value)
            .When(x => x.IssueDate.HasValue && x.DueDate.HasValue);
    }
}
