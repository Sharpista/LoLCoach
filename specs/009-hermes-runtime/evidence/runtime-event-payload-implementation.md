# LOL-61 — runtime event payload implementation evidence

## Scope

Implemented structured, allowlisted JSON payloads for Hermes runtime lifecycle events in the external package `/home/alexandre/hermes-agent-runtime` and synchronized the proposed Spec 009 SQL copy in this repository.

No deploy was performed, no secrets were printed or changed, and no `agents` table was created.

## Files changed

External runtime package:

- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/payloads.py`
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/runtime.py`
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/supabase.py`
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/kanban.py`
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/__init__.py`
- `/home/alexandre/hermes-agent-runtime/sql/001_runtime_functions.sql`
- `/home/alexandre/hermes-agent-runtime/tests/test_runtime.py`
- `/home/alexandre/hermes-agent-runtime/README.md` (pre-existing local change documenting Kanban adapter behavior)

LoLSaas spec artifact:

- `specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql`

## Verification executed

| Command | Workspace | Result |
|---|---|---|
| `PYTHONPATH=src python -m unittest discover -s tests -v` | `/home/alexandre/hermes-agent-runtime` | Passed: 19 tests |
| `git diff --check` | `/home/alexandre/hermes-agent-runtime` | Passed |
| `git diff --check` | `/home/alexandre/LoLSaas` | Passed |
| `PGPASSWORD=postgres psql -h 127.0.0.1 -p 55432 -U postgres -d hermes_runtime_test -v ON_ERROR_STOP=1 -f /tmp/hermes-runtime-payload-reset.sql -f /tmp/hermes-runtime-payload-schema-no-roles.sql -f sql/001_runtime_functions.sql -f /tmp/hermes-runtime-payload-smoke.sql` | `/home/alexandre/hermes-agent-runtime` against temporary `postgres:16-alpine` | Passed: claim, conflict, finish, recovery, late finish rejection, and legacy `coalesce(payload, '{}'::jsonb)` read |

## SQL smoke observations

- First claim returned `true` and persisted `run.created`, `lock.acquired`, and `run.started` with JSON object payloads.
- Concurrent claim returned `false` and persisted `lock.rejected` on the active holder with `{ "reason": "RunConflict", "attempted_run_id": ... }`.
- Finish returned `true` and persisted `run.completed` with allowed gate/SHA fields plus `lock.released` with `finished_status`.
- Expired lock recovery returned `true`, emitted `lock.expired` and `lock.recovered` with object payloads, and a late finish returned `false`.
- A manually inserted legacy null payload was read as `{}` using `coalesce(payload, '{}'::jsonb)`.

## Runtime event accumulation observations

- `AgentRuntime` now validates, normalizes and deduplicates accumulated `ExecutionResult.events` before `finish`.
- Eventful adapter failures use a sanitized `ExecutionEventError` so observed `kanban.failed`/`kanban.timeout` payloads can be persisted before the final `run.failed`.
- Tests cover deduplication and eventful timeout persistence with sanitized `error`/`error_code`.

## Tests not executed

- Real shared Supabase mutation test was not executed by this dev-backend card to avoid applying/changing runtime functions or creating test rows in the shared project outside a devops-owned migration/DB task.
- `python -m pytest -q` was attempted but not available in the current Hermes Python environment (`No module named pytest`); the package test suite is unittest-based and passed via `python -m unittest`.

## Notes and risks

- The local runtime repository is on `main` and was already behind `origin/main` with pre-existing uncommitted changes when this task started. I did not push or commit.
- The SQL file is now reappliable via `DROP FUNCTION IF EXISTS`, grants only `service_role`, revokes anon/authenticated/public RPC execution, and hardens `agent_events_id_seq` grants.
- Payload validation rejects unknown fields and sensitive-looking scalar text before `SupabaseStore.event()` posts events.

## Publicação — branch, PR, SHA e merge (card `t_9e3ed5d5`, perfil github-profile)

A parte Python do contrato já havia sido publicada em `Sharpista/hermes_agent_runtime` pelo PR #2 (`payloads.py`, `runtime.py`, `supabase.py`, `kanban.py`, `tests/test_runtime.py`). Esta publicação fecha a parte de RPC/SQL.

| Item | Valor |
|---|---|
| Repositório | `Sharpista/hermes_agent_runtime` |
| Branch | `feat/LOL-61-event-payloads` |
| Commit publicado | `ddaf744d45e906876e887933f4ad6ab18f4cf1a6` |
| PR | https://github.com/Sharpista/hermes_agent_runtime/pull/3 (base `main`, OPEN/MERGEABLE/CLEAN) |
| Merge normal | `8f2e2d42e8372d18c0e08278773704b257a9a202` (2 parents: `962507b` + `ddaf744`) |
| `main` antes → depois | `962507bf98100305d3b92dd7912d7331b045a6ed` → `8f2e2d42e8372d18c0e08278773704b257a9a202` |
| Arquivos no PR | `sql/001_runtime_functions.sql`, `sql/001_runtime_functions.reconciliation.md` |
| Blob publicado do SQL | `421f1f3f0309afcbf37cf782b86cb85555fac5e3` |

Tabela de hashes do artefato SQL (prova de que o candidato revisado é o que foi publicado):

| Versão | sha256 | bytes |
|---|---|---|
| `main` antes do PR #3 | `2d9f2c368c52c8c21d0b805791b3450cdc9e183587b9de1820b0f86ce6992fd5` | 8022 |
| candidato revisado (card `t_530fbad8`) | `170cf979026c9dcaa0830a03cd4a3225030b7461085110f8253b28b74ccb4a63` | 9040 |
| publicado no PR #3 / esta cópia LoLSaas | `bf752895693990812933e026a260fde78db3f56d956b0d1777162087ab365537` | 10283 |

O SQL publicado é **idêntico em tokens** ao candidato revisado (`diff` após remover comentários/espaços = 0, conferido por `tr -d '[:space:]'` em ambos). A diferença são comentários que existiam em `main` e haviam sido suprimidos no candidato: foram preservados na publicação para não regredir documentação do pacote (advisory lock, escopo único de exceção, remoção A3, hardening da sequence, runbook de run `blocked`). Nenhum teste existente foi removido: `tests/test_runtime.py` não foi tocado.

Verificações executadas na publicação (clone limpo de `origin/main` em `mktemp -d`, sem tocar a árvore de trabalho local):

| Verificação | Comando | Resultado |
|---|---|---|
| Suite do pacote | `PYTHONPATH=src python3 -m unittest discover -s tests -v` | 23/23 OK (0.018s) — a contagem de 19 no topo deste arquivo era do momento da implementação; a base publicada tem 23 testes |
| Compilação | `PYTHONPATH=src python3 -m compileall -q src tests` | OK |
| Diff check | `git diff --check` | OK |
| SQL smoke | `postgres:16-alpine` na porta 55433: reset + migration aplicada **2x** + smoke + grants | aplicação reaplicável; claim/conflito/finish/recovery/late-finish/legado nulo conforme esperado; `anon=false`, `authenticated=false`, `service_role=true` |
| Releitura remota | `gh api .../contents/sql/001_runtime_functions.sql?ref=ddaf744` + `sha256sum` | `bf752895…` igual ao candidato local |
| Pós-merge | `compare/ddaf744...main` = `ahead`; merge commit com 2 parents; conteúdo em `main` = `bf752895…` | confirmado |

Limitações e itens fora de escopo:

- O repositório `hermes_agent_runtime` não possui workflows de CI (`/.github` ausente), então não há check remoto; as evidências acima são execução local real.
- **Nenhuma migration foi aplicada no Supabase.** O artefato aplicado segue `2cdb809da5e4e27f8bf0bf4be18090625c2f1886c3fa2f35e744e110fa5bc9c5` (run 117, LOL-59). A revisão de payloads agora presente em `migration/001_runtime_functions.proposed.sql` está **pendente de aplicação** e exige autorização de `devops`.
- Sem deploy Railway, tag, release, secret, force-push ou exclusão de branch.
