namespace LoLCoach.Api.Application;

public sealed class PlayerNotFoundException(Guid playerId) : Exception
{
    public Guid PlayerId { get; } = playerId;
}
