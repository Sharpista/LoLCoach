# Higiene — Inventário de worktrees e branches órfãos

**Task:** t_c1e7799d · **Responsável:** github-profile · **Gerado em:** 2026-09-15 04:19
**Repositório:** /home/alexandre/LoLSaas · **Base de comparação:** `origin/develop` = `634a729`

> **Modo somente-leitura.** Nenhum worktree, branch, tag, stash ou arquivo foi removido, nenhum push/merge/deploy foi executado. As únicas escritas foram os artefatos de preservação em `/tmp/t_c1e7799d/` (arquivo de WIP, patch do stash, bundle do branch).

---

## 1. Resumo executivo

- **13 worktrees** registrados (12 secundários + 1 principal); nenhum prunable, nenhum diretório registrado ausente.
- **28 branches locais**: 26 contidos em `origin/develop`, 2 não contidos; destes, 18 são candidatos seguros a `git branch -d`.
- **7 dos 13 worktrees estão ativos agora** (`find -newermt '-30 min'`): `LoLSaas`, `t_30793c28`, `t_79bbaf4d`, `t_97a4840e`, `t_c1e7799d`, `t_c6893516`, `t_fa5d6855`.
- **4 worktrees citados na task** foram confirmados e classificados:
  - `t_871f11de` (`feat/006-ai-coach`) → **contém trabalho não versionado de maior valor** (Spec 006 AI Coach + Spec 005).
  - `t_fe545d9f` (`feat/005-recommendations`) → contém subconjunto do anterior (Spec 005 + analytics), versão mais antiga.
  - `t_ad378e4f` (`feat/004-dashboard`) → worktree limpo; os 4 commits são *patch-equivalentes* ao que já está em `develop`.
  - `t_c0f5ef38` (`qa/004-dashboard-integration`) → limpo, exceto **1 relatório de QA não versionado** (APROVADO).
- Além disso há WIP não versionado no **worktree principal** (`/home/alexandre/LoLSaas`), incluindo uma alteração de `AGENTS.md` (regras OpenViking + stack Angular) que nunca foi commitada.
- **Espaço potencialmente recuperável:** ~1.4 GB — somente worktrees **ociosos** e já integrados (majoritariamente `frontend/node_modules`, `bin/`, `obj/`).

**Nada deve ser removido antes da autorização explícita do usuário** (ver §7 e §8).

**Atenção — repositório em uso concorrente:** o dispatcher está executando outras tasks ao mesmo tempo neste repositório (`chore/004-dashboard-spec-done` recebeu commit há 2 minutos; novos worktrees `t_97a4840e` e `t_fa5d6855` estão em uso ativo). Este inventário é um **retrato de 2026-09-15 04:19**; a lista de candidatos deve ser reconferida imediatamente antes de qualquer remoção (§7 Fase 0).

---

## 2. Inventário de worktrees

| Worktree | Branch | HEAD | Contido em develop | ahead/behind | Working tree | Ocioso | Tam. |
|---|---|---|---|---|---|---|---|
| `$REPO` **(ativo)** | `fix/player-upsert-concurrency` | `06af613` | sim | 0/13 | 12 M · 1 ? | 3 min | 2.0G |
| `/home/alexandre/LoLCoach-001-player-search-frontend` | `feat/001-player-search-frontend` | `c079c66` | sim | 0/9 | 0 M · 0 ? | 3543 min | 288M |
| `/home/alexandre/LoLCoach-design-system` | `feat/design-system-tailwind` | `620bb9e` | sim | 0/7 | 0 M · 0 ? | 613 min | 306M |
| `$REPO/.worktrees/t_30793c28` **(ativo)** | `chore/repo-dirty-inventory` | `06af613` | sim | 0/13 | 0 M · 0 ? | 13 min | 968K |
| `$REPO/.worktrees/t_79bbaf4d` **(ativo)** | `chore/004-dashboard-spec-done` | `e2a105e` | **não** | 1/0 | 0 M · 0 ? | 9 min | 386M |
| `$REPO/.worktrees/t_871f11de` | `feat/006-ai-coach` | `06af613` | sim | 0/13 | 17 M · 47 ? | 443 min | 86M |
| `$REPO/.worktrees/t_97a4840e` **(ativo)** | `review/004-dashboard-closeout` | `06af613` | sim | 0/13 | 0 M · 0 ? | 8 min | 968K |
| `$REPO/.worktrees/t_ad378e4f` | `feat/004-dashboard` | `6d28100` | **não** | 4/13 | 0 M · 0 ? | 128 min | 386M |
| `$REPO/.worktrees/t_c0f5ef38` | `qa/004-dashboard-integration` | `06af613` | sim | 0/13 | 0 M · 1 ? | 182 min | 284M |
| `$REPO/.worktrees/t_c1e7799d` **(ativo)** | `chore/orphan-worktree-inventory` | `06af613` | sim | 0/13 | 0 M · 0 ? | 13 min | 968K |
| `$REPO/.worktrees/t_c6893516` **(ativo)** | `chore/004-verify-frontend` | `06af613` | sim | 0/13 | 0 M · 0 ? | 14 min | 968K |
| `$REPO/.worktrees/t_fa5d6855` **(ativo)** | `(detached)` | `634a729` | sim | 0/0 | 0 M · 0 ? | 3 min | 386M |
| `$REPO/.worktrees/t_fe545d9f` | `feat/005-recommendations` | `06af613` | sim | 0/13 | 16 M · 38 ? | 531 min | 86M |

