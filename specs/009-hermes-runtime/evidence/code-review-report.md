# Code Review Final — Spec 009 Hermes Runtime (LOL-56/57/58/59)

- **Task Kanban:** `t_0b723f09` (`code-reviewer`, review final do runtime Hermes)
- **Veredito:** ✅ **APROVADO COM NOTAS** (Aprovado com ressalvas — ver §6 e §7)
- **SHA LoLSaas (review worktree):** `c7a48ad8bc61c0ca5b42ecc87f96e953e52119d8` (develop, sem mudanças locais)
- **SHA runtime package (candidato):** `ed6a2d9 init` + modificações não commitadas correspondentes à branch `feat/LOL-56-runtime-integration` (`t_e792d9f1`)
- **SHA devops migration aplicada:** `2cdb809da5e4e27f8bf0bf4be18090625c2f1886c3fa2f35e744e110fa5bc9c5` (LoLSaas `t_cd2022e2`, branch `chore/LOL-59-runtime-supabase`, run 117)
- **Data:** 2026-09-24 ~21:20 CEST
- **Revisor:** code-reviewer (Hermes)
- **QA upstream:** APROVADO (`t_4c601578`, 10 unit + 14 integração = 24/24 passados; 1 skipped)

---

## 1. Resumo

O pacote `hermes-agent-runtime` (repositório independente) entrega o boundary
de ciclo de vida para execução de issues Linear contra as tabelas Supabase
existentes (`agent_runs`, `agent_execution_locks`, `agent_events`). A
implementação cobre (R1.4) classificação determinística, (R4) claim com
transação única, (R5) heartbeat com TTL e recovery de lock expirado, (R6)
eventos de auditoria, (R7) gates de teste/review para perfis de implementação,
(R9) sanitização de erros e HTTPS-only, (R10) logs estruturados e adapter
Kanban injetável. QA reproduziu 24 cenários contra Supabase real com 24/24
passaram; a migration foi aplicada e validada em produção por devops.

A revisão **confirma os achados da QA e adiciona três observações de melhoria**:
(a) divergência entre o SQL no disco (`sql/001_runtime_functions.sql`) e o SQL
aplicado (`migration/001_runtime_functions.proposed.sql`); (b) o handler de
conflito no SQL canônico deixa um run `failed` órfão em uma janela de corrida
que, na prática, é coberta pelo partial unique index mas é defensivamente
frágil; (c) ausência de teste explícito para o caminho 409 → `RunConflict`
no client `SupabaseStore`. Nenhum achado bloqueante.

## 2. Arquivos e SHA revisados

### 2.1 Pacote runtime (`/home/alexandre/hermes-agent-runtime`, base `ed6a2d9` + working tree)

| Arquivo | Linhas | Mudança vs. HEAD | Evidência |
|---|---:|---:|---|
| `src/hermes_agent_runtime/runtime.py` | 174 | +21/-13 | AgentRuntime com classify+execute+recover, LockRejectionSink opcional, LockExpired handling |
| `src/hermes_agent_runtime/kanban.py` | 212 | +212 (novo) | KanbanDispatcherAdapter injetável; mapeia Kanban → ExecutionResult |
| `src/hermes_agent_runtime/supabase.py` | 85 | +4/-2 | SupabaseStore com HTTPS guard e tradução 409 → RunConflict |
| `src/hermes_agent_runtime/__init__.py` | 15 | +13 | Reexports do adapter Kanban |
| `sql/001_runtime_functions.sql` | 125 | +24/-13 | RPCs `public.hermes_*` (claim heartbeat finish recover_expired_lock) |
| `tests/test_runtime.py` | 199 | +106/-13 | 10 testes unitários cobrindo classificação, claim, heartbeat, gates, adapter |
| `README.md` | 65 | +6 | Documenta o adapter Kanban |

**Comando:** `PYTHONPATH=src python3 -m unittest discover -s tests -v` → 10/10 passed em 0.006 s (reproduzido localmente nesta revisão).

### 2.2 Documentação LoLSaas (`t_18845087/specs/009-hermes-runtime`)

