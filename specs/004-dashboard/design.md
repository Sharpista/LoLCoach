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
