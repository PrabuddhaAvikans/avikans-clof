using FluentValidation;
using Sales.Application.SalesOrders;

namespace Sales.Application.SalesOrders.Validators;

public sealed class UpdateSalesOrderCommandValidator : AbstractValidator<UpdateSalesOrderCommand>
{
    public UpdateSalesOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LineItems).NotEmpty().When(x => x.LineItems is not null);
    }
}
