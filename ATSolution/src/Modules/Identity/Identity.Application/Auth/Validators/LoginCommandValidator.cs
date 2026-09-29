using FluentValidation;
using Identity.Application.Auth;
using Identity.Application.Users.Validators;

namespace Identity.Application.Auth.Validators;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).Email();
        RuleFor(command => command.Password).NotEmpty();
    }
}
