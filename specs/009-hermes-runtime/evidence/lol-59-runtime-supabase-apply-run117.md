# LOL-59 — aplicação e validação remota da migration do runtime (run 117)

- Task Kanban: `t_cd2022e2` (LOL-59, dono `devops`), run **117**
- Workspace: `/home/alexandre/LoLSaas/.worktrees/t_cd2022e2`, branch `chore/LOL-59-runtime-supabase`, base `e0d78c6` (run 115)
- Data: 2026-09-24, ~20:50–21:00 CEST
- Ambiente: projeto Supabase `tsfdsjmostggtxckmirr` (LoLCoach), PostgreSQL **17.6**, PostgREST **14.5**, API `https://tsfdsjmostggtxckmirr.supabase.co`
- Autorização: comentário do orquestrador no card `t_cd2022e2` (`1790275723`) — "AUTORIZAÇÃO EXPLÍCITA DO USUÁRIO: … autorizado aplicar a migration controlada 001_runtime_functions.sql no projeto tsfdsjmostggtxckmirr … Não fazer deploy Railway, merge, tag ou push."
- Artefato aplicado: `specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql`
  - `sha256 = 2cdb809da5e4e27f8bf0bf4be18090625c2f1886c3fa2f35e744e110fa5bc9c5`, 9502 bytes, único `begin; … commit;`
  - base `684ba8a9…` (revisão de working tree do pacote `hermes-agent-runtime`) + ajustes **A1–A4**
- Side effects: **migration aplicada** (4 funções + revoke de sequence); nenhum deploy, nenhum push/PR/tag/merge; linhas sintéticas de smoke criadas e **removidas** (estado final 0/0/0); nenhum valor de credencial impresso, versionado ou anexado.

## 1. Pré-condições verificadas (`repro/evidence/90_precheck_remote.out`)

| Verificação | Resultado |
|---|---|
| `SUPABASE_URL` do perfil | `https://tsfdsjmostggtxckmirr.supabase.co` (esquema + host corretos; aceito por `SupabaseStore`) |
| `SUPABASE_SERVICE_ROLE_KEY` | presente (JWT de 3 partes), valor não impresso |
| Funções `hermes_*` antes | **0** |
| Linhas antes | `agent_runs=0`, `agent_execution_locks=0`, `agent_events=0` |
| Sequence `agent_events_id_seq` antes (A4) | `anon` e `authenticated` com `SELECT, UPDATE, USAGE` (baseline Supabase) |
| Transação explícita no endpoint | suportada (`begin; select 'tx-ok'; commit;` → `201`) |
| Pacote canônico | `sql/001_runtime_functions.sql` = `684ba8a9…`, inalterado desde a validação do run 115 (sem drift de hotspot) |

## 2. Aplicação (`91_apply_remote.out`)

```
POST /v1/projects/tsfdsjmostggtxckmirr/database/query   HTTP 201 in 1.04s
sha256=2cdb809da5e4e27f8bf0bf4be18090625c2f1886c3fa2f35e744e110fa5bc9c5
```

Pós-aplicação imediata (mesma sessão): 4 funções `returns boolean`, `secdef=false`, `proconfig=search_path=""`, owner `postgres`, ACL `postgres=X/postgres | service_role=X/postgres`; contagens `0/0/0` (a migration não escreve dados).

## 3. Verificação de catálogo pós-aplicação (`92_catalog_after_remote.out`)

| Check | Resultado |
|---|---|
| B1 funções | `hermes_claim_run`, `hermes_heartbeat_run`, `hermes_finish_run`, `hermes_recover_expired_lock`; todas `boolean`, `security invoker`, `search_path=''` |
| B2/B3 EXECUTE efetivo | apenas `postgres` (owner) e `service_role`; `has_function_privilege('anon'/'authenticated', …, 'EXECUTE')` = **false** |
| B4 ACL da sequence (A4) | `anon`/`authenticated` **removidos**; restam `postgres` e `service_role` (`USAGE, SELECT, UPDATE`) |
| B5 RLS/policies/grants | inalterados: RLS ligado, 0 policies, grantees `postgres,service_role` nas três tabelas |
| B6/B7 constraints e índices | inalterados (CHECKs, PK/UNIQUE e a partial unique `uq_agent_runs_active_issue`) |
| B8 linhas | `0/0/0` |

