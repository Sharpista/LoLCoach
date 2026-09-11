namespace LoLCoach.Api.Application;

/// <summary>Account identity returned by the Riot ACCOUNT-V1 API.</summary>
public sealed record RiotAccount(string Puuid, string GameName, string TagLine);

public interface IRiotAccountClient
{
    /// <summary>
    /// Looks up a Riot account by game name and tag line on the given LoL platform.
    /// Throws <see cref="RiotAccountNotFoundException"/>, <see cref="RiotRateLimitedException"/>,
    /// or <see cref="RiotServiceUnavailableException"/> on upstream failures.
    /// </summary>
    Task<RiotAccount> GetAccountAsync(string gameName, string tagLine, string platform,
        CancellationToken cancellationToken = default);
}
