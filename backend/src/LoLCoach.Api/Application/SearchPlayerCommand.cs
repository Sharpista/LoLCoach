namespace LoLCoach.Api.Application;

/// <summary>Request body for searching a Riot account.</summary>
public sealed record SearchPlayerCommand
{
    /// <summary>Riot ID game name: trimmed length 3-16; Unicode letters, digits, and spaces.</summary>
    public string? GameName { get; init; }

    /// <summary>Riot ID tag line: trimmed length 2-5; alphanumeric characters.</summary>
    public string? TagLine { get; init; }

    /// <summary>LoL platform code, for example br1, euw1, na1, or kr.</summary>
    public string? Region { get; init; }
}
