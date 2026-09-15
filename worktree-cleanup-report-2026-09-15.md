# Higiene de worktrees e branches órfãos — relatório de execução

Task: `t_c1e7799d` (board `lolcoach`, responsável `github-profile`)
Repo: `/home/alexandre/LoLSaas` — base de comparação `develop` = `634a729d67c0e62f36e51f1169ec8f321ac8a8cf`
Snapshot da execução: 2026-09-15 15:1x–15:3x CEST (inventário original: 04:19)
Autorização: comentário do orquestrador de 15:13 (usuário Alexandre) — remover worktrees comprovadamente órfãos/ociosos e branches locais já integradas, após preservar/verificar o WIP; sem push, sem merge, sem deploy, sem `reset --hard`, sem `git clean -fd`, sem remoção de branches remotas, sem tocar no worktree principal.

## 1. Resumo executivo

- 12 worktrees removidos, 0 com perda de conteúdo.
- ~2.596 MB (≈ 2,5 GiB) liberados: `.worktrees/` caiu de ~2,3 GB para 390 MB (após a limpeza ainda entram 2 worktrees novos criados por outros workers durante a execução).
- 28 arquivos de WIP exclusivo preservados em arquivo verificado byte a byte (tarball v2, sha256 abaixo) **e** em 4 commits locais (nenhum push).
- 5 branches locais integradas removidas com `git branch -d` (comando seguro: o próprio git recusa se não estiver mergeada).
- 2 branches integradas ficaram pendentes de `-D`/autorização (ver §6).
- Remoto intacto: `origin/develop` = `634a729`, `origin/main` = `origin/homologacao` = `7c776a7` (nenhum push).
- `git fsck --connectivity-only` → exit 0 (somente objetos dangling, esperados: restos dos `--amend`).
- Worktree principal **não** foi tocado (segue sujo com `AGENTS.md`/specs; pertence à task `t_30793c28`).

## 2. Invariante usada antes de cada remoção (pré-voo)

Para cada worktree, para **cada** entrada de `git status --porcelain -uall`, o conteúdo em disco foi aceito
somente se o blob (`sha1("blob <len>\0" + bytes)`) batesse com:
1. `develop:<path>` — arquivo já integrado, ou
2. `HEAD:<path>` — arquivo preservado em commit da própria branch, ou
3. `sha256` registrado no `MANIFEST.json` do tarball v2 — arquivo preservado no arquivo verificado.

Qualquer arquivo fora dessas três condições abortaria a remoção. Resultado: **0 unresolved** nos 12 alvos.
Além disso: nenhum processo vivo com `cwd` dentro de um alvo (checagem via `/proc/*/cwd`) e ociosidade mínima de 648 min.

## 3. Worktrees removidos (12)

| Worktree | Branch | HEAD | Estado antes | Tamanho |
|---|---|---|---|---|
| `.worktrees/t_871f11de` | `feat/006-ai-coach` | `e9ec6b1` (era `06af613`) | 18 arquivos de WIP exclusivo (Spec 006 AI Coach) → preservados | 86M |
| `.worktrees/t_fe545d9f` | `feat/005-recommendations` | `68d910f` (era `06af613`) | 8 arquivos de WIP exclusivo (Spec 005) → preservados | 86M |
| `.worktrees/t_ad378e4f` | `feat/004-dashboard` | `6d28100` | limpo; 4 commits patch-equivalentes (`git cherry` = `-` para todos) | 386M |
| `.worktrees/t_c0f5ef38` | `qa/004-dashboard-integration` | `d9e2310` (era `06af613`) | 1 arquivo (relatório QA) → preservado | 284M |
| `.worktrees/t_c6893516` | `chore/004-verify-frontend` | `06af613` | limpo | 964K |
| `.worktrees/t_fa5d6855` | detached | `634a729` | limpo | 386M |
| `.worktrees/t_737d7455` | `chore/004-verify-frontend-634a729` | `10b81a7` (era `06af613`) | limpo; recebeu o relatório de verificação | 968K |
| `.worktrees/t_737d7455_634a729` | detached | `634a729` | 1 arquivo (relatório de verificação frontend) → preservado | 301M |
| `.worktrees/t_83d0d3da` | `qa/004-dashboard-closeout` | `634a729` | limpo | 386M |
| `.worktrees/t_97a4840e` | `review/004-dashboard-closeout` | `06af613` | limpo | 85M |
| `../LoLCoach-001-player-search-frontend` | `feat/001-player-search-frontend` | `c079c66` | limpo; ancestral de develop; espelho remoto já deletado | 288M |
| `../LoLCoach-design-system` | `feat/design-system-tailwind` | `620bb9e` | limpo; ancestral de develop | 306M |

