using ATSolution.SharedKernel.Constants;
using FluentValidation;

namespace Identity.Application.Users.Validators;

internal static class UserValidationRules
{
    public static IRuleBuilderOptions<T, string> FirstName<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(UserFieldLengths.FirstName);

    public static IRuleBuilderOptions<T, string> LastName<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(UserFieldLengths.LastName);

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(UserFieldLengths.Email);

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MinimumLength(UserFieldLengths.PasswordMinLength)
            .MaximumLength(UserFieldLengths.Password);
}
