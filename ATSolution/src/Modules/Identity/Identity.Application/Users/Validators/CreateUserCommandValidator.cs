using ATSolution.SharedKernel.Constants;
using FluentValidation;
using Identity.Application.Abstractions;

namespace Identity.Application.Users.Validators;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IIdentityRepository identityRepository)
    {
        RuleFor(command => command.FirstName).FirstName();
        RuleFor(command => command.LastName).LastName();
        RuleFor(command => command.Password).Password();

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .Email()
            .MustAsync(async (email, cancellationToken) =>
                !await identityRepository.AnyAsync(u => u.Email == email, cancellationToken))
            .WithErrorCode(ValidationErrorCodes.Conflict)
            .WithMessage(command =>
                string.Format(IdentityMessages.UserEmailAlreadyExists, command.Email));
    }
}
