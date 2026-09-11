using FluentValidation;
using LoLCoach.Api.Domain;

namespace LoLCoach.Api.Application;

public sealed class SearchPlayerHandler(
    IValidator<SearchPlayerCommand> validator,
    IRiotAccountClient accountClient,
    IPlayerRepository repository,
    TimeProvider timeProvider,
    ILogger<SearchPlayerHandler> logger)
{
    public async Task<PlayerDto> SearchAsync(SearchPlayerCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            throw new SearchPlayerValidationException(validation.Errors
                .GroupBy(error => ToCamelCase(error.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }

        var gameName = command.GameName!.Trim();
        var tagLine = command.TagLine!.Trim();
        var region = command.Region!.Trim().ToLowerInvariant();

        RiotAccount account;
        try
        {
            account = await accountClient.GetAccountAsync(gameName, tagLine, region, cancellationToken);
        }
        catch (RiotAccountNotFoundException)
        {
            logger.LogWarning("Riot account not found for region '{Region}'.", region);
            throw;
        }
        catch (RiotRateLimitedException)
        {
            logger.LogWarning("Riot rate limit reached for region '{Region}'.", region);
            throw;
        }
        catch (RiotServiceUnavailableException)
        {
            logger.LogError("Riot service unavailable for region '{Region}'.", region);
            throw;
        }

        var existing = await repository.FindByPuuidAsync(account.Puuid, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var player = existing is null
            ? new Player(Guid.NewGuid(), account.Puuid, account.GameName, account.TagLine, region, now, now)
            : new Player(existing.Id, account.Puuid, account.GameName, account.TagLine, region, existing.CreatedAt, now);

        var saved = await repository.SaveAsync(player, cancellationToken);
        logger.LogInformation("Player '{Puuid}' upserted for region '{Region}'.", saved.Puuid, saved.Region);
        return PlayerDto.FromPlayer(saved);
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) || !char.IsUpper(name[0]) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
