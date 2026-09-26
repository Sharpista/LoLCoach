# LOL-61 — aplicação e validação da migration de payloads no Supabase (devops)

Card Kanban: `t_afa9faf1` (assignee `devops`). Issue: LOL-61 (Linear, `In Review`).
Projeto Supabase: `tsfdsjmostggtxckmirr` (LoLCoach), PostgreSQL 17.6, API REST
`https://tsfdsjmostggtxckmirr.supabase.co`. Execução: 2026-09-25, 20:06–20:22 UTC
(22:06–22:22 CEST).

## 1. Autorização

O card pedia aplicar "somente se houver autorização explícita para esta nova alteração de
RPCs; sem autorização, encerrar BLOCKED antes de qualquer escrita". A autorização foi
registrada em nota do orquestrador durante a execução:

> "AUTORIZACAO EXPLICITA DO ORQUESTRADOR: aplicar a migration de payloads LOL-61 no
> projeto Supabase tsfdsjmostggtxckmirr. Prosseguir com validacao de hash, apply
> transacional, reaplicabilidade, grants, smoke controlado e limpeza apenas de fixtures.
> Não alterar dados funcionais, secrets, deploy Railway, tag, release ou force-push."

Limites respeitados: nenhuma escrita de dado funcional, nenhum secret alterado, nenhum
deploy Railway/tag/release, nenhum force-push. Toda escrita ficou restrita a DDL de RPC +
grants da sequence e a fixtures sintéticas removidas ao final.

## 2. Artefatos e hashes (validados por leitura real dos dois repositórios)

