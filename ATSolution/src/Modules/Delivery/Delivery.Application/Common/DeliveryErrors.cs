using ATSolution.Application;
using ATSolution.Application.Exceptions;

namespace Delivery.Application.Common;

internal static class DeliveryErrors
{
    public static void InvalidState(string message) =>
        throw new ApplicationValidationException(
            [new ValidationError("status", message, "INVALID_STATE")]);

    public static void NotFound(Guid id) =>
        throw new NotFoundException($"Delivery '{id}' was not found.");
}
