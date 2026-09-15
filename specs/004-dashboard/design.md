# Design 004 - Dashboard

## Angular

Features/componentes sugeridos:

```text
features/player-dashboard/
  player-dashboard-page
  performance-summary
  insights-summary
  champion-performance
  recent-matches
```

Criar serviço para consumir `/api/players/{id}/analysis` e endpoint de partidas recentes.

## UX

Priorizar leitura rápida: resumo no topo, 3 problemas principais em destaque, depois campeões e partidas.

## Implementação entregue

Decisões efetivamente adotadas na entrega (PR #13, merge `634a729` em `develop`):

- Rota `/player/:id` com `loadComponent` em `frontend/src/app/app.routes.ts`.
- `features/player-dashboard/` entrega `player-dashboard`, `performance-summary`, `insights-summary`, `champion-performance` e `recent-matches`, conforme os nomes sugeridos acima.
- `core/services/player-dashboard.service.ts` define o contrato abstrato `PlayerDashboardService` e o erro `DashboardError(message, status)`.
- `core/services/http-player-dashboard.service.ts` consome `GET /api/players/{id}/analysis`; 404 vira "Jogador não encontrado." e as demais falhas "Falha ao carregar análise.".
- `app.config.ts` injeta `HttpPlayerDashboardService` para `PlayerDashboardService`; `MockPlayerDashboardService` permanece no repositório apenas como demonstração local, sem uso na composição da aplicação.
- `core/models/dashboard.ts` espelha `PerformanceAnalysisDto` da Spec 003: o endpoint único devolve `recentMatches` junto de resumo, insights e campeões, então não foi necessário um endpoint separado de partidas recentes.
- Estados implementados: `loading`, `ready`, `no-matches` (`summary.matchesAnalysed === 0`), `not-found` (404 ou `id` ausente) e `error`.