`M` = arquivo rastreado modificado · `?` = não rastreado (`git status -uall`) · `Ocioso` = tempo desde a modificação de arquivo mais recente no diretório (fora de `.git`).

### 2.1 Classificação por worktree

| Worktree | Classe | Justificativa |
|---|---|---|
| `/home/alexandre/LoLSaas (principal)` | **MANTER** | Worktree primário: **não é removível**. WIP precisa ser triado (§4). |
| `LoLCoach-001-player-search-frontend` | **REMOVER (ocioso, 2 dias)** | Limpo; branch em `develop`; upstream remoto já deletado (`gone`). |
| `LoLCoach-design-system` | **REMOVER (ocioso, 10h)** | Limpo; branch `feat/design-system-tailwind` em `develop` (PR #11). |
| `t_30793c28` | **NÃO TOCAR (ativo)** | Worktree em uso por worker concorrente agora; reclassificar quando ocioso. |
| `t_79bbaf4d` | **NÃO TOCAR (ativo)** | Worktree em uso por worker concorrente agora; reclassificar quando ocioso. |
| `t_871f11de` | **ARQUIVAR → REMOVER (ocioso)** | Branch `feat/006-ai-coach` já em `develop`; 18 arquivos únicos **não versionados** (Spec 006 AI Coach + Spec 005) a preservar antes. |
| `t_97a4840e` | **NÃO TOCAR (ativo)** | Worktree em uso por worker concorrente agora; reclassificar quando ocioso. |
| `t_ad378e4f` | **REMOVER (ocioso, após arquivar bundle)** | Worktree limpo; 4 commits *patch-equivalentes* ao conteúdo já integrado (ver §3.1). |
| `t_c0f5ef38` | **ARQUIVAR → REMOVER (ocioso)** | Branch em `develop`; 1 relatório de QA não versionado a preservar. |
| `t_c1e7799d` | **MANTER** | Worktree da task atual. |
| `t_c6893516` | **NÃO TOCAR (ativo)** | Worktree em uso por worker concorrente agora; reclassificar quando ocioso. |
| `t_fa5d6855` | **NÃO TOCAR (ativo)** | Worktree em uso por worker concorrente agora; reclassificar quando ocioso. |
| `t_fe545d9f` | **ARQUIVAR → REMOVER (ocioso)** | Branch `feat/005-recommendations` já em `develop`; 8 arquivos únicos, subconjunto/versão antiga do worktree `t_871f11de`. |

---

## 3. Branches locais (28)

### 3.1 Contidos em `origin/develop` (26)

| Branch | SHA | Upstream | Assunto | Remover? |
|---|---|---|---|---|
| `chore/003-analytics-integration` | `fe850f2` | origin/chore/003-analytics-integration | docs(spec-002/003): promover para REVIEW | sim (`git branch -d`) |
| `chore/004-verify-backend` | `06af613` | - | fix: make player upsert concurrency-safe | sim (`git branch -d`) |
| `chore/004-verify-frontend` | `06af613` | - | fix: make player upsert concurrency-safe | não (worktree ativo) |
| `chore/orphan-worktree-inventory` | `06af613` | - | fix: make player upsert concurrency-safe | não (worktree ativo) |
| `chore/reconcile-pr-stack` | `06af613` | - | fix: make player upsert concurrency-safe | sim (`git branch -d`) |
| `chore/repo-dirty-inventory` | `06af613` | - | fix: make player upsert concurrency-safe | não (worktree ativo) |
| `develop` | `634a729` | origin/develop | Merge pull request #13 from Sharpista/feat/004-dashb | não (vida longa) |
| `feat/001-player-search` | `e55687b` | origin/feat/001-player-search | ci: adicionar esteiras backend/frontend/python e dep | sim (`git branch -d`) |
| `feat/001-player-search-frontend` | `c079c66` | origin/feat/001-player-search-frontend | feat(player-search): conclui integração frontend da  | sim, **após** remover o worktree (Fase 2 → Fase 3) |
| `feat/004-dashboard-integration` | `26547f4` | origin/feat/004-dashboard-integration | feat(dashboard): consumir GET /api/players/{id}/anal | sim (`git branch -d`) |
| `feat/005-recommendations` | `06af613` | - | fix: make player upsert concurrency-safe | sim, **após** remover o worktree (Fase 2 → Fase 3) |
| `feat/006-ai-coach` | `06af613` | - | fix: make player upsert concurrency-safe | sim, **após** remover o worktree (Fase 2 → Fase 3) |
| `feat/007-swagger` | `581e4de` | origin/feat/007-swagger | merge: resolve Swagger PR with develop | sim (`git branch -d`) |
| `feat/cors` | `4ae9725` | origin/feat/cors | feat(backend): adicionar política CORS para o fronte | sim (`git branch -d`) |
| `feat/design-system-tailwind` | `620bb9e` | origin/develop | feat(frontend): migrar para Tailwind v4 + design sys | sim, **após** remover o worktree (Fase 2 → Fase 3) |
| `feat/frontend` | `726c1f5` | origin/feat/frontend | feat(frontend): adicionar frontend Angular com busca | sim (`git branch -d`) |
| `feat/spec-006-gemini` | `03d9511` | origin/feat/spec-006-gemini | docs(spec-006): decidir LLM Gemini 3.8 | sim (`git branch -d`) |
| `feat/supabase-db` | `ffc5ad0` | origin/feat/supabase-db | docs(infra): documentar conexão Supabase (PostgreSQL | sim (`git branch -d`) |
| `fix/openapi-search-schema` | `96c239e` | origin/fix/openapi-search-schema | fix: align search player OpenAPI schema | sim (`git branch -d`) |
| `fix/player-search-pooler-persistence` | `05fd756` | origin/fix/player-search-pooler-persistence | fix(backend): evitar timeout do upsert no pooler Sup | sim (`git branch -d`) |
| `fix/player-upsert-concurrency` | `06af613` | origin/fix/player-upsert-concurrency | fix: make player upsert concurrency-safe | não (worktree ativo) |
| `homologacao` | `7c776a7` | origin/homologacao | Update cache configuration in frontend CI workflow ( | não (vida longa) |
| `main` | `7c776a7` | origin/main | Update cache configuration in frontend CI workflow ( | não (vida longa) |
| `qa/004-dashboard-integration` | `06af613` | - | fix: make player upsert concurrency-safe | sim, **após** remover o worktree (Fase 2 → Fase 3) |
| `review/004-dashboard-analytics` | `06af613` | - | fix: make player upsert concurrency-safe | sim (`git branch -d`) |
| `review/004-dashboard-closeout` | `06af613` | - | fix: make player upsert concurrency-safe | não (worktree ativo) |

### 3.2 NÃO contidos em `origin/develop` (2)

- `chore/004-dashboard-spec-done` (`e2a105e`) — docs(spec-004): promover dashboard para REVIEW — worktree `t_79bbaf4d` **ATIVO agora**
- `feat/004-dashboard` (`6d28100`) — feat(dashboard): consumir GET /api/players/{id}/analysis via — worktree `t_ad378e4f`, ocioso há 128 min

Verificação de equivalência de patches para `feat/004-dashboard`:

```
- f22fc07a69ff02e33d5d9506f03c4b37884f030a
- 1aa4190bcdb836e833772095753dcc6671ae90b4
- 36bb42fd3b5bf3a5d326f57b8816c8ead1b2d3e2
- 6d281004808de651ef6b091c388443f092fd7fd2
```

Todas as linhas começam com `-` → cada patch já existe em `origin/develop` por outro commit (o PR #13 integrou o conteúdo de `feat/004-dashboard-integration`). **Não há conteúdo exclusivo** nesse branch; ainda assim ele foi empacotado em `feat-004-dashboard.bundle` (§6) por segurança.

### 3.3 Branches de vida longa (não remover)

`develop`, `main`, `homologacao` — mantidos.

### 3.4 Branches remotas (`origin`)

15 branches remotas, todas com contraparte local. **Nenhuma remoção remota é proposta nesta task** (exigiria autorização separada, pois afeta o repositório compartilhado).

---

## 4. Conteúdo NÃO versionado relevante

Método: para cada arquivo reportado por `git status --porcelain -uall` calculei o hash do blob (`sha1("blob <len>\0" + bytes)`) e comparei com o blob equivalente em `origin/develop`. Arquivos idênticos a `develop` são apenas *conteúdo já integrado que nunca foi commitado no worktree* → descartáveis. Arquivos ausentes ou diferentes são **trabalho exclusivo** → preservar.

### 4.1 `t_871f11de`

Origem: `/home/alexandre/LoLSaas/.worktrees/t_871f11de` · idênticos a `origin/develop`: **46** · exclusivos: **18** · WIP mais recente: 2026-09-14 20:54

| Arquivo | Bytes | sha1(blob) | mtime |
|---|---|---|---|
| `backend/src/LoLCoach.Api/Program.cs` | 4866 | `a7382e699f9b` | 2026-09-14 20:54 |
| `specs/005-recommendations/tasks.md` | 351 | `1531356114e2` | 2026-09-14 20:54 |
| `specs/006-ai-coach/tasks.md` | 431 | `e97e71835d09` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Analytics/Recommendations/Recommendation.cs` | 277 | `493fc58a2a14` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Analytics/Recommendations/RecommendationEngine.cs` | 2589 | `79ded175b496` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Application/AiCoach.cs` | 5956 | `c36901c22940` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Application/AiCoachOptions.cs` | 591 | `911fd18181a8` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Application/AiCoachPromptBuilder.cs` | 1174 | `3a14ae3af782` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Application/CoachAnalysisInput.cs` | 216 | `4de69bac5035` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Application/CoachReport.cs` | 334 | `07b0609054bf` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Application/IAiCoach.cs` | 183 | `829ede0187b1` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Application/ICoachReportProvider.cs` | 204 | `e176685b221b` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Application/PerformanceAnalysisDto.cs` | 1362 | `96bb868a6d75` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Application/PerformanceAnalysisService.cs` | 5153 | `2a0063d6fe5f` | 2026-09-14 20:54 |
| `backend/src/LoLCoach.Api/Infrastructure/GeminiCoachReportProvider.cs` | 2614 | `44a3baccd026` | 2026-09-14 20:54 |
| `backend/tests/LoLCoach.Tests/AiCoachTests.cs` | 4029 | `cd83a4145785` | 2026-09-14 20:54 |
| `backend/tests/LoLCoach.Tests/PerformanceAnalysisEndpointTests.cs` | 8152 | `a2c772e6f153` | 2026-09-14 20:54 |
| `backend/tests/LoLCoach.Tests/RecommendationEngineTests.cs` | 1978 | `05192c76c3ab` | 2026-09-14 20:54 |

### 4.2 `t_fe545d9f`

Origem: `/home/alexandre/LoLSaas/.worktrees/t_fe545d9f` · idênticos a `origin/develop`: **46** · exclusivos: **8** · WIP mais recente: 2026-09-14 19:26

| Arquivo | Bytes | sha1(blob) | mtime |
|---|---|---|---|
| `backend/src/LoLCoach.Api/Program.cs` | 4370 | `b92dc26fad52` | 2026-09-14 19:19 |
| `specs/005-recommendations/tasks.md` | 351 | `1531356114e2` | 2026-09-14 19:26 |
| `backend/src/LoLCoach.Api/Analytics/Recommendations/Recommendation.cs` | 277 | `493fc58a2a14` | 2026-09-14 19:19 |
| `backend/src/LoLCoach.Api/Analytics/Recommendations/RecommendationEngine.cs` | 2589 | `79ded175b496` | 2026-09-14 19:19 |
| `backend/src/LoLCoach.Api/Application/PerformanceAnalysisDto.cs` | 1333 | `c9c1859afc1a` | 2026-09-14 19:19 |
| `backend/src/LoLCoach.Api/Application/PerformanceAnalysisService.cs` | 4836 | `24875d0a90c6` | 2026-09-14 19:19 |
| `backend/tests/LoLCoach.Tests/PerformanceAnalysisEndpointTests.cs` | 6786 | `59476c847030` | 2026-09-14 19:20 |
| `backend/tests/LoLCoach.Tests/RecommendationEngineTests.cs` | 1978 | `05192c76c3ab` | 2026-09-14 19:20 |

### 4.3 `t_c0f5ef38`

Origem: `/home/alexandre/LoLSaas/.worktrees/t_c0f5ef38` · idênticos a `origin/develop`: **0** · exclusivos: **1** · WIP mais recente: 2026-09-15 01:17

| Arquivo | Bytes | sha1(blob) | mtime |
|---|---|---|---|
| `qa-validation-report-004-dashboard.md` | 7486 | `51224cd87093` | 2026-09-15 01:17 |

### 4.4 `main-worktree`

Origem: `/home/alexandre/LoLSaas` · idênticos a `origin/develop`: **7** · exclusivos: **6** · WIP mais recente: 2026-09-15 00:34

| Arquivo | Bytes | sha1(blob) | mtime |
|---|---|---|---|
| `AGENTS.md` | 3212 | `e03f0a74048f` | 2026-09-14 23:05 |
| `backend/src/LoLCoach.Api/Program.cs` | 3308 | `40697285deed` | 2026-09-12 07:22 |
| `frontend/angular.json` | 2314 | `067cbfed30c4` | 2026-09-15 00:34 |
| `specs/001-player-search/spec.md` | 2095 | `27f8c73bbf97` | 2026-09-14 16:58 |
| `specs/001-player-search/tasks.md` | 2164 | `66633cd38029` | 2026-09-14 16:58 |
| `specs/004-dashboard/design.md` | 1053 | `ea31bf36f054` | 2026-09-14 17:23 |

### 4.5 Leitura do conteúdo exclusivo

- **`t_871f11de` (maior valor):** implementação da Spec 006 (AI Coach) — `AiCoach`, `IAiCoach`, `AiCoachOptions`, `AiCoachPromptBuilder`, `CoachAnalysisInput`, `CoachReport`, `ICoachReportProvider`, `GeminiCoachReportProvider`, `AiCoachTests` — e da Spec 005 (`Recommendation`, `RecommendationEngine`, `RecommendationEngineTests`), além de `Program.cs` com todo o wiring de DI e as marcações `[x]` em `specs/005-recommendations/tasks.md` e `specs/006-ai-coach/tasks.md`. **Nenhum desses arquivos existe em `origin/develop`.**
- **`t_fe545d9f`:** subconjunto/versão anterior do acima (sem os arquivos de AI Coach; `Program.cs` sem o wiring do coach). `RecommendationEngine.cs`, `RecommendationEngineTests.cs` e `specs/005-.../tasks.md` são byte-idênticos aos de `t_871f11de`; `PerformanceAnalysis*.cs` e `PerformanceAnalysisEndpointTests.cs` são variantes mais antigas.
- **`t_c0f5ef38`:** `qa-validation-report-004-dashboard.md` (7.486 B) — relatório de QA da Spec 004 com status **APROVADO** (backend 67/67, frontend 16/16, 12 cenários de UI e integração). Não existe em `develop`.
- **`main-worktree`:** destacam-se `AGENTS.md` (**regras de memória compartilhada OpenViking + stack de frontend Angular 22** — nunca commitadas) e atualizações de spec (`specs/001-player-search/spec.md` → `status: DONE` + decisão de navegação; `specs/001-player-search/tasks.md`; `specs/004-dashboard/design.md` + design system). Em contrapartida, `backend/src/LoLCoach.Api/Program.cs` e `frontend/angular.json` são **variantes antigas** (o `Program.cs` não tem o wiring de analytics/match-import; o `angular.json` volta para SCSS) — servem como registro, **não devem ser aplicados**.

### 4.6 Conteúdo descartável (idêntico a develop)

Resumo por worktree (arquivos que já estão em `develop` mas aparecem como modificados/não rastreados porque ficaram pendurados no commit-base 06af613):

- `t_871f11de`: 46 arquivos
- `t_fe545d9f`: 46 arquivos
- `main-worktree`: 7 arquivos

Exemplos no worktree principal: `RiotRegions.cs`, `SearchPlayerValidator.cs`, `HostTests.cs`, `specs/007-swagger/*`, `SearchPlayerSchemaFilter.cs` — todos byte-idênticos a `origin/develop`.

### 4.7 Stash

```
stash@{0}: On fix/player-upsert-concurrency: hermes-preserve-mixed-fixes
```

`stash@{0}` (`hermes-preserve-mixed-fixes`) foi criado sobre `fix/player-upsert-concurrency` (base `8917bea`). Conteúdo: 9 arquivos rastreados + 1 não rastreado (`backend/src/LoLCoach.Api/Infrastructure/SearchPlayerSchemaFilter.cs`).

Verificação arquivo a arquivo (`scripts/stash_check.py`): **os 9 arquivos rastreados do stash são byte-idênticos aos arquivos atuais do worktree principal** (portanto o stash está integralmente preservado no working tree de lá). A parte não rastreada (`stash^3`) contém um blob **vazio** para `SearchPlayerSchemaFilter.cs`, enquanto o arquivo em disco no worktree principal é idêntico ao de `develop` — ou seja, o stash não guarda nada que não esteja também no disco ou em `develop`.

Ressalva: `git apply --reverse --check` do patch do stash falha em `Program.cs` porque o patch foi produzido contra a base `8917bea` (não contra o HEAD atual `06af613`) — não é divergência de conteúdo. Recomendação: **manter o stash intacto** até o WIP do worktree principal ser triado; o patch completo (incluindo não rastreados) está exportado em §6.

### 4.8 Ruído recorrente de `git status`

`.gitignore` **não** cobre `.worktrees/` nem `.angular/`:

```
- .worktrees -> ausente no .gitignore
- .angular   -> ausente no .gitignore
- node_modules -> presente
```

Sem isso, `git status` no worktree principal lista os 12 diretórios de worktree + o cache do Angular como não rastreados (foi provavelmente a origem de tasks anteriores de "repo dirty inventory"). Sugestão (mudança separada, requer autorização): adicionar `.worktrees/` e `.angular/` ao `.gitignore`.

---

## 5. O que arquivar vs. o que preservar

### 5.1 PRESERVAR

| # | Item | Onde | Ação recomendada |
|---|---|---|---|
| P1 | Spec 006 AI Coach + Spec 005 (18 arquivos, ~44 KB) | `t_871f11de` | Recuperar como base de uma nova branch de task (não commitar direto em develop). |
| P2 | Relatório QA Spec 004 (`qa-validation-report-004-dashboard.md`) | `t_c0f5ef38` | Mover para `docs/`/`specs/004-dashboard/` via PR, ou manter apenas como artefato anexado à task. |
| P3 | `AGENTS.md` (OpenViking + stack Angular) + atualizações de spec 001/004 (~12 KB) | worktree principal | Commitar em branch própria e abrir PR — são regras de projeto em vigor. |
| P4 | `stash@{0}` | worktree principal | Manter intacto até P3 ser resolvido. |
| P5 | `feat/004-dashboard` (bundle) | `t_ad378e4f` | Conteúdo já em develop; bundle guardado apenas como prova/rollback. |
| P6 | Spec 005 / analytics (8 arquivos) | `t_fe545d9f` | Redundante com P1; preservar apenas no arquivo (menor prioridade). |

### 5.2 ARQUIVAR E DESCARTAR (após autorização)

- Worktrees **ociosos** sem WIP exclusivo: `LoLCoach-001-player-search-frontend`, `LoLCoach-design-system`, `t_ad378e4f`.
- Worktrees ociosos **com WIP já arquivado** (§6): `t_871f11de`, `t_fe545d9f`, `t_c0f5ef38`.
- **Excluídos por atividade concorrente** (worktrees usados por outros workers agora, não remover nesta passagem): `LoLSaas`, `t_30793c28`, `t_79bbaf4d`, `t_97a4840e`, `t_c6893516`, `t_fa5d6855`.
- No worktree principal: os 7 arquivos byte-idênticos a `develop` (descartar) — **mantendo** o WIP de P3.
- Branches locais marcadas "sim" na tabela §3.1 (`git branch -d`) — 18 branches.
- Branches de worktrees ativos e `chore/orphan-worktree-inventory` (esta task): **não remover**.
- Após a limpeza: `git branch -D feat/004-dashboard` (só com autorização explícita, pois `-d` recusará) e `git worktree prune`.

### 5.3 NÃO TOCAR

- `develop`, `main`, `homologacao` e o worktree principal.
- Worktrees com atividade recente (workers concorrentes) e os branches que eles têm checados, incluindo `chore/004-dashboard-spec-done` (`e2a105e`, commit de 2 minutos antes deste relatório).
- Qualquer branch remota.
- Tags/releases (nenhuma foi avaliada para remoção).

---

## 6. Artefatos de preservação já gerados (somente leitura sobre o repo)

Todos em `/tmp/t_c1e7799d/` — nenhum deles altera o repositório:

| Arquivo | Conteúdo | Bytes |
|---|---|---|
| `orphan-worktree-preservation-2026-09-15.tar.gz` | — | 256353 |
| `preservation/MANIFEST.txt` | — | 3799 |
| `stash-hermes-preserve-mixed-fixes.patch` | — | 19064 |
| `main-worktree-dirty.patch` | — | 15597 |
| `feat-004-dashboard.bundle` | — | 198483 |
| `classification.json` | — | 10985 |
| `unique-files.json` | — | 7922 |
| `verification.txt` | — | 65 |

O tarball é auto-contido: `preservation/<worktree>/...` (arquivos exclusivos, no caminho relativo do repo, + `MANIFEST.txt` com sha1/size/mtime + `README.txt`), `scripts/*.py` (os scripts que geraram este inventário, somente leitura), o patch do stash, o diff do worktree principal e o bundle do branch.

**Cópia durável** (fora de `/tmp`, que pode ser limpo em reboot): `/home/alexandre/backups/lolsaas-wip-2026-09-15/` contém o mesmo tarball, os patches, o bundle, `MANIFEST.txt`, `classification.json`, `unique-files.json` e uma cópia deste relatório.

**Verificação de integridade executada** (`scripts/verify_tar.py`): o tarball foi extraído em diretório temporário e os **33 arquivos preservados** foram comparados byte a byte com as origens — 33 idênticos, 0 divergentes. `git bundle verify` retornou `rc=0` ("The bundle records a complete history"). Este relatório e o tarball também estão **anexados à task t_c1e7799d** no board (durável, independente de `/tmp`).

```
sha256: 38f11bca1104f1835c536de64608b995c36d45f66f89803c86b3ca129fb6c6b2
tamanho: 256353 bytes
```

### 6.1 Como restaurar depois de uma remoção

```bash
# extrair o WIP arquivado
tar -xzf /tmp/t_c1e7799d/orphan-worktree-preservation-2026-09-15.tar.gz -C /tmp/restore
# reaplicar o stash a partir do patch (se necessário)
git -C /home/alexandre/LoLSaas apply --3way /tmp/restore/stash-hermes-preserve-mixed-fixes.patch
# recriar o branch feat/004-dashboard a partir do bundle
git -C /home/alexandre/LoLSaas fetch /tmp/restore/feat-004-dashboard.bundle feat/004-dashboard:restored/feat-004-dashboard
# recriar um worktree, se preciso
git -C /home/alexandre/LoLSaas worktree add /home/alexandre/LoLSaas/.worktrees/restored-006 feat/006-ai-coach
```

---

## 7. Proposta de limpeza (comandos reversíveis — NÃO EXECUTADOS)

Ordem recomendada. **Cada fase requer autorização explícita antes de ser executada.**

### Fase 0 — pré-checagem OBRIGATÓRIA (segura, somente leitura)

O repositório é compartilhado com outros workers do dispatcher. Antes de qualquer remoção:

```bash
git -C /home/alexandre/LoLSaas fetch origin
git -C /home/alexandre/LoLSaas worktree list
for w in /home/alexandre/LoLSaas/.worktrees/*; do
  echo "== $w"; git -C "$w" status --porcelain -uall | head
  find "$w" -path '*/.git' -prune -o -type f -newermt '-30 minutes' -print | head
done
git -C /home/alexandre/LoLSaas worktree prune --dry-run -v
```

Critério de bloqueio: **qualquer worktree alvo com status sujo ou atividade nos últimos 30 minutos sai da lista** até ficar ocioso e ser reconferido.

### Fase 1 — triagem do WIP do worktree principal (P3)

```bash
cd /home/alexandre/LoLSaas
git switch -c chore/agents-and-specs-updates       # sai de fix/player-upsert-concurrency
git add AGENTS.md specs/001-player-search/spec.md specs/001-player-search/tasks.md specs/004-dashboard/design.md
git status                                          # revisar o diff antes do commit
git commit -m "docs: registrar regras de memoria compartilhada e atualizar specs 001/004"
# executar build + testes antes de qualquer push (AGENTS.md)
```
Comandos reversíveis/descartáveis para o ruído já integrado (após revisão do diff):

```bash
git restore backend/src/LoLCoach.Api/Application/RiotRegions.cs \
            backend/src/LoLCoach.Api/Application/SearchPlayerValidator.cs \
            backend/src/LoLCoach.Api/Infrastructure/SearchPlayerSchemaFilter.cs \
            backend/tests/LoLCoach.Tests/HostTests.cs \
            specs/007-swagger/
git clean -n -- backend/src/LoLCoach.Api/Infrastructure/SearchPlayerSchemaFilter.cs  # dry-run
```

### Fase 2 — remover worktrees (somente os ociosos listados na §2.1)

```bash
for w in t_871f11de t_ad378e4f t_c0f5ef38 t_fe545d9f; do
  git -C /home/alexandre/LoLSaas worktree remove --dry-run "/home/alexandre/LoLSaas/.worktrees/$w" 2>&1 || true
done
git -C /home/alexandre/LoLSaas worktree remove --dry-run /home/alexandre/LoLCoach-001-player-search-frontend || true
git -C /home/alexandre/LoLSaas worktree remove --dry-run /home/alexandre/LoLCoach-design-system || true
# depois da conferência, remover um a um (sem --force; o git recusa worktree sujo) :
git -C /home/alexandre/LoLSaas worktree remove /home/alexandre/LoLCoach-001-player-search-frontend
git -C /home/alexandre/LoLSaas worktree remove /home/alexandre/LoLCoach-design-system
git -C /home/alexandre/LoLSaas worktree remove /home/alexandre/LoLSaas/.worktrees/t_871f11de
git -C /home/alexandre/LoLSaas worktree remove /home/alexandre/LoLSaas/.worktrees/t_ad378e4f
git -C /home/alexandre/LoLSaas worktree remove /home/alexandre/LoLSaas/.worktrees/t_c0f5ef38
git -C /home/alexandre/LoLSaas worktree remove /home/alexandre/LoLSaas/.worktrees/t_fe545d9f
```

> `t_c0f5ef38`, `t_871f11de` e `t_fe545d9f` têm conteúdo não rastreado: **só remover depois** de confirmar que o WIP está no tarball (§6) — o `worktree remove` sem `--force` recusa nesses casos. Nenhum deles é o worktree atual de outro worker no momento deste relatório (todos ociosos há horas).

### Fase 3 — remover branches locais já integradas (`-d` só apaga o que está contido em develop)

```bash
git -C /home/alexandre/LoLSaas branch -d \
  chore/003-analytics-integration chore/004-verify-backend chore/reconcile-pr-stack \
  feat/001-player-search feat/001-player-search-frontend feat/004-dashboard-integration \
  feat/005-recommendations feat/006-ai-coach feat/007-swagger feat/cors feat/design-system-tailwind \
  feat/frontend feat/spec-006-gemini feat/supabase-db fix/openapi-search-schema \
  fix/player-search-pooler-persistence qa/004-dashboard-integration review/004-dashboard-analytics
```

(18 branches. `git branch -d` recusa qualquer branch não totalmente contido em `develop`, portanto é seguro por construção.)

### Fase 4 — branch divergente-com-conteúdo-integrado (exige `-D` ⇒ autorização explícita)

```bash
git -C /home/alexandre/LoLSaas branch -D feat/004-dashboard    # SOMENTE com autorização explícita
git -C /home/alexandre/LoLSaas worktree prune -v
```

### Fase 5 — higiene do `.gitignore` (requer autorização)

```bash
cd /home/alexandre/LoLSaas
printf '.worktrees/\n.angular/\n' >> .gitignore
git add .gitignore && git commit -m "chore: ignorar .worktrees/ e .angular/"
```

---

## 8. Riscos, mitigação e rollback

| Risco | Probabilidade | Impacto | Mitigação aplicada / proposta |
|---|---|---|---|
| Perder trabalho não versionado das specs 006/005 | Média | Alto | Tarball + MANIFEST com sha1 (§6); remoção só depois de conferir |
| Perder o relatório de QA da Spec 004 | Baixa | Médio | Anexado à task e incluído no tarball |
| Perder o bundle/branch `feat/004-dashboard` | Muito baixa | Baixo | `git cherry` provou equivalência de patches; bundle guardado |
| Perder o stash | Baixa | Médio | Patch exportado; stash permanece intacto |
| Remover worktree com diretório em uso | Baixa | Baixo | `git worktree remove` sem `--force` falha com segurança |
| **Colisão com workers concorrentes** (worktree/branch em uso por outra task) | Alta | Médio | Fase 0 obrigatória: excluir qualquer worktree com atividade < 30 min ou status sujo |
| **Irreversível:** `rm -rf`, `git clean -fd`, `worktree remove --force` | — | Alto | **Não propostos / não executados** |

Rollback de qualquer fase: os worktrees são recriáveis com `git worktree add <path> <branch>` (todas as branches envolvidas apontam para commits já existentes no repositório), e o WIP volta do tarball/bundle (§6.1).

---

## 9. Pendência (BLOCKED)

Este levantamento **para aqui** e aguarda autorização explícita do usuário. Nenhuma remoção, deleção de branch, `rm`, `push`, `merge` ou `deploy` foi executada.

Decisões necessárias:

1. Autorizar a remoção dos worktrees **ociosos** listados na §2.1 (Fase 2)?
2. Autorizar a remoção dos worktrees com WIP **após** extração/validação do tarball? Ou preferir recuperar primeiro o trabalho de Spec 006/005 abrindo uma branch a partir de `feat/006-ai-coach`?
3. Autorizar a deleção das branches integradas marcadas "sim" na §3.1 (Fase 3) e de `feat/004-dashboard` com `-D` (Fase 4)?
4. Autorizar a triagem/commit do WIP do worktree principal (`AGENTS.md` + specs, Fase 1) e a higiene do `.gitignore` (Fase 5)?
5. O relatório de QA da Spec 004 deve virar arquivo versionado (PR em `specs/004-dashboard/`) ou basta o anexo da task?

Observação operacional: como o repositório está em uso por outros workers, recomenda-se executar a limpeza somente quando a fila do dispatcher estiver ociosa, ou limitar a remoção aos worktrees que continuarem ociosos na pré-checagem (Fase 0).

