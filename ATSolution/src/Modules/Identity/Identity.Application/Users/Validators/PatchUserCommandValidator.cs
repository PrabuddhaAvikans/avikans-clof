using ATSolution.SharedKernel.Constants;
using FluentValidation;
using Identity.Application.Abstractions;

namespace Identity.Application.Users.Validators;

public sealed class PatchUserCommandValidator : AbstractValidator<PatchUserCommand>
{
    public PatchUserCommandValidator(IIdentityRepository identityRepository)
    {
        RuleFor(command => command.CurrentEmail).Email();

        RuleFor(command => command)
            .Must(command =>
                command.FirstName is not null
                || command.LastName is not null
                || command.Email is not null)
            .WithMessage(IdentityMessages.PartialUpdateRequiresField);

        RuleFor(command => command.FirstName!)
            .MaximumLength(UserFieldLengths.FirstName)
            .When(command => command.FirstName is not null);

        RuleFor(command => command.LastName!)
            .MaximumLength(UserFieldLengths.LastName)
            .When(command => command.LastName is not null);

        RuleFor(command => command.Email!)
            .Cascade(CascadeMode.Stop)
            .EmailAddress()
            .MaximumLength(UserFieldLengths.Email)
            .MustAsync(async (command, email, cancellationToken) =>
                string.Equals(command.CurrentEmail, email, StringComparison.OrdinalIgnoreCase)
                || !await identityRepository.AnyAsync(u => u.Email == email, cancellationToken))
            .WithErrorCode(ValidationErrorCodes.Conflict)
            .WithMessage(command =>
                string.Format(IdentityMessages.UserEmailAlreadyExists, command.Email))
            .When(command => command.Email is not null);
    }
}
