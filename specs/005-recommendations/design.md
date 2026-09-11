# Design 005 - Recommendations

## Componente

Criar `RecommendationEngine` na camada Analytics/Application.

Entrada: coleção de `Insight`.
Saída: coleção de `Recommendation`.

## Recommendation

- ProblemType
- Evidence
- RecommendationText
- GoalMetric
- TargetValue

A engine deve usar regras/templates determinísticos. LLM não é necessário nesta etapa.
