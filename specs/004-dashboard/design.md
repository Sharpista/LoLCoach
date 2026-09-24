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

## Design System (visual)

O frontend adota o design system do modelo `Sharpista/ui-lol-ai-2` (decisão do usuário, 2026-09-14), reproduzido em Angular + Tailwind:

- Tailwind CSS v4 (migrar de SCSS).
- Paleta: stone (neutros) + amber (acento); dark mode via variante `dark` (estratégia `class` + toggle).
- Tipografia: Syne (títulos/display), Plus Jakarta Sans (corpo), JetBrains Mono (mono/dados).
- Ícones lucide (SVG inline ou `lucide-angular`).
- Animações via `@angular/animations` ou CSS transitions (substitui `motion`).

Aplica-se de forma transversal às telas de frontend (`player-search` e `player-dashboard`).

## Implementação entregue

Decisões efetivamente adotadas na entrega (PR #13, merge `634a729` em `develop`):

- Rota `/player/:id` com `loadComponent` em `frontend/src/app/app.routes.ts`.
- `features/player-dashboard/` entrega `player-dashboard`, `performance-summary`, `insights-summary`, `champion-performance` e `recent-matches`, conforme os nomes sugeridos acima.
- `core/services/player-dashboard.service.ts` define o contrato abstrato `PlayerDashboardService` e o erro `DashboardError(message, status)`.
- `core/services/http-player-dashboard.service.ts` consome `GET /api/players/{id}/analysis`; 404 vira "Jogador não encontrado." e as demais falhas "Falha ao carregar análise.".
- `app.config.ts` injeta `HttpPlayerDashboardService` para `PlayerDashboardService`; `MockPlayerDashboardService` permanece no repositório apenas como demonstração local, sem uso na composição da aplicação.
- `core/models/dashboard.ts` espelha `PerformanceAnalysisDto` da Spec 003: o endpoint único devolve `recentMatches` junto de resumo, insights e campeões, então não foi necessário um endpoint separado de partidas recentes.
- Estados implementados: `loading`, `ready`, `no-matches` (`summary.matchesAnalysed === 0`), `not-found` (404 ou `id` ausente) e `error`.
