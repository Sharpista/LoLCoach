# LoLCoach — Frontend (Angular)

Shell de navegação + busca de jogador (spec 001) + dashboard (spec 004).
Tema escuro premium (stone + âmbar), fontes Syne / Plus Jakarta Sans / JetBrains Mono.

## Stack

- Angular 22 (standalone, zoneless), TypeScript 6
- Estilo: **SCSS** com design tokens em `src/styles.scss` (CSS custom properties)
- Testes: **Vitest + jsdom** via `@angular/build:unit-test` (sem navegador/Chrome)
- Sem Angular Material

## Comandos

```bash
npm install --include=dev   # instalar dependências
npm start                   # dev server (ng serve, http://localhost:4200)
npm run build               # build de produção (dist/frontend/browser)
npm test                    # testes unitários (Vitest, sem watch)
```

CI (`../.github/workflows/frontend-ci.yml`) roda `npm ci --include=dev`, `npm run build` e `npm test`.

## Integração com o backend (spec 001)

- `src/environments/environment.ts` — `apiUrl: 'http://localhost:5181'` (porta documentada no `backend/README.md`).
- `src/environments/environment.prod.ts` — `apiUrl: ''` (caminhos relativos; usar atrás de reverse-proxy).
- Override em runtime: `window.LOLCOACH_API_URL` (ver `src/app/core/config/api-config.ts`).
- Endpoint: `POST /api/players/search` → `{ id, puuid, gameName, tagLine, region, ... }`.
  Em sucesso navega para `/player/{id}`. Erros 400/404/429/503 são mapeados para
  mensagens amigáveis (Problem Details, sem vazar stack/chave).

## Dashboard (spec 004) — dados MOCK

O backend 002/003 ainda não existe. O provider atual é o `MockPlayerDashboardService`
(`src/app/core/services/mock-player-dashboard.service.ts`), registrado em
`src/app/app.config.ts`:

```ts
{ provide: PlayerDashboardService, useClass: MockPlayerDashboardService }
```

**Para trocar pela API real:** criar um `HttpPlayerDashboardService` que estende
`PlayerDashboardService` (consumindo `GET /api/players/{id}/analysis`, ver
`specs/004-dashboard/design.md`) e alterar o `useClass` em `app.config.ts`. O
contrato de dados (`PlayerDashboard` em `core/models/dashboard.ts`) e a UI não mudam.

Estados cobertos: `loading`, jogador não encontrado (404), sem partidas
(`matchesAnalysed === 0`) e erro. IDs sentinela para exercitar manualmente:
`/player/not-found`, `/player/no-matches`, `/player/error`.

## AI Coach (spec 006) — não implementado

O AI Coach (Gemini) será consumido **exclusivamente via backend**. Nenhuma chamada
direta à API Gemini deve existir no browser. Ver:
- `src/app/core/services/ai-coach.service.ts` (stub documentado)
- `src/environments/environment.ts` (placeholder `geminiApiKey`, sempre vazio)
- `.env.example` (a chave `GEMINI_API_KEY` pertence ao backend)