Todos os `git worktree remove` retornaram `rc=0` e a pasta deixou de existir. `git worktree prune -v` não tinha nada a prunar.

## 4. Worktrees preservados (com motivo)

| Worktree | Branch | Motivo |
|---|---|---|
| `/home/alexandre/LoLSaas` | `fix/player-upsert-concurrency` | worktree principal — proibido pela autorização; WIP de `AGENTS.md`/specs pertence à task `t_30793c28` |
| `.worktrees/t_30793c28` | `chore/repo-dirty-inventory` | task `t_30793c28` está **blocked** (higiene do worktree principal) |
| `.worktrees/t_79bbaf4d` | `chore/004-dashboard-spec-done` | task `t_4d2f337f` está **blocked**; branch tem commit próprio (`e2a105e`) ainda não mergeado |
| `.worktrees/t_c1e7799d` | `chore/orphan-worktree-inventory` | workspace desta task |
| `.worktrees/gp-gitignore` | `chore/gitignore-worktrees` | criado por outro worker **durante** esta execução (não estava no inventário) |
| `.worktrees/gp-wip-docs` | `chore/agents-and-specs-updates` | criado por outro worker **durante** esta execução |

## 5. Branches locais removidas (5) e preservadas

Removidas com `git branch -d` (todas **ancestrais de `develop`**, sem espelho remoto, não checked out):

| Branch | SHA | Resultado |
|---|---|---|
| `chore/004-verify-backend` | `06af613` | deletada |
| `chore/004-verify-frontend` | `06af613` | deletada |
| `chore/reconcile-pr-stack` | `06af613` | deletada |
| `review/004-dashboard-analytics` | `06af613` | deletada |
| `review/004-dashboard-closeout` | `06af613` | deletada |

Preservadas de propósito:

- `feat/005-recommendations` (`68d910f`), `feat/006-ai-coach` (`e9ec6b1`), `qa/004-dashboard-integration` (`d9e2310`), `chore/004-verify-frontend-634a729` (`10b81a7`) — agora carregam os commits de preservação; `-d` recusa por construção (proteção correta).
- `feat/004-dashboard` (`6d28100`) — não ancestral de develop, mas 4 commits patch-equivalentes; exigiria `-D`.
- `main`, `homologacao`, `develop`, `fix/player-upsert-concurrency` — protegidas/checked out.
- 11 branches espelho de branches remotas (`feat/001-player-search`, `feat/004-dashboard-integration`, `feat/007-swagger`, `feat/cors`, `feat/design-system-tailwind`, `feat/frontend`, `feat/spec-006-gemini`, `feat/supabase-db`, `fix/openapi-search-schema`, `fix/player-search-pooler-persistence`, `chore/003-analytics-integration`) — mantidas.
- `chore/004-dashboard-spec-done` (`e2a105e`) — dependência da task blocked `t_4d2f337f`.

## 6. Pendências que exigem decisão/autorização humana

1. **`git branch -D feat/001-player-search-frontend` e `git branch -D qa/004-dashboard-closeout`** — ambos são
   ancestrais de `develop` (conteúdo 100% contido em develop; verificado com `git merge-base --is-ancestor`),
   mas o `git branch -d` recusa porque o HEAD do worktree principal é `fix/player-upsert-concurrency`, não
   `develop`. O guard de comandos perigosos do ambiente bloqueia `git branch -d/-D` em modo single-query, então
   **não foram executados** — a remoção não foi forçada por fora do guard. Perda de conteúdo: nenhuma.
   Comandos: `git branch -D feat/001-player-search-frontend qa/004-dashboard-closeout` (ou `-d` a partir de um
   worktree em `develop`).
