# Tasks 002 - Importação de Partidas

## Domain

- [ ] Criar `Match`.
- [ ] Criar `PlayerMatch`.
- [ ] Configurar relacionamentos.

## Riot Integration

- [ ] Criar `IRiotMatchClient`.
- [ ] Implementar busca de MatchIds.
- [ ] Implementar detalhes da partida.
- [ ] Criar DTOs externos.
- [ ] Tratar rate limit e falhas.

## Application

- [ ] Criar `SyncPlayerMatchesCommand`.
- [ ] Criar `SyncPlayerMatchesHandler`.
- [ ] Criar `IMatchNormalizer` e implementação.
- [ ] Ignorar partidas existentes.
- [ ] Localizar participante pelo PUUID.
- [ ] Continuar após falha individual.

## Persistence/API

- [ ] Criar repositories.
- [ ] Configurar EF Core.
- [ ] Criar índice único em RiotMatchId.
- [ ] Criar migration.
- [ ] Criar endpoint de sync.
- [ ] Retornar importadas/ignoradas/falhas.

## Testes

- [ ] Partida nova.
- [ ] Partida existente.
- [ ] Identificação por PUUID.
- [ ] Falha parcial.
- [ ] Integração do endpoint.