## 4. Reaplicabilidade — ajuste A1 (`93_reapply_remote.out`)

O mesmo arquivo foi aplicado **mais duas vezes** no projeto já migrado: `HTTP 201` (0.93 s e 1.19 s), sem `42P13`, mantendo retorno `boolean` e a ACL `service_role`-only, com `0/0/0` ao final. Sem o `drop function if exists` do A1 isso abortaria a transação (provado localmente em `21_stage_crossapply.out`).

## 5. Smoke REST contra o PostgREST real (`94_smoke_remote.out`, 24/24 PASS)

| # | Prova | Resultado bruto |
|---|---|---|
| S1 | `POST rpc/hermes_claim_run` (service_role) | `200 true` |
| S2 | run gravado + lock + eventos iniciais | `status=running`, lock com `expires_at`, eventos `run.created`,`lock.acquired`,`run.started` |
| S3 | `rpc/hermes_heartbeat_run` do dono | `200 true` |
| S4 | segundo claim na mesma issue | `200 false` (contrato booleano; **sem** 409 cru) |
| S5 | resíduo do claim rejeitado | **nenhuma linha** (nem `queued`, nem `failed`, nem órfão); lock segue com o detentor |
| S6 | auditoria do conflito | `lock.rejected` gravado no run detentor com `payload.attempted_run_id` |
| S7 | TTL fora da faixa (9) | `400 {"code":"P0001","message":"Invalid lock TTL"}`, nenhum run criado |
| S8 | `finish` com status não-terminal | `400 Invalid final status`, run intacto |
| S9 | `finish` do dono | `200 true`; campos `commit_sha`/`tests_status` persistidos, `finished_at` marcado, lock removido |
| S10 | `finish` repetido | `200 false` |
| S11 | `recover` sem lock | `200 false` |
| S12 | papel `anon` (apikey sem Authorization) | rpc → `401 {42501 permission denied for function}`; `GET agent_runs` e `agent_execution_locks` → `401 {42501}` |
| S13 | catálogo pós-smoke | `anon`/`authenticated` sem EXECUTE; `service_role` com EXECUTE |

## 6. TTL real, recovery e conflito no lock (`96_recovery_lock_conflict_remote.out`, 15/15 PASS)

- **P1 (TTL/recovery real):** claim com `ttl=10 s` → `200 true`; após 11.5 s, heartbeat do dono → `200 false` (lock vencido); `recover` → `200 true`; run vira `failed`/`LockExpired` com `finished_at`; eventos `lock.expired` + `lock.recovered`; lock removido; `finish` tardio → `200 false`; `recover` repetido → `200 false`.
- **P2 (conflito no insert do lock, lock órfão com run não ativo):** seed de run `completed` + lock vigente apontando para ele; claim concorrente → `200 false`; **nenhum run residual**; lock continua com o detentor; `lock.rejected` contra o detentor com `attempted_run_id`; nenhum run `queued` no projeto.

## 7. Cliente canônico contra o projeto real (`97_client_canonical_remote.out`, 17/17 PASS)

Cliente de produção `hermes-agent-runtime/src` (`runtime.py d2a8f06d…`, `supabase.py 012799e3…`) — **não** o snapshot de `repro/`, que relaxa a checagem de URL para `http` a fim de permitir o gateway local.

