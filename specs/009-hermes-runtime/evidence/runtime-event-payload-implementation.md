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
