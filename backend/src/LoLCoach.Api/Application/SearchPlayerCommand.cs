namespace LoLCoach.Api.Application;

public sealed record SearchPlayerCommand
{
    public string? GameName { get; init; }
    public string? TagLine { get; init; }
    public string? Region { get; init; }
}
