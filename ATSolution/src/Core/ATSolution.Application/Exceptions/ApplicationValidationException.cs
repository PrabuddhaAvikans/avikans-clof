using ATSolution.SharedKernel.Constants;

namespace ATSolution.Application.Exceptions;

public sealed class ApplicationValidationException : Exception
{
    public ApplicationValidationException(IReadOnlyList<ValidationError> errors)
        : base(ValidationMessages.OneOrMoreErrorsOccurred)
    {
        Errors = errors;
    }

    public IReadOnlyList<ValidationError> Errors { get; }

    public bool HasConflict =>
        Errors.Any(error => error.ErrorCode == ValidationErrorCodes.Conflict);
}
