namespace LoLCoach.Api.Application;

public static class DeterministicCoachReportFactory
{
    public static CoachReport Create(CoachAnalysisInput input)
    {
        var priority = input.Recommendations.FirstOrDefault();
        var weaknesses = input.Recommendations
            .Take(3)
            .Select(recommendation => recommendation.ProblemType)
            .ToList();
        var strengths = BuildStrengths(input).Take(3).ToList();
        var plan = input.Recommendations
            .Take(3)
            .Select(recommendation => new CoachGoalDto(
                recommendation.GoalMetric,
                recommendation.TargetValue,
                recommendation.RecommendationText))
            .ToList();

        return new CoachReport(
            $"Período com {input.Summary.MatchesAnalysed} partidas analisadas, {input.Summary.Winrate}% de winrate e KDA médio de {input.Summary.Kda}.",
            strengths,
            weaknesses,
            priority is null ? "Manter consistência e ampliar a amostra de partidas." : priority.RecommendationText,
            plan,
            false);
    }

    private static IEnumerable<string> BuildStrengths(CoachAnalysisInput input)
    {
        if (input.Summary.Winrate >= 50)
            yield return $"Winrate de {input.Summary.Winrate}% no período analisado.";
        if (input.Summary.Kda >= 2.5m)
            yield return $"KDA médio de {input.Summary.Kda}.";
        if (input.Summary.CsPerMin >= 6.5m)
            yield return $"CS/min de {input.Summary.CsPerMin}.";
        if (input.Summary.VisionPerMin >= 0.8m)
            yield return $"Vision/min de {input.Summary.VisionPerMin}.";
    }
}
