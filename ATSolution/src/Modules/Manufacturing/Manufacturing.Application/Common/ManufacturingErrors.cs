using ATSolution.Application;
using ATSolution.Application.Exceptions;

namespace Manufacturing.Application.Common;

internal static class ManufacturingErrors
{
    public static void InvalidState(string message) =>
        throw new ApplicationValidationException(
            [new ValidationError("status", message, "INVALID_STATE")]);

    public static void NotFound(string entity, string id) =>
        throw new NotFoundException($"{entity} '{id}' was not found.");
}
