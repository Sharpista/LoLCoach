# Tasks 002 - Importação de Partidas

## Domain

- [x] Criar `Match`.
- [x] Criar `PlayerMatch`.
- [x] Configurar relacionamentos.

## Riot Integration

- [x] Criar `IRiotMatchClient`.
- [x] Implementar busca de MatchIds.
- [x] Implementar detalhes da partida.
- [x] Criar DTOs externos.
- [x] Tratar rate limit e falhas.

## Application

- [x] Criar `SyncPlayerMatchesCommand`.
- [x] Criar `SyncPlayerMatchesHandler`.
- [x] Criar `IMatchNormalizer` e implementação.
- [x] Ignorar partidas existentes.
- [x] Localizar participante pelo PUUID.
- [x] Continuar após falha individual.

## Persistence/API

- [x] Criar repositories.
- [x] Configurar EF Core.
- [x] Criar índice único em RiotMatchId.
- [x] Criar migration.
- [x] Criar endpoint de sync.
- [x] Retornar importadas/ignoradas/falhas.

## Testes

- [x] Partida nova.
- [x] Partida existente.
- [x] Identificação por PUUID.
- [x] Falha parcial.
- [x] Integração do endpoint.
