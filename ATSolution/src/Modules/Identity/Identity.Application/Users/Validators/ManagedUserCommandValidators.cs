using ATSolution.SharedKernel.Constants;
using FluentValidation;
using Identity.Application.Users;
using Identity.Application.Users.Validators;
using Identity.Domain.Roles;

namespace Identity.Application.Users.Validators;

public sealed class CreateManagedUserCommandValidator : AbstractValidator<CreateManagedUserCommand>
{
    public CreateManagedUserCommandValidator()
    {
        RuleFor(command => command.FirstName).FirstName();
        RuleFor(command => command.LastName).LastName();
        RuleFor(command => command.Email).Email();
        RuleFor(command => command.RoleId).NotEmpty();
        RuleFor(command => command.Status)
            .Must(status => status is EntityStatuses.Active or EntityStatuses.Inactive)
            .WithMessage("Status must be 'active' or 'inactive'.");
        RuleFor(command => command.Password!)
            .Password()
            .When(command => !string.IsNullOrWhiteSpace(command.Password));
        RuleFor(command => command.Phone)
            .MaximumLength(UserFieldLengths.Phone)
            .When(command => command.Phone is not null);
        RuleFor(command => command.Department)
            .MaximumLength(UserFieldLengths.Department)
            .When(command => command.Department is not null);
        RuleFor(command => command.JobTitle)
            .MaximumLength(UserFieldLengths.JobTitle)
            .When(command => command.JobTitle is not null);
    }
}

public sealed class UpdateManagedUserCommandValidator : AbstractValidator<UpdateManagedUserCommand>
{
    public UpdateManagedUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.FirstName!)
            .FirstName()
            .When(command => command.FirstName is not null);
        RuleFor(command => command.LastName!)
            .LastName()
            .When(command => command.LastName is not null);
        RuleFor(command => command.Email!)
            .Email()
            .When(command => command.Email is not null);
        RuleFor(command => command.Status!)
            .Must(status => status is EntityStatuses.Active or EntityStatuses.Inactive)
            .When(command => command.Status is not null)
            .WithMessage("Status must be 'active' or 'inactive'.");
    }
}
