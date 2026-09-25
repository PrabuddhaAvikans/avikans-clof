using ATSolution.Application;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Microsoft.AspNetCore.Mvc;

namespace ATSolution.Api.Exceptions;

internal static class ExceptionHttpMapper
{
    public static bool TryMap(Exception exception, out int statusCode, out object body)
    {
        switch (exception)
        {
            case ApplicationValidationException validation when validation.HasConflict:
                statusCode = StatusCodes.Status409Conflict;
                body = ApiResponse.Failed(
                    validation.Errors.First(error => error.ErrorCode == ValidationErrorCodes.Conflict).ErrorMessage);
                return true;

            case ApplicationValidationException validation:
                statusCode = StatusCodes.Status400BadRequest;
                body = CreateValidationProblem(validation);
                return true;

            case NotFoundException notFound:
                statusCode = StatusCodes.Status404NotFound;
                body = ApiResponse.Failed(notFound.Message);
                return true;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                body = ApiResponse.Failed(exception.Message);
                return false;
        }
    }

    private static ValidationProblemDetails CreateValidationProblem(
        ApplicationValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = exception.Message
        };
    }
}
