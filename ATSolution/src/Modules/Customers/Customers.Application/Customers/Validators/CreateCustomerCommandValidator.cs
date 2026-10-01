using Customers.Application.Customers;
using Customers.Domain.Common;
using FluentValidation;

namespace Customers.Application.Customers.Validators;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Code).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Code));
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Type).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).Must(s => s is EntityStatuses.Active or EntityStatuses.Inactive);
        RuleFor(x => x.BillingAddresses).NotEmpty();
        RuleFor(x => x.ContactPersons).NotEmpty();
    }
}
