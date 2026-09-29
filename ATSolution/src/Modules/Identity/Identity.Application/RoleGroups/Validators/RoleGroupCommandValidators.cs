using ATSolution.SharedKernel.Constants;
using FluentValidation;
using Identity.Application.RoleGroups;
using Identity.Domain.Roles;

namespace Identity.Application.RoleGroups.Validators;

public sealed class CreateRoleGroupCommandValidator : AbstractValidator<CreateRoleGroupCommand>
{
    public CreateRoleGroupCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(UserFieldLengths.RoleName);
        RuleFor(command => command.Description!)
            .MaximumLength(UserFieldLengths.Description)
            .When(command => command.Description is not null);
        RuleFor(command => command.Status)
            .Must(status => status is EntityStatuses.Active or EntityStatuses.Inactive);
        RuleFor(command => command.RoleIds).NotNull();
    }
}

public sealed class UpdateRoleGroupCommandValidator : AbstractValidator<UpdateRoleGroupCommand>
{
    public UpdateRoleGroupCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name!)
            .NotEmpty()
            .MaximumLength(UserFieldLengths.RoleName)
            .When(command => command.Name is not null);
        RuleFor(command => command.Status!)
            .Must(status => status is EntityStatuses.Active or EntityStatuses.Inactive)
            .When(command => command.Status is not null);
    }
}
