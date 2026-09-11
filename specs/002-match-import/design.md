# Design 002 - Importação de Partidas

## Fluxo

```text
POST /api/players/{id}/matches/sync
-> SyncPlayerMatchesHandler
-> IRiotMatchClient
-> Riot Match API
-> IMatchNormalizer
-> Repositories
-> PostgreSQL
```

## Entidades

### Match

- Id
- RiotMatchId
- GameStart
- GameDuration
- QueueId
- GameMode

### PlayerMatch

- Id
- MatchId
- PlayerId
- ChampionId
- ChampionName
- TeamPosition
- Win
- Kills
- Deaths
- Assists
- TotalCs
- GoldEarned
- DamageToChampions
- DamageTaken
- VisionScore
- WardsPlaced
- WardsKilled

Relacionamento: `Player 1:N PlayerMatch N:1 Match`.

## Application

Criar `SyncPlayerMatchesCommand`, `SyncPlayerMatchesHandler` e `IMatchNormalizer`.

## Infrastructure

Criar `IRiotMatchClient`, DTOs externos e implementação dos repositórios. DTOs da Riot não entram no domínio.

## Persistência

`RiotMatchId` deve ser único.
