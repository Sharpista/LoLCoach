---
id: 003
name: analytics
status: READY
depends_on:
  - 001
  - 002
---

# Spec 003 - Analytics Engine

## Objetivo

Transformar partidas persistidas em métricas consolidadas e insights determinísticos de performance.

## Métricas iniciais

- partidas, vitórias, derrotas e winrate
- kills/deaths/assists médios
- KDA médio
- CS/min
- damage/min
- vision/min
- métricas por campeão
- consistência entre partidas

## Insights iniciais

- farm baixo
- mortes excessivas
- visão baixa
- KDA/participação baixa
- inconsistência
- melhores/piores campeões

## Regra central

A identificação primária de problemas não deve depender de LLM.

## Saída de insight

```json
{
  "type": "LOW_CS",
  "severity": "high",
  "currentValue": 5.2,
  "targetValue": 6.5,
  "matchesAnalyzed": 20
}
```

## Critérios de aceitação

- Mesma coleção de partidas produz o mesmo resultado.
- Nenhum número pode ser inventado.
- Insights devem possuir evidência mensurável.

## Open Questions

Nenhuma para a primeira versão. Targets iniciais podem começar configuráveis e ser refinados depois.
