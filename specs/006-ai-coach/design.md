# Design 006 - AI Coach

## Fluxo

```text
Matches -> Metrics -> Insights -> Recommendations -> AI Coach -> Coach Report
```

## Serviço

Criar uma abstração `IAiCoach` para evitar acoplamento com um provedor específico.

Entrada sugerida: `CoachAnalysisInput` contendo apenas dados estruturados validados.

Saída sugerida: `CoachReport` com resumo, forças, fraquezas, prioridade e plano.

## Resiliência

- timeout controlado
- logs sem dados secretos
- fallback para recomendações determinísticas
- nenhuma chave de API no código fonte
