namespace LoLCoach.Api.Application;

public interface IAiCoach
{
    Task<CoachReport> CreateReportAsync(CoachAnalysisInput input, CancellationToken cancellationToken = default);
}
