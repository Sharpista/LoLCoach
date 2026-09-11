---
id: 002
name: match-import
status: READY
depends_on:
  - 001
---

# Spec 002 - Importação de Partidas

## Objetivo

Importar as últimas partidas relevantes do jogador para análise.

## Requisitos

1. Usar o PUUID do jogador.
2. Buscar os IDs das últimas 20 partidas.
3. Buscar detalhes de cada partida ainda não persistida.
4. Identificar o participante pelo PUUID.
5. Normalizar os dados.
6. Persistir partidas e dados do jogador.
7. Uma falha individual não deve cancelar toda a importação.

## Dados principais

- MatchId
- Data
- Duração
- QueueId
- GameMode
- Champion
- Role/Position
- Win
- Kills
- Deaths
- Assists
- CS
- Gold
- Damage
- DamageTaken
- VisionScore
- WardsPlaced
- WardsKilled

## Critérios de aceitação

- Buscar até 20 partidas recentes.
- Não duplicar partidas.
- Identificar corretamente o jogador pelo PUUID.
- Continuar importação após falha em uma partida.

## Fora do escopo

- replay
- vídeo
- partida em tempo real
- IA

## Open Questions

Nenhuma para o MVP. O filtro exato de filas competitivas pode ser refinado depois sem bloquear a estrutura inicial.