| Papel | Origem | sha256 | Bytes |
|---|---|---|---|
| **Aplicado agora (artefato de registro)** | `origin/develop` do LoLCoach (`6f2643f`, PR #31) == `origin/main` do runtime (`8f2e2d4`, PR #3) | `bf752895693990812933e026a260fde78db3f56d956b0d1777162087ab365537` | 10283 |
| Aplicado antes (LOL-59, run 117) | `HEAD` do workspace (`2efcaf2`) | `2cdb809da5e4e27f8bf0bf4be18090625c2f1886c3fa2f35e744e110fa5bc9c5` | 9502 |
| Candidato aprovado no code review `t_530fbad8` | arquivo no working tree (`170cf979…`, também no working tree do runtime) | `170cf979026c9dcaa0830a03cd4a3225030b7461085110f8253b28b74ccb4a63` | 9040 |

Comparação semântica (comentários/whitespace removidos, `lol61_artifacts.py`):

- `tokens(published) == tokens(candidate)` → **true** (a diferença é só comentário).
- `tokens(published) == tokens(runtime origin/main)` → **true** (cópias byte-idênticas).
- `tokens(applied_anterior) != tokens(published)` → **false esperado** (mudança real de payloads).

Checagem de segurança do artefato antes do apply (`lol61_artifact_safety.py`): 21 statements
de nível superior = `begin`, 4 `drop function if exists`, 4 `create or replace function`,
4 `revoke all on function … from public, anon, authenticated`, 4 `grant execute … to service_role`,
`revoke all on sequence … from anon, authenticated`, `grant usage, select on sequence … to service_role`,
`commit`. Zero DML de dados, zero DDL de tabela, transação única (`begin` primeiro, `commit` último).

## 3. Estado pré-apply lido do projeto real (somente SELECT via Management API)

`lol61_supabase_read.py` / `lol61_supabase_read2.py` (`POST /v1/projects/<ref>/database/query`):

- 4 RPCs `public.hermes_*` `RETURNS boolean`, `prosecdef=false` (`security invoker`),
  `proconfig={"search_path=\"\""}`, ACL `postgres:EXECUTE,service_role:EXECUTE`.
- `anon`/`authenticated` sem `EXECUTE`; `PUBLIC` sem `EXECUTE`.
- `agent_events_id_seq`: ACL `{postgres=rwU/postgres,service_role=rwU/postgres}`; `anon`/`authenticated`
  sem `USAGE`/`SELECT`.
- Tabelas `agent_runs`/`agent_events`/`agent_execution_locks` com RLS habilitado, 0 policies,
  ACL apenas `postgres`/`service_role`; `payload jsonb NOT NULL DEFAULT '{}'::jsonb`.
- Dados: 12 eventos (0 nulos, 0 preenchidos = todos `{}`), 2 runs (`completed`, `canceled`),
  0 locks.
- `pg_get_functiondef` de cada RPC normalizado == corpo do artefato **anterior** (`2cdb809d…`):
  `identical=True` nas 4 funções, e nenhum marcador de payload (`jsonb_strip_nulls`,
  `finished_status`, `previous_run_id`) presente → a revisão LOL-61 **não estava aplicada**.

## 4. Validação local antes de tocar o projeto (fixture fiel)

Fixture reconstruída do catálogo real (roles `anon`/`authenticated` NOLOGIN, `service_role`
BYPASSRLS, default privileges do Supabase concedendo EXECUTE/rwU a `anon`/`authenticated`,
3 tabelas, CHECKs, `uq_agent_runs_active_issue`, índices, trigger `trg_agent_runs_updated_at`,
RLS sem policies) em `postgres:17-alpine` (17.11) — `fixture_schema.sql`.

| Verificação | Comando | Resultado |
|---|---|---|
| Reaplicabilidade (2ª aplicação do arquivo publicado) | `psql -f published.sql` 2x | **executado: passou** (2ª aplicação sem erro; `DROP FUNCTION IF EXISTS` preserva) |
| Contrato de payloads | `psql -f smoke_local.sql` | **executado: passou 8/8** (claim, conflito, heartbeat, recovery, payload por status completed/failed/blocked/canceled, entrada inválida sem write, payload não nulo/objeto + default `{}` legado, grants) |
| Contrato HTTP | `http_contract.py` contra `postgrest/postgrest:v14.5` | **executado: passou 8/8** — `service_role` 200/201; `anon` sem `Authorization` → **401 `42501`**; `authenticated` com JWT válido → **403 `42501`**; conflito → `200 false` |
| Compatibilidade com o cliente real | `client_compat.py` (`SupabaseStore` do pacote, proxy HTTPS local equivalente ao Kong em `/rest/v1`) | **executado: passou 13/13** — claim, heartbeat, conflito→`RunConflict`, `run` rejeitado não persistido, `lock.rejected` no detentor, finish completed, finish tardio→`RuntimeError`, lock liberado, ciclo de 8 eventos, payloads do SQL **aceitos por `validate_event_payload`** |
| Suíte do pacote | `PYTHONPATH=src python3 -m unittest discover -s tests` | **executado: passou 22/22 (`OK`)** |

Detalhe de contrato a registrar: com JWT válido de papel sem `EXECUTE`, o PostgREST responde
**403** (não 401); o **401** aparece quando ele não consegue resolver o papel (anon sem token).
Isso não é falha do SQL — é a semântica do PostgREST 14.5.

## 5. Apply real (transacional)

- Comando: `python3 real_apply.py` → `POST /v1/projects/tsfdsjmostggtxckmirr/database/query`
  com o conteúdo integral de `published.sql` (transação única no próprio arquivo).
- **Apply #1: HTTP 201 em 0,82 s. Apply #2: HTTP 201 em 0,87 s** (reaplicabilidade e
  idempotência provadas no projeto real; a 2ª aplicação manteve hashes idênticos).

Hash de definição (`md5(pg_get_functiondef(oid))`) lido do banco real:

