using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shop.Domain.Exceptions;

namespace Shop.API.Infrastructure;

/// <summary>
/// Maps exceptions to RFC 7807 ProblemDetails so domain rule violations surface
/// as 400/404 instead of a generic 500.
/// </summary>
public class CustomExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> _logger;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException => BuildValidationProblem(validationException),

            // MUST come before the DomainException arm: switch takes the first match,
            // and a lost concurrency race is 409 (safe to retry), not 400 (don't resend).
            ConcurrencyConflictException conflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrent modification",
                Detail = conflictException.Message
            },

            // Every other domain rule violation lands here because they all derive
            // from DomainException - that is why the exception hierarchy is worth having.
            DomainException domainException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Domain rule violated",
                Detail = domainException.Message
            },

            KeyNotFoundException notFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = notFoundException.Message
            },

            ArgumentException argumentException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid argument",
                Detail = argumentException.Message
            },

            _ => null
        };

        if (problemDetails is null)
        {
            // Unexpected: log the full exception and let the default handler
            // return a 500 without leaking internals to the client.
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
            return false;
        }

        _logger.LogWarning("Request failed with {Status}: {Detail}",
            problemDetails.Status, problemDetails.Detail);

        problemDetails.Instance = httpContext.Request.Path;
        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        // Serialize by RUNTIME type, not the ProblemDetails static type: otherwise
        // System.Text.Json writes only the base properties and silently drops the
        // Errors dictionary on ValidationProblemDetails.
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails, problemDetails.GetType(), options: null, cancellationToken);

        return true;
    }

    private static ValidationProblemDetails BuildValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred"
        };
    }
}
