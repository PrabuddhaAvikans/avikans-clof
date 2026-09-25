using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ATSolution.Api.Exceptions;

internal sealed class ApplicationExceptionFilter : IAsyncExceptionFilter
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (!ExceptionHttpMapper.TryMap(context.Exception, out var statusCode, out var body))
        {
            return Task.CompletedTask;
        }

        context.Result = new ObjectResult(body) { StatusCode = statusCode };
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
