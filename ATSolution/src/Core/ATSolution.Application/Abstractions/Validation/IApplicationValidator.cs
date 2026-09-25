namespace ATSolution.Application.Abstractions.Validation;

public interface IApplicationValidator
{
    Task ValidateAsync<T>(T instance, CancellationToken cancellationToken = default);
}
