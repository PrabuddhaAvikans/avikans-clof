using ATSolution.SharedKernel.Constants;
using FluentValidation;
using Identity.Application.Abstractions;

namespace Identity.Application.Users.Validators;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(IIdentityRepository identityRepository)
    {
        RuleFor(command => command.CurrentEmail).Email();
        RuleFor(command => command.FirstName).FirstName();
        RuleFor(command => command.LastName).LastName();

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .Email()
            .MustAsync(async (command, email, cancellationToken) =>
                string.Equals(command.CurrentEmail, email, StringComparison.OrdinalIgnoreCase)
                || !await identityRepository.AnyAsync(u => u.Email == email, cancellationToken))
            .WithErrorCode(ValidationErrorCodes.Conflict)
            .WithMessage(command =>
                string.Format(IdentityMessages.UserEmailAlreadyExists, command.Email));
    }
}
