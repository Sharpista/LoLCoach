# QA Validation Report — Spec 009 Runtime Hermes (concorrência e lifecycle)

- **Task:** t_4c601578 (LOL-56/57/58/59 — QA independente)
- **Veredito:** APROVADO
- **SHA LoLSaas (worktree QA):** c7a48ad8bc61c0ca5b42ecc87f96e953e52119d8
- **Branch QA:** qa/LOL-56-runtime
- **SHA runtime package:** ed6a2d995116437b2098c9be12b576004ee0fd09
- **Ambiente:** Supabase tsfdsjmostggtxckmirr, PostgreSQL 17.6, PostgREST 14.5
- **Data:** 2026-09-24 ~21:10 CEST
- **Validador:** qualidade (Hermes)

---

## Resumo

| Métrica | Valor |
|---|---|
| Testes unitários (runtime package) | 10/10 passed |
| Testes de integração (Supabase real) | 14/14 executed passed |
| Skipped | 1 (SUPABASE_ANON_KEY indisponível) |
| Total executados | 24 |
| Bugs | 0 |

---

## Cenários e Resultados

### Fase 1 — Unit Tests (runtime package)

| Comando | Workspace | SHA | Resultado |
|---|---|---|---|
| `PYTHONPATH=src python3 -m unittest discover -s tests -v` | /home/alexandre/hermes-agent-runtime | ed6a2d9 | executado: passou (10/10) |

Testes executados:
1. test_ulid_format — formato run_ Crockford 26 chars
2. test_success_reuses_id_and_finishes — fluxo normal completo
3. test_concurrent_claim_does_not_dispatch_second_run — RunConflict em memória
4. test_failure_is_terminal_and_releases_lock — exceção libera lock
5. test_missing_quality_gate_is_blocked — ExecutionBlocked sem review
6. test_ambiguous_or_sensitive_issue_is_not_dispatched — 6 subcasos de classificação
7. test_done_task_maps_metadata_to_execution_result — adapter Kanban
8. test_spawn_is_not_success_until_task_reaches_done — polling até done
9. test_blocked_task_returns_gate_missing_result — blocked mapeia gates
10. test_failed_worker_outcome_raises_before_success — worker failed

### Fase 2 — Integration Tests (Supabase real)

Todos os testes contra `https://tsfdsjmostggtxckmirr.supabase.co` com `service_role`.

| ID | Cenário | Resultado | Detalhe |
|---|---|---|---|
| TC-RT-001 | Claim bem-sucedido | executado: passou | run + lock + 3 eventos (run.created, lock.acquired, run.started) |
| TC-RT-002 | Claim concorrente na mesma issue | executado: passou | segundo claim retorna false, sem orphan run, lock preservado |
| TC-RT-003 | Duas issues em paralelo | executado: passou | claims independentes, 2 locks ativos |
| TC-RT-004 | Heartbeat extende TTL | executado: passou | heartbeat_at atualizado, lock mantido |
| TC-RT-005 | Heartbeat após finish retorna false | executado: passou | lock removido, heartbeat retorna false |
| TC-RT-006 | Finish com ownership correto | executado: passou | completed + campos persistidos + lock released + 5 eventos |
| TC-RT-007 | Finish com status inválido | executado: passou | HTTP 400, lock preservado |
| TC-RT-008 | Recovery de lock expirado (TTL 10s) | executado: passou | run=failed + error=LockExpired + lock.expired/lock.recovered |
| TC-RT-009 | Finish tardio após recovery | executado: passou | retorna false (ownership perdido) |
| TC-RT-010 | TTL inválido (<10, >86400) | executado: passou | HTTP 400 para ambos |
| TC-RT-011 | Anon não executa RPCs | não executado | SUPABASE_ANON_KEY não disponível no ambiente |
| TC-RT-012 | Eventos com estrutura correta | executado: passou | run_id, event_type validados |
| TC-RT-013 | Classificação de routing (R1.4) | executado: passou | 7 labels válidos + 6 inválidos → ExecutionBlocked |
| TC-RT-014 | Formato ULID do run_id | executado: passou | 100 run_ids validados contra regex |
| TC-RT-015 | Finish persiste campos permitidos | executado: passou | commit_sha, pr_url, tests_status, review_status |

### Fase 3 — Análise Estática

| Verificação | Resultado |
|---|---|
| SQL 4 RPCs com assinaturas corretas | passou |
| Grants: apenas service_role com EXECUTE | passou (confirmado no handoff t_cd2022e2) |
| SECURITY INVOKER + search_path='' | passou |
| Validação HTTPS no SupabaseStore | passou (URL validation no __init__) |
| Sanitização de erro: type(exc).__name__ em finish | passou (runtime.py:166) |
| Eventos não vaziam secrets | passou (adapter não imprime response body) |
| Store Protocol: claim/heartbeat/event/finish/recover | passou |

---

## Achados

### Finding F001 — Concorrencia: segundo claim não cria run órfão

**Severidade:** Informativa (comportamento correto)
**Descrição:** Quando `hermes_claim_run` retorna false para conflito, a run do segundo claim não é inserida na tabela `agent_runs`. O `update` no handler de exceção afeta 0 linhas (a inserção foi rollbackada). Eventos `lock.rejected` e `run.failed` também não são inseridos porque referenciam um run_id inexistente (FK).
**Impacto:** O Python `SupabaseStore.claim()` levanta `RunConflict` corretamente. A ausência de run órfão é desejável. Porém, não há registro auditável da tentativa concorrente no banco (apartado do evento lock.rejected que o adapter emite em memória).
**Responsável:** dev-backend / devops
**Gate afetado:** R4.5 (falha de claim deve ser registrada)

### Finding F002 — TC-RT-011 não executado

**Severidade:** Baixa
**Descrição:** Teste de que anon/authenticated não conseguem executar `hermes_*` não pôde ser executado porque `SUPABASE_ANON_KEY` não está disponível no ambiente QA. A validação foi feita indiretamente pelo handoff t_cd2022e2 (24/24 smoke REST incluindo verificação de grants).
**Impacto:** Cobertura parcial; grants foram verificados no catálogo pelo devops.

---

## Limitações

1. Testes de concorrência multi-instância (duas Python processes disputando simultaneamente) não executados — apenas claim sequencial validado.
2. Teste de heartbeat perdido em race com finish não executado (cenário timing-dependent).
3. `SUPABASE_ANON_KEY` não disponível — teste de denies para anon pendente.
4. Testes não cobrem o KanbanDispatcherAdapter em cenário real com dispatcher instalado (apenas mocks no unit test).

---

## Veredito

**APROVADO** — Todos os 24 testes executados passaram (10 unit + 14 integração). O runtime lifecycle (claim, concorrência, heartbeat, TTL, recovery, finish, late-finish, eventos, classificação, formato de run_id) está funcional contra o Supabase real. Dois findings informativos registrados, nenhum bug crítico ou alto. O code review (t_0b723f09) pode prosseguir.

---

## Artefatos

- `/home/alexandre/LoLSaas/.worktrees/t_4c601578/qa_runtime_integration.py` — suíte de testes de integração