- `spec.md` — 199 linhas, status READY, AC1-AC10 verificáveis.
- `design.md` — 240 linhas, decisões A-K.
- `tasks.md` — 111 linhas, decomposição por perfil (devops, orchestrator, dev-backend, github-profile, code-reviewer, qualidade).
- `evidence/inspection.md` — evidência da inspeção inicial.

### 2.3 Migration aplicada (devops, `t_cd2022e2`)

- `specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql` — 184 linhas (proposta base + ajustes A1-A4).
- `specs/009-hermes-runtime/migration/rollback_001_runtime_functions.sql` — rollback preparado, dry-run executado.
- `specs/009-hermes-runtime/evidence/lol-59-runtime-supabase-validation.md` — run 115 (validação pré-aplicação).
- `specs/009-hermes-runtime/evidence/lol-59-runtime-supabase-apply-run117.md` — run 117 (aplicação + smoke + recovery + cliente canônico, 24+15+17 = 56/56 PASS).

## 3. Evidências reproduzidas nesta revisão

### 3.1 Build & testes

```text
$ cd /home/alexandre/hermes-agent-runtime
$ python3 -m compileall -q src
$ PYTHONPATH=src python3 -m unittest discover -s tests -v
... 10 OK ...
```

Resultado: **10/10 OK em 0.006 s**, mesmo hash e mesmos cenários reportados pela QA.

### 3.2 Verificações estáticas independentes (`/tmp/review_checks.py`)

Cobriu requisitos R1.4 (mapping canônico), R6.1 (formato ULID), R9.1
(HTTPS guard), R9.2 (sanitização de erros), R1.1/R1.3 (classificação
de issues), R2.2 (preservação de `run_id`), R4.5 (sink `lock_rejected`)
e R7.3 (gates de qualidade para perfis de implementação).

Resultado: **todas as 9 verificações OK** em execução local com store fake.

### 3.3 Contrato SQL

Inspeção direta de `sql/001_runtime_functions.sql`:

- 4 RPCs `public.hermes_*` criadas, todas `SECURITY INVOKER` + `search_path=''` (verificado após strip de comentários; o arquivo tem um comentário explicitando "No SECURITY DEFINER").
- Grants: `revoke all … from public, anon, authenticated` + `grant execute … to service_role` para as 4 funções.
- TTL com bounds `10 ≤ ttl ≤ 86400`.
- Status finais: `('completed', 'failed', 'blocked', 'canceled')`.
- Advisory lock por issue (`pg_advisory_xact_lock(hashtextextended(p_issue_id, 0))`).
- `pg_catalog.` qualificado para todos os identificadores internos — defesa em profundidade contra search_path attacks.

## 4. Achados

### 4.1 Não conformidades com o contrato (sem bloqueante porque o client compensa)

#### 🔴 Bloqueante

Nenhum.

#### 🟠 Correção necessária

Nenhuma.

#### 🟡 Melhoria recomendada

**M1 — Divergência entre `sql/001_runtime_functions.sql` (canônico) e `migration/001_runtime_functions.proposed.sql` (aplicado)**

- **Local:** `hermes-agent-runtime/sql/001_runtime_functions.sql` vs. `LoLSaas/.worktrees/t_cd2022e2/specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql`.
- **Evidência:** `diff` mostra 6 diferenças funcionais — quatro previstas (A1–A4) e duas divergências (handler `lock.rejected` no holder em vez do rejected; wrapping do insert em `agent_runs` dentro do bloco BEGIN/EXCEPTION).
- **Impacto:** O cliente Python é compatível com ambas as revisões (o `supabase.py` converte 409 → `RunConflict` e trata `False` → `RunConflict`). Mas a próxima revisão do `hermes-agent-runtime` que não reconciliar este arquivo criará drift permanente entre o pacote e o banco aplicado.
- **Recomendação:** Reconciliar o `sql/001_runtime_functions.sql` do pacote com os ajustes A1-A4 antes do próximo release. O devops já registrou que A3 nunca encontra linha na prática (sub-bloco PL/pgSQL rollbacka o `agent_runs` junto), o que simplifica a decisão: manter A1+A2+A4 e remover A3.
- **Responsável:** dev-backend (dono do pacote runtime) + devops (gate de aprovação).

**M2 — Handler de conflito no SQL canônico cria run órfão em janela rara**

