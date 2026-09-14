namespace LoLCoach.Api.Application;

public sealed class SyncPlayerMatchesHandler(
    IPlayerRepository playerRepository,
    IMatchRepository matchRepository,
    IRiotMatchClient matchClient,
    IMatchNormalizer matchNormalizer,
    ILogger<SyncPlayerMatchesHandler> logger)
{
    private const int RecentMatchLimit = 20;

    public async Task<SyncPlayerMatchesResult> SyncAsync(SyncPlayerMatchesCommand command,
        CancellationToken cancellationToken = default)
    {
        var player = await playerRepository.FindByIdAsync(command.PlayerId, cancellationToken)
            ?? throw new PlayerNotFoundException(command.PlayerId);

        var matchIds = await matchClient.GetRecentMatchIdsAsync(player.Puuid, player.Region, RecentMatchLimit,
            cancellationToken);

        var imported = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var matchId in matchIds.Take(RecentMatchLimit))
        {
            if (await matchRepository.ExistsByRiotMatchIdAsync(matchId, cancellationToken))
            {
                skipped++;
                continue;
            }

            try
            {
                var details = await matchClient.GetMatchAsync(matchId, player.Region, cancellationToken);
                var normalized = matchNormalizer.Normalize(details, player);
                if (normalized is null)
                {
                    failed++;
                    logger.LogWarning("Match '{MatchId}' does not contain player '{Puuid}'.", matchId, player.Puuid);
                    continue;
                }

                await matchRepository.AddAsync(normalized.Match, cancellationToken);
                await matchRepository.SaveChangesAsync(cancellationToken);
                imported++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is RiotRateLimitedException or RiotServiceUnavailableException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                failed++;
                logger.LogWarning(exception, "Failed to import match '{MatchId}' for player '{PlayerId}'.", matchId, player.Id);
            }
        }

        return new SyncPlayerMatchesResult(imported, skipped, failed);
    }
}