| Função | Antes (`2cdb809d…`) | Depois do apply #1 | Depois do apply #2 |
|---|---|---|---|
| `hermes_claim_run` | `a779d058…` (2811 B) | `5d2f35c6…` (2939 B) | `5d2f35c6…` (2939 B) |
| `hermes_finish_run` | `715cebe6…` (1391 B) | `4ee870a8…` (3203 B) | `4ee870a8…` (3203 B) |
| `hermes_heartbeat_run` | `791a4fe3…` (815 B) | `791a4fe3…` (815 B) | `791a4fe3…` (815 B) |
| `hermes_recover_expired_lock` | `a490456f…` (1131 B) | `ee93efe0…` (1286 B) | `ee93efe0…` (1286 B) |

`hermes_heartbeat_run` não muda (não recebe payload nesta revisão) — coerente com o diff.
Marcadores presentes após o apply: `hermes_finish_run` com `jsonb_strip_nulls` +
`finished_status`; `hermes_recover_expired_lock` com `jsonb_strip_nulls` + `previous_run_id`.

## 6. Smoke controlado no projeto real (`real_smoke.py`) — 39/39 PASS

Fixtures com prefixo único `LOL61-APPLY-1790367457`, `run_id` gerado por `new_run_id()` do
pacote (formato `^run_[0-9A-HJKMNP-TV-Z]{26}$`), tudo pelo PostgREST real com a chave
server-side (`SUPABASE_SERVICE_ROLE_KEY`); nenhum valor de credencial foi impresso.

Payloads efetivamente gravados e lidos de volta:

| Evento | Payload observado no projeto |
|---|---|
| `run.created` | `{}` |
| `lock.acquired` | `{"expires_at": "2026-09-25T20:19:09.339488+00:00", "ttl_seconds": 90}` |
| `run.started` | `{"risk": "medium", "environment": "staging", "execution_mode": "auto"}` |
| `lock.rejected` | `{"reason": "RunConflict", "attempted_run_id": "run_01M3D3ETF9036VV18SVNDG485G"}` (no run detentor) |
| `run.completed` | `{"commit_sha": "ddd…", "tests_status": "passed", "review_status": "approved", "pull_request_url": "https://github.com/Sharpista/LoLCoach/pull/31", "railway_deployment_id": "smoke-lol61"}` |
| `lock.released` | `{"finished_status": "completed"}` |
| `run.failed` | `{"error": "RuntimeError", "error_code": "TimeoutError", "kanban_outcome": "blocked", "kanban_task_id": "t_smoke1"}` |
| `run.blocked` | `{"error": "ExecutionBlocked", "tests_status": "passed", "blocked_reason": "needs_input", "kanban_task_id": "t_smoke2"}` |
| `run.canceled` | `{"reason": "smoke", "canceled_by": "operator"}` |
| `lock.expired` | `{"expired_at": "2020-01-01T00:00:00+00:00", "last_heartbeat_at": "2026-09-25T20:17:48.957216+00:00"}` |
| `lock.recovered` | `{"previous_run_id": "run_01M3D3F2NEWNKWW5601GK9YR88"}` |

Cenários verificados (amostra das asserções):

- **A** claim `service_role` → `200 true`; os 3 payloads de claim conferem.
- **B** claim concorrente na mesma issue → `200 false`, run rejeitado **não** persistido,
  `lock.rejected` gravado no run **detentor** com `attempted_run_id`.
- **C** heartbeat → `true`; finish `completed` → `true`; `run.completed` só com campos da
  allowlist; `lock.released.finished_status=completed`; finish tardio → `false`; lock removido.
- **D/E/F** payload por status final `failed` (fallback `RuntimeError` + `error_code`),
  `blocked` (fallback `ExecutionBlocked` + `blocked_reason`) e `canceled` conferem.
- **G** lock forçado ao vencimento → `hermes_recover_expired_lock` → `true`; `lock.expired` com
  `last_heartbeat_at`; `lock.recovered.previous_run_id` correto; run marcado `failed`/`LockExpired`;
  finish tardio → `false`.
