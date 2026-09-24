using LoLCoach.Api.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LoLCoach.Api.Infrastructure;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            SearchPlayerValidationException validation => ValidationProblem(validation),
            PlayerNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Player not found",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Detail = "No local player matches the supplied identifier.",
            },
            RiotAccountNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Player not found",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Detail = "No Riot account matches the supplied Riot ID.",
            },
            RiotRateLimitedException rateLimit => RateLimitProblem(rateLimit.RetryAfterSeconds),
            RiotServiceUnavailableException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Riot service unavailable",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.4",
                Detail = "The Riot API is temporarily unavailable. Please retry later.",
            },
            _ => null,
        };

        if (problem is null)
        {
            return false;
        }

        if (problem.Status is { } status)
        {
            httpContext.Response.StatusCode = status;
        }

        if (exception is RiotRateLimitedException { RetryAfterSeconds: { } retryAfter })
        {
            httpContext.Response.Headers.RetryAfter = retryAfter.ToString();
        }

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
        return true;
    }

    private static HttpValidationProblemDetails ValidationProblem(SearchPlayerValidationException exception) => new()
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "One or more validation errors occurred.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Detail = "The supplied search parameters are invalid.",
        Errors = exception.Errors.ToDictionary(pair => pair.Key, pair => pair.Value),
    };

    private static ProblemDetails RateLimitProblem(int? retryAfter) => new()
    {
        Status = StatusCodes.Status429TooManyRequests,
        Title = "Rate limit exceeded",
        Type = "https://tools.ietf.org/html/rfc6585#section-4",
        Detail = retryAfter is { } seconds
            ? $"The Riot API rate limit was reached. Retry after {seconds} seconds."
            : "The Riot API rate limit was reached. Please retry later.",
    };
}
