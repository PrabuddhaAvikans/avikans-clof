using FluentValidation;

namespace Identity.Application.Users.Validators;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(command => command.Email).Email();
    }
}
