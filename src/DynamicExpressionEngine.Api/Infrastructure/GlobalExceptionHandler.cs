using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DynamicExpressionEngine.Api.Infrastructure;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Exception occurred: {Message}", exception.Message);
        ProblemDetails problemDetails;
        switch (exception)
        {
            case ValidationException:
            {
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Bad Request",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = exception.Message
                };
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;
            }
            default:
            {
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Server Error",
                    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
                };
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                break;
            }
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}