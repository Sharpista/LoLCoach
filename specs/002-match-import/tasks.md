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

## Rastreabilidade

Estado: DONE — `READY -> REVIEW -> DONE`. Promoção para DONE na branch `chore/spec-002-003-done` (base `8be530d`).

- Implementação entregue: PR #12 (`chore/003-analytics-integration`), merge `b875658` em `develop`.
- Código correspondente por task:
  - Domain (`Match`, `PlayerMatch`): `backend/src/LoLCoach.Api/Domain/Match.cs`, `Domain/PlayerMatch.cs`.
  - Riot integration: `backend/src/LoLCoach.Api/Application/IRiotMatchClient.cs`, `Infrastructure/RiotMatchClient.cs`, `Application/MatchImportExceptions.cs`.
  - Application (`SyncPlayerMatchesCommand`/`Handler`, `IMatchNormalizer`): `backend/src/LoLCoach.Api/Application/SyncPlayerMatchesCommand.cs`, `Application/SyncPlayerMatchesHandler.cs`, `Application/IMatchNormalizer.cs`, `Application/MatchNormalizer.cs`.
  - Persistência e índice único: `backend/src/LoLCoach.Api/Application/IMatchRepository.cs`, `Infrastructure/MatchRepository.cs`, `Infrastructure/Migrations/20260914165003_AddMatches.cs` (migration nova; nenhuma migration antiga alterada).
  - Endpoint de sync: `backend/src/LoLCoach.Api/Controllers/PlayersController.cs` (`POST /api/players/{id}/matches/sync`).
- Testes: `backend/tests/LoLCoach.Tests/MatchNormalizerTests.cs`, `MatchRepositoryTests.cs`, `MatchSyncEndpointTests.cs`, `SyncPlayerMatchesHandlerTests.cs`, com Postgres real via Testcontainers (`PostgresFixture.cs`).
- CI `backend-ci`: SUCCESS no head do PR (`fe850f2`, evento `pull_request`) e no commit de merge em `develop` (`b875658`, evento `push`).
- Verificação independente do agente `github-profile` no head atual de `develop` (`8be530d`): `dotnet build` 0 warnings / 0 errors e `dotnet test` 75/75 passando, incluindo as suítes desta spec.
- Ressalva de rastreabilidade: o PR #12 registrou a promoção para `REVIEW` e não há parecer de QA/code review versionado para esta spec; a promoção para DONE foi feita sobre a evidência de código, CI verde, testes reproduzidos e o merge aceito, não sobre um parecer formal arquivado.
