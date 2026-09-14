namespace LoLCoach.Api.Application;

public sealed record PerformanceAnalysisDto(
    AnalysisPlayerDto Player,
    PerformanceSummaryDto Summary,
    IReadOnlyList<AnalysisInsightDto> Insights,
    IReadOnlyList<ChampionPerformanceDto> Champions,
    IReadOnlyList<RecentMatchDto> RecentMatches);

public sealed record AnalysisPlayerDto(Guid Id, string GameName, string TagLine, string Region);

public sealed record PerformanceSummaryDto(
    int MatchesAnalysed,
    decimal Winrate,
    decimal Kda,
    decimal CsPerMin,
    decimal VisionPerMin,
    decimal DamagePerMin);

public sealed record AnalysisInsightDto(
    string Id,
    string Severity,
    string Title,
    string Description,
    string Type,
    string Metric,
    decimal CurrentValue,
    decimal TargetValue,
    int MatchesAnalyzed);

public sealed record ChampionPerformanceDto(string Champion, int Games, decimal Winrate, decimal Kda);

public sealed record RecentMatchDto(
    Guid Id,
    string Champion,
    string Result,
    string Kda,
    int Cs,
    decimal DurationMinutes,
    DateTimeOffset PlayedAt);
