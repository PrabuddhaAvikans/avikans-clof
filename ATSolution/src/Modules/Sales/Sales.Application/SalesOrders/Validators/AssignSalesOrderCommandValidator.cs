using FluentValidation;
using Sales.Application.SalesOrders;

namespace Sales.Application.SalesOrders.Validators;

public sealed class AssignSalesOrderCommandValidator : AbstractValidator<AssignSalesOrderCommand>
{
    public AssignSalesOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
