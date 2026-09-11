# Design 003 - Analytics Engine

## Projeto

Criar `LolCoach.Analytics`.

```text
Analytics/
  Metrics/
  Analyzers/
  Insights/
  Recommendations/
```

## Metrics

Criar `PlayerMetricsCalculator` retornando `PlayerMetrics`.

Campos iniciais:

- Matches/Wins/Losses/WinRate
- AverageKills/Deaths/Assists/Kda
- AverageCs/CsPerMinute
- AverageVisionScore/VisionPerMinute
- AverageDamage/DamagePerMinute

## Analyzers

Interface:

```csharp
public interface IPerformanceAnalyzer
{
    IEnumerable<Insight> Analyze(PlayerMetrics metrics);
}
```

Implementações:

- FarmingAnalyzer
- DeathAnalyzer
- VisionAnalyzer
- CombatAnalyzer
- ConsistencyAnalyzer
- ChampionAnalyzer

## Insight

- Type
- Severity
- Metric
- CurrentValue
- TargetValue
- Evidence

Todos os analyzers devem ser determinísticos.
