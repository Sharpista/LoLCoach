---
id: 004
name: dashboard
status: DONE
depends_on:
  - 003
---

# Spec 004 - Dashboard

## Objetivo

Exibir visualmente a performance recente do jogador.

## Página

`/player/{id}`

## Seções

- resumo de performance
- principais insights
- campeões
- histórico recente

## Resumo

Exibir quando disponível:

- partidas analisadas
- winrate
- KDA
- CS/min
- Vision/min
- Damage/min

## Estados

- loading
- jogador não encontrado
- sem partidas
- erro externo
- erro interno

## Critérios de aceitação

- Dados devem vir da API interna, não diretamente da Riot no frontend.
- Os 3 principais insights devem ficar visíveis no dashboard.

## Open Questions

Nenhuma.