- **H** `p_fields` não-objeto → **HTTP 400 `P0001` "p_fields must be a JSON object"**, run
  permanece `running` e **nenhum** evento de status final foi gravado.
- **I** `rpc/hermes_claim_run` com `apikey` anon e sem `Authorization` → **401 `42501`**
  (fingerprint da anon key: `7650ca244458fb96`; valor nunca impresso).
- **J** todos os 29 eventos de fixture lidos por REST têm payload objeto não nulo e passam em
  `validate_event_payload` (contrato Python).

## 7. Limpeza e releitura final independente

- Limpeza por `DELETE /rest/v1/agent_runs?linear_issue_id=like.LOL61-APPLY-1790367457*`
  (o `on delete cascade` remove eventos e locks): 6 runs removidos. Resíduo verificado:
  events=0, locks=0, runs=0.
- Contagens globais voltaram ao estado pré-apply: **12 eventos, 0 nulos, 2 runs, 0 locks**.
- `final_verify.py` (releitura independente após o smoke): as 4 funções vivas são
  **token-idênticas ao artefato publicado**; eventos por tipo/timestamps, runs, colunas,
  constraints, índices, grants de tabela, triggers e policies **inalterados** em relação ao
  snapshot pré-apply. Nenhum dado funcional foi tocado.

## 8. Rollback (preparado, não executado)

`migration/rollback_001_runtime_functions.sql` (drop das 4 RPCs + restauração do grant de
sequence para `anon`/`authenticated`) continua válido para esta revisão: remove as funções e
não toca tabelas/dados. Executar apenas com falha comprovada ou decisão explícita de
operador; depois, `POST /rest/v1/rpc/hermes_claim_run` deve responder `404 PGRST202`.
Não houve necessidade de rollback — smoke 39/39 e releitura final verdes.

## 9. Achados, riscos e pendências

1. **Hotspot de arquivo — `specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql`**:
   o working tree do LoLSaas (`170cf979…`, candidato comentário-limpo) **não é byte-idêntico**
   ao artefato publicado/aplicado (`bf752895…`); a diferença é somente comentário (tokens
   iguais). O `migration/README.md` do working tree também está em revisão anterior (diz que
   `001…proposed.sql` é o artefato aplicado do LOL-59). Duas revisões do mesmo arquivo
   circularam no mesmo working tree — reconciliar com `origin/develop` (`bf752895…`) antes de
   qualquer novo uso, para não aplicar bytes que não correspondem ao publicado.
2. O apply foi feito pelos **bytes publicados** (`bf752895…`), não pelo arquivo do working
   tree. Registro de estado aplicado: este documento (seção 2/5). Não alterei README nem o SQL
   do working tree por estarem com replay de outra revisão e fora do escopo do card.
3. `blocked` segue terminal e continua na partial unique index `uq_agent_runs_active_issue`:
   uma issue que terminar em `blocked` só volta a aceitar claim com intervenção de operador
   (`update public.agent_runs set status='canceled', …`). Não mudou nesta revisão.
4. Linear LOL-61 continua `In Review`: o card pedia encerrar no Kanban com evidência; a
   atualização de estado/comentário na issue Linear ficou a cargo do orquestrador.
5. Reprodução durável: scripts e saídas brutas desta validação ficaram em
   `migration/repro/lol61/` (fixture, smoke local, contrato HTTP, compat do cliente, apply,
   smoke real, releitura final, varredura de credenciais). Contêineres locais
   (`postgres:17-alpine` em 55434, `postgrest:v14.5` em 3300, proxy HTTPS em 8443) foram
   descartados ao final. Nenhum secret foi gravado: as credenciais são lidas do
   `~/.hermes/shared/.env` em tempo de execução e o JWT do PostgREST local exige
   `LOL61_FIXTURE_JWT_SECRET` no ambiente. `repro/lol61/secret_scan.py` contém os *padrões*
   de busca (por isso aparece no próprio grep de credencial), não valores.