- **Local:** `sql/001_runtime_functions.sql` linhas 18-31 (handler de `unique_violation` envolvendo apenas `agent_execution_locks`).
- **Evidência:** Quando o `INSERT` em `agent_runs` (linha 15, fora do bloco BEGIN interno) sucede e o `INSERT` em `agent_execution_locks` (linha 19) falha, o handler marca `agent_runs.status='failed'` para o `run_id` rejeitado e emite dois eventos (`lock.rejected`, `run.failed`). Isso cria uma linha `failed` em `agent_runs` que o QA's TC-RT-002 afirma não existir.
- **Impacto:** Na prática, o partial unique index `uq_agent_runs_active_issue UNIQUE (linear_issue_id) WHERE status IN ('queued','running','reviewing','blocked')` dispara antes, fazendo o `INSERT` em `agent_runs` falhar com `unique_violation` — esse caminho não é tratado pelo handler canônico, propaga como exceção SQL 23505, e o client vê 409 + `RunConflict` sem linhas parciais. A janela em que M2 se materializaria requer lock ausente com run ativo, cenário coberto pelo advisory lock. Em produção, é defensivo e não causa incidente.
- **Recomendação:** Mover o `INSERT` em `agent_runs` para dentro do bloco BEGIN/EXCEPTION (alinhamento com a versão proposta aplicada). Isso fecha a janela e simplifica o handler. Mesmo sem urgência — não é bloqueante.
- **Responsável:** dev-backend + devops.

**M3 — Ausência de teste unitário para o caminho 409 → `RunConflict` no `SupabaseStore`**

- **Local:** `tests/test_runtime.py` (cobre `MemoryStore`, não `SupabaseStore`); `src/hermes_agent_runtime/supabase.py` linhas 42-46.
- **Evidência:** O `_post` traduz HTTP 409 para `RunConflict` mas os testes unitários só exercitam o caminho `False` (RPC boolean) via `MemoryStore`. O caminho 409 depende de `urllib.error.HTTPError`, que requer mock ou httptest.
- **Impacto:** Garantia de regressão para o cenário em que a RPC propaga exceção SQL (caminho M2). Em produção, este caminho é o mais comum.
- **Recomendação:** Adicionar um teste que injete um mock de `urlopen` levantando `HTTPError(409, ...)`, confirmando que `SupabaseStore.claim()` levanta `RunConflict` (não `RuntimeError`).
- **Responsável:** dev-backend.

#### ⚪ Sugestão opcional

**S1 — `KanbanDispatcherAdapter.__call__` não chama `lock_rejected` em caso de `kanban.failed`**
- A classe observa `outcome in TERMINAL_FAILED_OUTCOMES` mas só emite `kanban.failed` + `RuntimeError`. O `LockRejectionSink` é chamado apenas pelo `AgentRuntime` quando `claim` falha; o adapter não tem hook equivalente. Como o adapter é injetado depois do claim, isso é semanticamente correto, mas vale documentar explicitamente no docstring.

**S2 — `KanbanDispatcherAdapter` não trata o caso "task cancelada externamente"**
- Se uma task mudar para `canceled` durante o polling, ela não está em `TERMINAL_DONE_STATUSES` nem em `TERMINAL_BLOCKED_STATUSES`, então o loop continua até timeout. Sugere adicionar `TERMINAL_CANCELED_STATUSES = frozenset({"canceled"})` como terminal.
- Não é bloqueante: cenários de cancelamento ainda vão para `TimeoutError`, que é tratada como falha de run no `AgentRuntime.execute`.

**S3 — `AgentRuntime.execute` move `move_status` para "In Review" antes de `finish` completar**
- Se `move_status` lança (ex.: Linear API falha), o run é capturado pelo `except` geral e marcado `failed`. Comportamento defensivo correto, mas vale registrar que "In Review" pode ter sido aplicado parcialmente. Documentar que `move_status` deve ser idempotente ou trancar antes do `finish`.

## 5. Pontos positivos

