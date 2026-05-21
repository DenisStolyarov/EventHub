using System.Diagnostics;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Presentation.ExceptionHandlers;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            logger.LogWarning(exception, "Response already started. Skipping exception handling.");

            return false;
        }

        ProblemDetails problemDetails = CreateProblemDetails(httpContext, exception);

        if (problemDetails.Status >= 500)
        {
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

            if (environment.IsDevelopment())
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }
        }
        else
        {
            logger.LogInformation(exception,
                "Handled exception ({StatusCode}): {Message}", problemDetails.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        ProblemDetails problemDetails = exception switch
        {
            ValidationException ex => new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Type = TypeUri(StatusCodes.Status400BadRequest),
                Extensions = { ["errors"] = ex.Errors }
            },

            NotFoundException ex => new ProblemDetails
            {
                Title = "Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Type = TypeUri(StatusCodes.Status404NotFound),
            },

            DomainException ex => new ProblemDetails
            {
                Title = "Domain Rule Violation",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Type = TypeUri(StatusCodes.Status422UnprocessableEntity),
            },

            _ => new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                Type = TypeUri(StatusCodes.Status500InternalServerError),
            }
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return problemDetails;
    }

    private static string TypeUri(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        StatusCodes.Status422UnprocessableEntity => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        _ => $"https://tools.ietf.org/html/rfc9110#section-15.6",
    };
}