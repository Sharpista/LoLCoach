using LoLCoach.Api.Analytics.Insights;

namespace LoLCoach.Api.Analytics.Recommendations;

public sealed class RecommendationEngine
{
    public IReadOnlyList<Recommendation> Generate(IEnumerable<Insight> insights)
        => insights.SelectMany(Generate).ToList();

    private static IEnumerable<Recommendation> Generate(Insight insight)
    {
        var impact = ImpactFor(insight);
        var problemType = ToProblemCode(insight.Type);

        switch (insight.Type)
        {
            case InsightType.LowCs:
                yield return new Recommendation(
                    insight.Type,
                    insight.Evidence,
                    impact,
                    "Treine last hit nos 10 primeiros minutos e priorize coletar waves antes de rotacionar sem objetivo claro.",
                    insight.Metric,
                    insight.TargetValue);
                break;
            case InsightType.HighDeaths:
                yield return new Recommendation(
                    insight.Type,
                    insight.Evidence,
                    impact,
                    "Revise mortes antes de objetivos, jogue com visão antes de avançar e aceite perder farm quando não houver informação do mapa.",
                    insight.Metric,
                    insight.TargetValue);
                break;
            case InsightType.LowVision:
                yield return new Recommendation(
                    insight.Type,
                    insight.Evidence,
                    impact,
                    "Compre sentinelas de controle em recalls importantes e posicione visão antes de disputar dragão, Arauto ou Barão.",
                    insight.Metric,
                    insight.TargetValue);
                break;
            case InsightType.LowCombatImpact:
                yield return new Recommendation(
                    insight.Type,
                    insight.Evidence,
                    impact,
                    "Escolha lutas com vantagem numérica, sincronize habilidades principais com o time e evite engage sem cooldowns relevantes.",
                    insight.Metric,
                    insight.TargetValue);
                break;
            case InsightType.InconsistentPerformance:
                yield return new Recommendation(
                    insight.Type,
                    insight.Evidence,
                    impact,
                    "Reduza variação jogando um grupo menor de campeões e registre uma decisão repetível para lane, primeira rotação e primeira luta por objetivo.",
                    insight.Metric,
                    insight.TargetValue);
                break;
        }
    }

    private static string ImpactFor(Insight insight)
        => $"Prioridade {ToSeverityText(insight.Severity)} em {insight.MatchesAnalyzed} partidas para {ToProblemCode(insight.Type)}.";

    private static string ToSeverityText(InsightSeverity severity) => severity switch
    {
        InsightSeverity.High => "alta",
        InsightSeverity.Medium => "média",
        InsightSeverity.Low => "baixa",
        _ => severity.ToString().ToLowerInvariant(),
    };

    private static string ToProblemCode(InsightType type) => type switch
    {
        InsightType.LowCs => "LOW_CS",
        InsightType.HighDeaths => "HIGH_DEATHS",
        InsightType.LowVision => "LOW_VISION",
        InsightType.LowCombatImpact => "LOW_COMBAT_IMPACT",
        InsightType.InconsistentPerformance => "INCONSISTENT_PERFORMANCE",
        InsightType.BestChampion => "BEST_CHAMPION",
        InsightType.WorstChampion => "WORST_CHAMPION",
        _ => type.ToString().ToUpperInvariant(),
    };
}
