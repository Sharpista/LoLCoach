namespace LoLCoach.Api.Application;

public sealed record SyncPlayerMatchesCommand(Guid PlayerId);

public sealed record SyncPlayerMatchesResult(int Imported, int Skipped, int Failed);
