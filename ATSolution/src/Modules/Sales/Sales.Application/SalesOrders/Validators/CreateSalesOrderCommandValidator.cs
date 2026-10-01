using FluentValidation;
using Sales.Application.SalesOrders;

namespace Sales.Application.SalesOrders.Validators;

public sealed class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Priority).NotEmpty();
        RuleFor(x => x.LineItems).NotEmpty();
        RuleForEach(x => x.LineItems).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductName).NotEmpty().MaximumLength(300);
            line.RuleFor(l => l.ProductSku).NotEmpty().MaximumLength(100);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
