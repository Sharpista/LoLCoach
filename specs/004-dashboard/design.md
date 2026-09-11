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