| # | Prova | Resultado |
|---|---|---|
| C1 | `AgentRuntime.execute` completo por HTTP real | run `completed`, campos do dispatcher persistidos, eventos `run.created → lock.acquired → run.started → agent.dispatched → client.probe → run.completed → lock.released`, lock liberado |
| C2 | segundo `claim` do mesmo issue pelo cliente | `RunConflict`, sem run residual, `lock.rejected` no detentor |
| C3 | `recover` em issue sem lock | `False` (`store` e `runtime`) |
| C4 | `ExecutionBlocked` no dispatch | run gravado `blocked`/`ExecutionBlocked`; novo claim → `RunConflict` (issue presa, comportamento documentado); **runbook aplicado** → `canceled`/`ReleasedByOperator` |
| C5 | claim após liberação | sucesso (`running`) → `finish canceled`; nenhum lock residual nas issues do teste |

## 8. Delta do projeto (prova de não regressão)

`98_catalog_final.out` (dump completo pós-aplicação) comparado com `10_catalogo_live.out` (pré-aplicação) em `98_catalog_diff.out`:
**22 seções idênticas** — `tables, columns, constraints, indexes, rls_policies, table_privileges, triggers, row_counts, roles, seq_ownership, identity_columns, default_privileges, realtime_publication, version, exposed_schemas, postgrest_settings, public_tables_without_grantee_anon, helper_functions, check_regex_sample, table_owner_and_reloptions, …`;
**2 mudaram, exatamente as pretendidas** — `functions_hermes` (0 → 4) e `sequence_privileges` (remoção de `anon`/`authenticated`).
As tabelas do produto (`players`, `__EFMigrationsHistory`, …) e seus ACLs permanecem intocados.

## 9. Escrita de dados e limpeza

Todas as linhas do smoke são sintéticas (`LOL-59-SMOKE*`, ULIDs válidos no formato do CHECK real) e foram removidas por `repro/remote/750_cleanup.py` (delete transacional por `linear_issue_id like 'LOL-59-SMOKE%'`). Estado final verificado em `99_cleanup_final.out`: `runs=0, locks=0, events=0, hermes_fns=4` → `CELANUP_OK=True`. Nenhuma linha pré-existente foi tocada (o projeto tinha 0 linhas antes).

## 10. Achados

### 10.1 A3 é defensivo e inalcançável nesta forma (observado no projeto real)

O `update … status='failed' … where … status='queued'` do A3 **nunca encontra linha**: o `exception when unique_violation` fecha um sub-bloco do plpgsql, e o rollback desse sub-bloco desfaz o insert do run tentado antes do handler rodar. Evidência: S5 (nenhuma linha do run rejeitado) e P2.3/P2.5 (idem no caminho do insert do lock). O contrato desejado continua garantido — e de forma mais forte: **zero resíduo** de claim rejeitado, em vez de "resíduo marcado como failed". Ação desta task: nenhuma (os bytes aplicados estão congelados por hash e são o artefato sob revisão); recomendação ao dono da migration: manter como defesa em profundidade ou remover a cláusula numa próxima revisão — decisão de revisão, não bloqueio.

### 10.2 Runbook de `blocked` comprovado no projeto real

O caminho "run preso em `blocked`" (F2 da validação do run 115) foi exercitado de ponta a ponta: issue presa → `RunConflict` no novo claim → `update … status='canceled', error='ReleasedByOperator'` → novo claim liberado. Continua **decisão pendente do orquestrador**: manter o passo manual no runbook ou criar RPC dedicada (`hermes_release_run`). Nada no runtime cobre esse caminho.

### 10.3 Correção de `SUPABASE_URL` (F1) validada ponta a ponta

O valor corrigido `https://tsfdsjmostggtxckmirr.supabase.co` é aceito pelo `SupabaseStore` (que rejeita o host sem esquema) e serve o PostgREST do projeto (`/rest/v1`) — provado por todos os checks C1–C5 com o cliente canônico.

### 10.4 Errata do run 115 — hash do snapshot de `supabase.py`

