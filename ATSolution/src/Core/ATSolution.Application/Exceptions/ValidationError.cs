namespace ATSolution.Application.Exceptions;

public sealed record ValidationError(
    string PropertyName,
    string ErrorMessage,
    string ErrorCode);
