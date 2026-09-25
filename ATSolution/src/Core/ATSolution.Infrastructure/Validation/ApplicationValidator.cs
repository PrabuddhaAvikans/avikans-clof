using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ATSolution.Infrastructure.Validation;

internal sealed class ApplicationValidator(IServiceProvider serviceProvider) : IApplicationValidator
{
    public async Task ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var validators = serviceProvider.GetServices<IValidator<T>>().ToArray();
        if (validators.Length == 0)
        {
            return;
        }

        var context = new ValidationContext<T>(instance);
        var errors = new List<ValidationError>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);

            errors.AddRange(result.Errors.Select(failure => new ValidationError(
                failure.PropertyName,
                failure.ErrorMessage,
                failure.ErrorCode)));
        }

        if (errors.Count > 0)
        {
            throw new ApplicationValidationException(errors);
        }
    }
}