A validação anterior listou `supabase.py 012799e3…` como snapshot congelado. O arquivo em `repro/runtime_pkg/hermes_agent_runtime/supabase.py` é, na verdade, `c951214a9b1c1faa4456800b1974df1006776500a7f69f42d146ac4f5f57700b` — o snapshot canônico (`012799e3…`) com a checagem de URL relaxada para `http` a fim de permitir o gateway local do repro. **Nada muda nos resultados locais** (a mudança só amplia a URL aceita); a distinção é relevante para rastreabilidade, e por isso o teste remoto usou o cliente **canônico**, não o snapshot.

## 11. Riscos e limitações

- As 4 RPCs estão **vivas** no projeto `tsfdsjmostggtxckmirr`, expostas pelo PostgREST em `public` e executáveis apenas por `service_role`; `anon`/`authenticated` recebem `401 {42501}` (função e tabelas). O ganho de superfície é restrito a isso — não há DDL de dados nem novos grants de tabela.
- Nenhuma aplicação consome as RPCs ainda (runtime não implantado; nenhum deploy Railway neste run). O tráfego real depende do card de deploy.
- Concorrência multi-instância não foi medida no projeto real (só o comportamento serializado por `pg_advisory_xact_lock` no repro local). Claims reais concorrentes ficam para o QA/deploy.
- `blocked` mantém a issue presa até liberação humana (§10.2) — bloqueio funcional conhecido, não regressão.
- Segredos: nenhum valor de credencial foi impresso, versionado ou anexado; os scripts leem o `.env` do perfil (modo 600) dentro do processo.

## 12. Rollback (preparado, não executado)

- Arquivo: `specs/009-hermes-runtime/migration/rollback_001_runtime_functions.sql` (transacional: `drop function if exists` das 4 RPCs + restauração do grant de sequence do baseline).
- Executor: `repro/remote/900_rollback.py` — **dry-run por padrão**; só aplica com `--confirm-rollback`. Dry-run executado em `99b_rollback_dryrun.out` (nada alterado; 4 funções presentes, `0/0/0`).
- Critério de acionamento: falha do smoke pós-aplicação (não ocorreu) ou decisão explícita de reverter.

## 13. Evidência bruta

`migration/repro/evidence/`: `90_precheck_remote.out`, `91_apply_remote.out`, `92_catalog_after_remote.out`, `93_reapply_remote.out`, `94_smoke_remote.out`, `95_cleanup_pass1.out`, `96_recovery_lock_conflict_remote.out`, `97_client_canonical_remote.out`, `98_catalog_final.out`, `98_catalog_diff.out`, `99_cleanup_final.out`, `99b_rollback_dryrun.out`, `99c_final_state.out`.
Scripts: `migration/repro/remote/700_precheck.py`, `710_apply.py`, `720_catalog_after.py`, `730_reapply_idempotency.py`, `740_smoke_rest.py`, `745_recovery_probe.py`, `750_cleanup.py`, `760_client_against_real_project.py`, `770_compare_catalog.py`, `799_final_state.py`, `900_rollback.py`.

**Releitura final independente** (`99c_final_state.out`), após toda a sequência: hash do artefato em disco = hash aplicado (`2cdb809d…`); `hermes_fns=4`, `anon_exec=0`, `seq_public_grants=0`, `runs/locks/events=0`; `service_role` → `200 false` em `hermes_recover_expired_lock` e `200 []` na leitura; `anon` → `401 {42501}`.

## 14. Próximo passo

Handoff para QA (`t_4c601578`): as RPCs `public.hermes_*` existem e respondem no projeto real neste estado; o contrato HTTP observado (`200 true/false`, `400 P0001` para TTL/status inválidos, `401 42501` para `anon`) é o esperado pelo `SupabaseStore` canônico. As tabelas operacionais voltaram a `0/0/0`, então o QA pode criar seu próprio namespace de issues sintéticas sem colisão.