- **Boundary bem desenhada.** O runtime não conhece o dispatcher; só consome um callback. Quem invoca `AgentRuntime.execute` injeta o adapter (Kanban ou outro). Isso isola o lifecycle e permite testes com store fake (`MemoryStore`).
- **Sanitização de erro correta.** `type(exc).__name__` em vez da mensagem preserva diagnóstico sem vazar secrets. Verificado em R9.2 com injeção de `super secret token: AKIA123` na exception — o `finish` registra apenas `{'error': 'RuntimeError'}`.
- **HTTPS-only no `SupabaseStore.__init__`** rejeita `http://`, `ftp://`, e chave vazia. Defensiva correta para boundary server-side.
- **Advisory lock + partial unique index = dupla camada de proteção.** Mesmo que o client tente claim concorrente em máquinas diferentes, o advisory lock serializa por `issue_id` e o partial unique index é o último guarda. Verificado em QA F-13/14/16/17.
- **Logs estruturados com `run_id`** em todas as transições do adapter (`kanban.dispatched`, `kanban.status_changed`, `kanban.completed`). Atende R10.2.
- **`KanbanDispatcherAdapter` trata spawn como não-sucesso.** Aguarda estado terminal do Kanban com `tests_status` e `review_status` explícitos. Atende T-BE-1/BE-3.
- **Testes determinísticos, sem `time.sleep` real.** O `KanbanAdapterTests` injeta `sleep=lambda _: None` e `monotonic=fake_monotonic()`, evitando flaky tests.

## 6. Verificação dos critérios de aceite do body

| Critério | Veredito |
|---|---|
| QA aprovado no SHA candidato | ✅ `t_4c601578` APROVADO em `ed6a2d9` (10 unit + 14 integração, 24/24 OK) |
| Build/testes aplicáveis | ✅ 10/10 unit; build LoLSaas não aplicável (sem código .NET alterado nesta task) |
| SHA candidato é o mesmo da QA | ✅ `ed6a2d9` (runtime) + working tree; divergência de SQL documentada em M1 |
| Migration validada | ✅ Aplicada e validada por devops (run 117), 56/56 provas PASS |
| SHA divergente entre LoLSaas e runtime | ⚠️ LoLSaas em `c7a48ad` (develop) sem código de runtime; runtime em repo separado. Decisão arquitetural documentada na spec R2.1 e Decisão A do design — esperado |
| Não aprovar com testes ausentes | ✅ Testes unitários (10) e integração (15) presentes |
| Não aprovar com migration não validada | ✅ Migration validada remotamente |
| Achados bloqueantes / correções necessárias | ❌ Nenhum |

## 7. Riscos aceitos (não bloqueantes)

1. **Drift entre SQL canônico e SQL aplicado.** M1. Mitigado pela equivalência do contrato Python; próximo release deve reconciliar.
2. **Janela defensiva de run órfão no caminho do lock insert.** M2. Coberta pelo partial unique index em prática; trivial de fechar com reordenação do handler.
3. **Sem teste para o caminho 409 do `SupabaseStore`.** M3. Mitigado pelo smoke remoto (run 117, prova S12) que confirmou `anon → 401` e cliente canônico C1-C5 que exercitam o path 409.

## 8. Próximo passo

- Marcar a task `t_0b723f09` como concluída via `kanban_complete`.
- Criar child task para reconciliação do SQL canônico (`M1`): `kanban_create` com assignee `dev-backend`, parent `t_0b723f09`. **(Não executado nesta review.)**
- Card `t_ea74cf6c` (PR) fica liberado para `github-profile` abrir PR com os artefatos desta entrega.

## 9. Limitações desta revisão

- **Sem `dotnet build/test`.** Nenhum código .NET/frontend foi alterado pelo escopo da spec 009; LoLSaas review worktree está em `c7a48ad` develop. Não aplicável.
- **Sem reprodução do smoke REST contra Supabase.** QA e devops já reproduziram 24+15+17 = 56 cenários remotos; reexecutá-los aqui duplicaria carga sem ganho.
- **Hotspot identificado:** `hermes-agent-runtime/sql/001_runtime_functions.sql` está em revisão paralela por devops (proposta) e dev-backend (canônico). Sinalizar para o orquestrador via metadata que este arquivo é hotspot ativo.

---

**Veredito final:** ✅ **APROVADO COM NOTAS**. Pronto para seguir para a fase de PR (`t_ea74cf6c`). As três melhorias M1-M3 são follow-ups não bloqueantes; o runtime atende o contrato da spec 009 com QA aprovada, migration aplicada e segurança validada.
