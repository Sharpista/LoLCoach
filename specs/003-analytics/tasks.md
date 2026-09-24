# Tasks 003 - Analytics Engine

- [x] Criar `PlayerMetrics`.
- [x] Criar `PlayerMetricsCalculator`.
- [x] Calcular winrate.
- [x] Calcular KDA.
- [x] Calcular CS/min.
- [x] Calcular vision/min.
- [x] Calcular damage/min.
- [x] Criar `Insight`, `InsightType`, `InsightSeverity`.
- [x] Criar `FarmingAnalyzer` e testes.
- [x] Criar `DeathAnalyzer` e testes.
- [x] Criar `VisionAnalyzer` e testes.
- [x] Criar `CombatAnalyzer` e testes.
- [x] Criar `ConsistencyAnalyzer` e testes.
- [x] Criar `ChampionAnalyzer` e testes.
- [x] Criar `PerformanceAnalysisService`.
- [x] Consolidar/remover duplicidades de insights.
- [x] Ordenar por prioridade e limitar aos principais.
- [x] Criar endpoint `/api/players/{id}/analysis`.
- [x] Criar testes de integração.

## Rastreabilidade

Estado: DONE — `READY -> REVIEW -> DONE`. Promoção para DONE na branch `chore/spec-002-003-done` (base `8be530d`).

- Implementação entregue: PR #12 (`chore/003-analytics-integration`), merge `b875658` em `develop`.
- Código correspondente por task:
  - Métricas: `backend/src/LoLCoach.Api/Analytics/Metrics/PlayerMetrics.cs`, `Analytics/Metrics/PlayerMetricsCalculator.cs`.
  - Insights: `backend/src/LoLCoach.Api/Analytics/Insights/Insight.cs`, `InsightType.cs`, `InsightSeverity.cs`.
  - Analyzers: `backend/src/LoLCoach.Api/Analytics/Analyzers/IPerformanceAnalyzer.cs` + `FarmingAnalyzer.cs`, `DeathAnalyzer.cs`, `VisionAnalyzer.cs`, `CombatAnalyzer.cs`, `ConsistencyAnalyzer.cs`, `ChampionAnalyzer.cs`.
  - Serviço e DTO: `backend/src/LoLCoach.Api/Application/PerformanceAnalysisService.cs`, `Application/PerformanceAnalysisDto.cs` (deduplica insights por tipo, ordena por severidade, gap relativo e tipo, limita aos 3 principais).
  - Endpoint: `backend/src/LoLCoach.Api/Controllers/PlayersController.cs` (`GET /api/players/{id}/analysis`).
- Determinismo (regra central da spec): nenhuma análise depende de LLM; os números derivam das partidas persistidas e a ordenação é determinística.
- Testes: `backend/tests/LoLCoach.Tests/PlayerMetricsCalculatorTests.cs`, `PerformanceAnalyzerTests.cs`, `PerformanceAnalysisEndpointTests.cs`.
- Consumo posterior: o mesmo endpoint passou a devolver `recommendations` e `coachReport` nas specs 005 e 006, sem quebrar o contrato anterior.
- CI `backend-ci`: SUCCESS no head do PR (`fe850f2`, evento `pull_request`) e no commit de merge em `develop` (`b875658`, evento `push`).
- Verificação independente do agente `github-profile` no head atual de `develop` (`8be530d`): `dotnet build` 0 warnings / 0 errors e `dotnet test` 75/75 passando, incluindo as suítes desta spec.

### Pareceres formais (consolidação Specs 002 e 003)

A ressalva anterior — ausência de parecer formal arquivado — está resolvida: os quatro pareceres foram produzidos e versionados na branch de consolidação `docs/formal-qa-review-002-003`, e nenhum deles é bloqueante ou pede correção.

- QA da Spec 003 — `specs/003-analytics/evidence/qa-validation-report.md` (origem `qa/formal-spec-003` @ `fc5b773`, validador `qualidade`): **APROVADO**.
- Code review da Spec 003 — `specs/003-analytics/evidence/code-review-report.md` (origem `review/formal-spec-003` @ `d848031`, revisor `code-reviewer`): **APROVADO**; 6 achados menores como follow-up para `dev-backend` (o principal: cobrir determinismo no pipeline completo), nenhum bloqueante.
- QA da Spec 002 — `specs/002-match-import/evidence/qa-validation-report.md` (origem `qa/formal-spec-002` @ `1612d6b`, validador `qualidade`): **APROVADO**.
- Code review da Spec 002 — `specs/002-match-import/evidence/code-review-report.md` (origem `review/formal-spec-002` @ `a1a40dc`, revisor `code-reviewer`): **APROVADO**; 3 sugestões não bloqueantes.

Status da spec: **DONE** — `READY -> REVIEW -> DONE`, agora sustentado por parecer formal de QA e de code review em `specs/003-analytics/evidence/`.
