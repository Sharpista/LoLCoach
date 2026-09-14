using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;

namespace LoLCoach.Api.Analytics.Analyzers;

public interface IPerformanceAnalyzer
{
    IEnumerable<Insight> Analyze(PlayerMetrics metrics);
}
