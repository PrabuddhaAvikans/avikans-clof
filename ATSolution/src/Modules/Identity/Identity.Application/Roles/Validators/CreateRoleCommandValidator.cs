using ATSolution.SharedKernel.Constants;
using FluentValidation;
using Identity.Application.Roles;
using Identity.Domain.Roles;

namespace Identity.Application.Roles.Validators;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(UserFieldLengths.RoleName);
        RuleFor(command => command.Description!)
            .MaximumLength(UserFieldLengths.Description)
            .When(command => command.Description is not null);
        RuleFor(command => command.Status)
            .Must(status => status is EntityStatuses.Active or EntityStatuses.Inactive);
        RuleFor(command => command.Permissions).NotNull();
    }
}
