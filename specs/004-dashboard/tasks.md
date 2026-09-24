# Tasks 004 - Dashboard

- [x] Criar feature `player-dashboard`.
- [x] Criar rota `/player/:id`.
- [x] Criar serviço de API.
- [x] Criar resumo de performance.
- [x] Criar cards dos 3 principais insights.
- [x] Criar tabela/lista de campeões.
- [x] Criar histórico de partidas.
- [x] Implementar loading.
- [x] Implementar estado sem partidas.
- [x] Implementar 404 e erro genérico.
- [x] Criar testes dos componentes principais.
- [x] Wire HTTP real: `HttpPlayerDashboardService` → GET `/api/players/{id}/analysis` (base Spec 003 no worktree).

## Rastreabilidade

Estado: DONE — READY -> REVIEW -> DONE na branch de fechamento `chore/004-dashboard-spec-done` (base `634a729`).

- Implementação entregue: PR #13 (`feat/004-dashboard-integration`), merge `634a729` em `develop`.
- Código correspondente por task:
  - feature e rota `/player/:id`: `frontend/src/app/app.routes.ts`, `frontend/src/app/features/player-dashboard/`.
  - serviço de API: `frontend/src/app/core/services/http-player-dashboard.service.ts` (HTTP real) e `frontend/src/app/core/services/player-dashboard.service.ts` (contrato + `DashboardError`).
  - testes dos componentes e do serviço: `frontend/src/app/features/player-dashboard/player-dashboard.spec.ts`, `frontend/src/app/core/services/http-player-dashboard.service.spec.ts`.
- CI do PR #13: workflow `Build e teste do frontend` passou (runs 34917677312 e 34917685150).
- QA formal: APROVADO, nenhum bug funcional encontrado; contrato HTTP conferido campo a campo contra `PerformanceAnalysisDto` (Spec 003) e estados `loading`, `ready`, `not-found`, `no-matches` e `error` validados.
- Revalidação local desta branch (base `634a729`): frontend 5 arquivos / 24 testes passando; backend `dotnet build` 0 warnings / 0 errors e 67/67 testes passando.
- Code review: APROVADO, nenhum achado bloqueante. Sugestões não bloqueantes registradas no PR de fechamento.
- Promoção para DONE: executada após QA e code review aprovados, conforme `AGENTS.md` e `orchestrator/ORCHESTRATOR.md` (passo 12).
- Evidências versionadas nesta branch: `specs/004-dashboard/evidence/qa-validation-report.md` (QA formal) e `specs/004-dashboard/evidence/frontend-verification-634a729.md` (revalidação independente do frontend na base `634a729`).
