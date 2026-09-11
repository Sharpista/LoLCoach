namespace LoLCoach.Api.Application;

/// <summary>Thrown when the Riot API reports the account does not exist (HTTP 404).</summary>
public sealed class RiotAccountNotFoundException : Exception;

/// <summary>Thrown when the Riot API rate-limits the request (HTTP 429).</summary>
public sealed class RiotRateLimitedException(int? retryAfterSeconds) : Exception
{
    public int? RetryAfterSeconds { get; } = retryAfterSeconds;
}

/// <summary>Thrown when the Riot API is unavailable (HTTP 5xx, timeout, or transport error).</summary>
public sealed class RiotServiceUnavailableException : Exception;

/// <summary>Thrown when a search command fails validation.</summary>
public sealed class SearchPlayerValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
