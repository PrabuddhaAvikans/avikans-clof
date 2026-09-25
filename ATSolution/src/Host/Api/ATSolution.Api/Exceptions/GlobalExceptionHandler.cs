using Microsoft.AspNetCore.Diagnostics;

namespace ATSolution.Api.Exceptions;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!ExceptionHttpMapper.TryMap(exception, out var statusCode, out var body))
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);

        return true;
    }
}
