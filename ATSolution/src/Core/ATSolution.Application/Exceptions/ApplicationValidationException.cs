using ATSolution.SharedKernel.Constants;

namespace ATSolution.Application.Exceptions;

public sealed class ApplicationValidationException : Exception
{
    public ApplicationValidationException(IReadOnlyList<ValidationError> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<ValidationError> Errors { get; }

    public bool HasConflict =>
        Errors.Any(error => error.ErrorCode == ValidationErrorCodes.Conflict);

    private static string FormatMessage(IReadOnlyList<ValidationError> errors)
    {
        if (errors.Count == 0)
        {
            return ValidationMessages.OneOrMoreErrorsOccurred;
        }

        var details = string.Join(
            " ",
            errors.Select(error => error.ErrorMessage).Where(message => !string.IsNullOrWhiteSpace(message)).Distinct());

        return string.IsNullOrWhiteSpace(details)
            ? ValidationMessages.OneOrMoreErrorsOccurred
            : details;
    }
}
