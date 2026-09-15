---
id: 005
name: recommendations
status: DONE
depends_on:
  - 003
---

# Spec 005 - Recommendations

## Objetivo

Transformar insights determinísticos em ações práticas e metas mensuráveis.

## Estrutura

Cada recomendação deve possuir:

- problema
- evidência
- impacto/contexto
- recomendação
- meta

## Regras

- Não inventar números.
- Utilizar somente métricas calculadas.
- Recomendações devem ser acionáveis.
- Meta deve estar ligada a uma métrica existente.

## Exemplo

Para `LOW_CS`, recomendar uma meta explícita de CS/min com base na configuração do analyzer.

## Critérios de aceitação

- Cada insight suportado pode gerar recomendação.
- Recomendações podem ser produzidas sem IA.

## Open Questions

Nenhuma.