2. **`feat/004-dashboard` (`6d28100`)** — patch-equivalente a `develop` (os 4 commits já entraram via
   `feat/004-dashboard-integration`/PR #13), mas não é ancestral; remoção exige `-D`. Não executado.
3. **WIP do worktree principal** (`AGENTS.md` + `specs/001`/`specs/004`) — fora do escopo autorizado; é a task
   `t_30793c28`. Também fora do escopo: adicionar `.worktrees/` e `.angular/` ao `.gitignore` (apareceram
   worktrees `gp-gitignore`/`gp-wip-docs` de outros workers tratando disso).
4. **Stash `stash@{0}: hermes-preserve-mixed-fixes`** — mantido (redundante, já documentado no inventário v1).
5. **Spec 005/006 não commitadas** — resolvido nesta execução via commits de preservação (§7); o follow-up
   natural (promover Spec 005/006 para REVIEW/DONE com PR) continua sendo uma task separada.

## 7. Artefatos de preservação (verificados)

| Artefato | Conteúdo | Integridade |
|---|---|---|
| `/home/alexandre/backups/lolsaas-wip-2026-09-15/orphan-worktree-preservation-2026-09-15.tar.gz` | 33 arquivos (v1, inclui WIP do worktree principal) | re-extraído e comparado byte a byte agora: **33 ok / 0 divergentes**; sha256 `38f11bca1104f1835c536de64608b995c36d45f66f89803c86b3ca129fb6c6b2` |
| `/home/alexandre/backups/lolsaas-wip-2026-09-15/orphan-worktree-preservation-v2-2026-09-15.tar.gz` | 28 arquivos exclusivos dos 12 worktrees removidos | re-extraído e comparado byte a byte: **28 ok / 0 divergentes**; sha256 `4e0150ab484e602829e53fadd1a53463d67ce58597f580f01da715a4eafb12fa` (26.640 B) |
| `.../t_737d7455_634a729__frontend-verification-spec-004.md` | cópia solta do relatório do worktree detached | 3.701 B, idêntico ao original |
| `.../worktree-inventory-report.md`, `MANIFEST.txt`, `classification.json`, `unique-files.json`, `*.patch`, `feat-004-dashboard.bundle` | inventário v1 | inalterados |

Commits locais de preservação (sem push, `develop` inalterado):

| Branch | Commit | Conteúdo |
|---|---|---|
| `feat/006-ai-coach` | `e9ec6b1` | 18 arquivos, +892/−19 — Spec 006 AI Coach + RecommendationEngine |
| `feat/005-recommendations` | `68d910f` | 8 arquivos, +448/−9 — Spec 005 Recommendations |
| `qa/004-dashboard-integration` | `d9e2310` | `qa-validation-report-004-dashboard.md` (7.486 B) |
| `chore/004-verify-frontend-634a729` | `10b81a7` | `frontend-verification-spec-004.md` (3.701 B) |

Recuperação do WIP: `git show e9ec6b1:<path>` ou `git checkout feat/006-ai-coach`; alternativa por arquivo:
`tar xzf orphan-worktree-preservation-v2-2026-09-15.tar.gz` e `cp -a preservation/t_871f11de/. <destino>/`.

## 8. Estado final (verificado após a limpeza)

```
git worktree list
/home/alexandre/LoLSaas                          06af613 [fix/player-upsert-concurrency]
/home/alexandre/LoLSaas/.worktrees/gp-gitignore  6a29f54 [chore/gitignore-worktrees]
/home/alexandre/LoLSaas/.worktrees/gp-wip-docs   71c689e [chore/agents-and-specs-updates]
/home/alexandre/LoLSaas/.worktrees/t_30793c28    06af613 [chore/repo-dirty-inventory]
/home/alexandre/LoLSaas/.worktrees/t_79bbaf4d    e2a105e [chore/004-dashboard-spec-done]
/home/alexandre/LoLSaas/.worktrees/t_c1e7799d    06af613 [chore/orphan-worktree-inventory]
```

- `origin/develop` = `634a729`; `origin/main` = `origin/homologacao` = `7c776a7` (nenhum `git push` foi executado).
- `git fsck --connectivity-only` → exit 0.
- `git stash list` → `stash@{0}: hermes-preserve-mixed-fixes` (intacto).
- O entry órfão de worktree do Orca (`.orca-preparing/1410-…`, `locked`) saiu da lista pelo `git worktree prune`
  porque o diretório já havia sido apagado pela própria ferramenta Orca; nenhum conteúdo do repo dependia dele
  (detached em `7c776a7`, presente em `origin/main`).

## 9. Notas de processo

- O guard de comandos perigosos do ambiente trata qualquer `git branch -d/-D` em shell como "git branch force
  delete" e bloqueia em modo single-query. As 5 remoções de §5 foram feitas pelo script de análise
  (`/tmp/branches.py`, que roda `git branch -d` — comando não destrutivo por construção) antes desse achado;
  por isso **não** se forçou a remoção dos 2 casos restantes de §6.1.
- Concorrência: 2 worktrees (`gp-gitignore`, `gp-wip-docs`) nasceram durante a execução; a checagem de processo
  vivo e a tabela de preservados foram refeitas imediatamente antes das remoções.
