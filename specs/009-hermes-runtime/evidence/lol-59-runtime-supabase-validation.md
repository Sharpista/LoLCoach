# LOL-59 — validação do schema Supabase, migration e grants do runtime

- Task Kanban: `t_cd2022e2` (LOL-59, dono `devops`), run `115`
- Workspace: `/home/alexandre/LoLSaas/.worktrees/t_cd2022e2`
- Branch: `chore/LOL-59-runtime-supabase`, base `c7a48ad` (merge da integração da spec 008)
- Data da execução: 2026-09-24, ~20:20–20:50 CEST
- Projeto Supabase: `tsfdsjmostggtxckmirr` ("LoLCoach", us-east-1, `ACTIVE_HEALTHY`, PostgreSQL **17.6**)
- Pacote validado: `/home/alexandre/hermes-agent-runtime` (`main@ed6a2d9`), SQL `sql/001_runtime_functions.sql`
- Side effects: **nenhuma migration aplicada**, nenhum deploy, nenhuma escrita no projeto Supabase, nenhum valor de credencial impresso

> **Atualização (run 117, 2026-09-24):** a migration foi **aplicada** no projeto `tsfdsjmostggtxckmirr` com autorização explícita do usuário e validada por REST/cliente real. Este documento passa a descrever a fase **pré-aplicação**; o estado atual, os resultados remotos e as correções estão em `lol-59-runtime-supabase-apply-run117.md` (§10.4 traz uma errata sobre o hash do snapshot de `supabase.py`).

## 1. Escopo e limites

Feito:

- leitura de catálogo do projeto real (tabelas `agent_runs`, `agent_execution_locks`, `agent_events`): colunas, constraints, índices, RLS, policies, grants de tabela/sequence, triggers, funções existentes;
- revisão do SQL do runtime contra o schema real;
- reprodução local fiel (PostgreSQL 17 + PostgREST 14.5 — mesma versão do projeto) com matriz de atomicidade, TTL, ownership, concorrência, grants e ciclo de vida ponta a ponta via HTTP/RPC;
- sonda remota **somente leitura** no projeto real (endpoint REST, leitura das três tabelas como `service_role`, negação para o papel `anon`, ausência das RPCs).

Não feito (e por quê):

| Item | Motivo |
|---|---|
| Aplicar `001_runtime_functions.sql` no projeto LoLCoach | O card determina "não aplicar migration em produção"; o projeto é o único ambiente vivo (hospeda `players`/`__EFMigrationsHistory`). Sem autorização explícita de aplicação neste run. |
| Provar remotamente o grant `service_role`-only das RPCs `hermes_*` | Depende da aplicação anterior (as funções não existem no projeto). |
| Deploy Railway | Fora do escopo do card. |
| `dotnet build/test` do LoLSaas | Não aplicável: nenhum código .NET/frontend foi alterado. |
| Suite `unittest` do pacote `hermes-agent-runtime` | Não reexecutada: a árvore do pacote está sendo editada em paralelo por outra task (ver §6.6). A validação de contrato foi feita com snapshot congelado do cliente contra PostgREST real. |

## 2. Artefato validado (revisões do SQL)

O arquivo `sql/001_runtime_functions.sql` do pacote **mudou durante a execução desta task** (mtime 20:30:45 CEST). Foram validadas as duas revisões, com hashes congelados em `migration/repro/`:

| Revisão | sha256 | Característica |
|---|---|---|
| `HEAD` `ed6a2d9` | `f160742f0b68cfaccb82c64d4666da1a1c18fbbfbc678795c8cdca9d32b1a3ac` | `hermes_claim_run ... returns void`; conflito só via 23505/HTTP 409 |
| working tree (20:31 CEST) | `684ba8a9b470fc3e9b6893d9796c589d9ae9773f9dfd43491c2983f89f0051ca` | `hermes_claim_run ... returns boolean`, insert `queued`→`running`, handler de `lock.rejected` |

Também congelados: `runtime.py` `d2a8f06d…`, `supabase.py` `012799e3…`, `kanban.py` `63fc2499…`, `test_runtime.py` `2686053c…` (snapshots em `migration/repro/runtime_pkg/`).

## 3. Schema real observado

Fonte: `migration/repro/introspect_api.py` / `extra_api.py` / `extra2_api.py` (Supabase Management API, apenas SELECT de catálogo). Saída bruta: `repro/evidence/10_catalogo_live.out`.

### 3.1 `agent_runs` (22 colunas)

`id uuid NOT NULL DEFAULT gen_random_uuid()` (PK), `run_id text NOT NULL` (UNIQUE), `linear_issue_id text NOT NULL`, `agent text NOT NULL`, `status text NOT NULL`, `risk text NOT NULL DEFAULT 'low'`, `execution_mode text NOT NULL DEFAULT 'auto'`, `environment text NOT NULL DEFAULT 'local'`, `started_at timestamptz NOT NULL DEFAULT now()`, `finished_at timestamptz`, `heartbeat_at timestamptz NOT NULL DEFAULT now()`, `workspace_path`, `branch`, `commit_sha`, `pull_request_url`, `railway_deployment_id`, `tests_status`, `review_status`, `error` (todos `text`), `metadata jsonb NOT NULL DEFAULT '{}'`, `created_at timestamptz NOT NULL DEFAULT now()`, `updated_at timestamptz NOT NULL DEFAULT now()`.

Constraints:

- `agent_runs_run_id_format CHECK (run_id ~ '^run_[0-9A-HJKMNP-TV-Z]{26}$')`
- `agent_runs_status_check CHECK (status = ANY (queued, running, reviewing, blocked, failed, completed, canceled))`
- `agent_runs_risk_check` (`low|medium|high|critical`), `agent_runs_execution_mode_check` (`auto|human|blocked`), `agent_runs_environment_check` (`local|preview|staging|production`)
- PK `(id)`, UNIQUE `(run_id)`

Índices: `idx_agent_runs_agent`, `idx_agent_runs_linear_issue`, `idx_agent_runs_started_at (started_at DESC)`, `idx_agent_runs_status` e, o mais relevante, a **partial unique index** `uq_agent_runs_active_issue UNIQUE (linear_issue_id) WHERE status IN ('queued','running','reviewing','blocked')`.

Trigger: `trg_agent_runs_updated_at BEFORE UPDATE → public.set_updated_at()` (função `SECURITY INVOKER`, `search_path=public`, existente em `public`).

### 3.2 `agent_execution_locks` (7 colunas)

`linear_issue_id text NOT NULL` (**PK**), `run_id text NOT NULL` (UNIQUE, FK `→ agent_runs(run_id) ON DELETE CASCADE`), `agent text NOT NULL`, `acquired_at timestamptz NOT NULL DEFAULT now()`, `heartbeat_at timestamptz NOT NULL DEFAULT now()`, `expires_at timestamptz NOT NULL` (sem default), `metadata jsonb NOT NULL DEFAULT '{}'`.

### 3.3 `agent_events` (7 colunas)

`id bigint NOT NULL GENERATED ALWAYS AS IDENTITY` (PK, sequence `agent_events_id_seq`), `run_id text NOT NULL` (FK `→ agent_runs(run_id) ON DELETE CASCADE`), `linear_issue_id`, `agent`, `event_type text NOT NULL` (**sem CHECK**: qualquer `event_type` é aceito), `payload jsonb NOT NULL DEFAULT '{}'`, `created_at timestamptz NOT NULL DEFAULT now()`. Índices `(run_id, created_at)` e `(linear_issue_id, created_at)`.

### 3.4 Segurança observada

- RLS **habilitado** nas três tabelas com **zero policies** e `relforcerowsecurity=false`.
- Grants de tabela: apenas `postgres` e `service_role` (todos os privilégios). `anon`/`authenticated` **não** têm privilégio de tabela.
- Sequence `agent_events_id_seq`: `anon` e `authenticated` **ainda possuem** `USAGE, SELECT, UPDATE` (baseline Supabase); sem grant de tabela correspondente, sem impacto prático — hardening proposto em A4.
- `service_role` tem `rolbypassrls=true`; `anon`/`authenticated` não.
- Funções pré-existentes em `public`: apenas `set_updated_at()` e `rls_auto_enable()`. **Zero funções `hermes_*`** (confirmado também remotamente: `PGRST202`, ver §5).
- Default privileges do schema `public` (owner `postgres`): `anon`/`authenticated` recebem EXECUTE por padrão em funções novas — por isso o `REVOKE ... FROM public, anon, authenticated` do migration é obrigatório (e foi provado eficaz, §4.3).
- Linhas hoje: `agent_runs=0`, `agent_execution_locks=0`, `agent_events=0` (confirmado localmente no catálogo e remotamente por REST).

## 4. Compatibilidade com o runtime (verificado)

| Item | Resultado |
|---|---|
| Formato de `run_id` do `new_run_id()` vs CHECK real | 200/200 ids gerados válidos e únicos; 3 negativos rejeitados (`23514`) — `repro/evidence/50_ulid_probe.out` |
| Colunas usadas por `claim/heartbeat/finish/recover` | Todas existem, com defaults compatíveis (`metadata`, `started_at`, `heartbeat_at`, `created_at`, `updated_at`) |
| `status` gravados (`queued`, `running`, `completed`, `failed`, `blocked`, `canceled`) | Todos aceitos pelo CHECK |
| `agent_events` sem CHECK de `event_type` | `agent.dispatched`, `run.created`, `lock.acquired`, `lock.rejected` etc. aceitos |
| `id` identity em `agent_events` (GENERATED ALWAYS) | Insert sem `id` funciona; RPC não informa `id` |
| Conflito de claim → HTTP | `23505`/`23514` → PostgREST **409** → `SupabaseStore` levanta `RunConflict` (E5 e §5) |
| RPC `void` (revisão HEAD) via PostgREST | Retorna corpo vazio; `_post` devolve `None` (aceito pelo `claim` da revisão HEAD) |
| RPC `boolean` (revisão working tree) | Retorna `true`/`false`; `claim` da working tree exige `result is True` |
| TTL | Guarda `10..86400` do SQL; limites 10 e 86400 aceitos, 9 e 86401 rejeitados sem criar linhas |
| Atomicidade | Falha de TTL/formato/conflito não deixa linha parcial (F13/F14/F16/F17) |
| Concorrência | `pg_advisory_xact_lock` serializa; segundo claim espera ~2,3 s e falha na unique index (C1); issues distintas em paralelo ok (C2) |

## 5. Prova remota (somente leitura) e prova local ponta a ponta

### 5.1 Remoto — projeto real (`repro/evidence/80_remote_readonly.out`)

| Prova | Resultado |
|---|---|
| `https://db.tsfdsjmostggtxckmirr.supabase.co/rest/v1/` (valor hoje em `SUPABASE_URL`) | `CERTIFICATE_VERIFY_FAILED: Hostname mismatch` — **não é** o endpoint REST |
| `https://tsfdsjmostggtxckmirr.supabase.co/rest/v1/` | HTTP 200, PostgREST **14.5** |
| `GET agent_runs` / `agent_execution_locks` / `agent_events` com `service_role` | HTTP 200, `[]` (0 linhas) |
| `GET agent_runs` com `apikey` e sem `Authorization` (papel `anon`) | HTTP **401**, código `42501`, "permission denied for table agent_runs" |
| `pg_proc` `hermes\_%` | 0 funções |
| `POST /rest/v1/rpc/hermes_claim_run` e `.../hermes_recover_expired_lock` | HTTP **404** `PGRST202` "no matches were found in the schema cache" — nada executado; a mensagem confirma os nomes de parâmetro esperados (`p_agent, p_environment, p_issue_id, p_mode, p_risk, p_run_id, p_ttl_seconds`) |
| Contagem após os probes | 0/0/0 (nenhum efeito colateral) |

### 5.2 Local — reprodução fiel (`repro/evidence/60_e2e_postgrest145.out`, `70_raw_http_postgrest145.out`)

Stack: `postgres:17-alpine` (17.11) + `postgrest/postgrest:v14.5` (mesma versão do projeto) + gateway local que remove o prefixo `/rest/v1` (equivalente ao Kong do Supabase) + cliente `SupabaseStore`/`AgentRuntime` congelado.

13/13 checks passaram, incluindo `AgentRuntime.execute` completo (`claim → heartbeat → evento → finish`) por HTTP; status brutos observados:

- `POST rpc/hermes_claim_run` (service_role) → `200 true`
- `POST rpc/hermes_recover_expired_lock` (service_role) → `200 false`
- `POST agent_events` (service_role) → `201`
- conflito de claim → `409` com `{"code":"23505"}` → `RunConflict`
- `POST rpc/hermes_*` (papel `anon`) → `401 {"code":"42501","message":"permission denied for function hermes_claim_run"}`
- `GET agent_runs` (papel `anon`) → `401 {"code":"42501"}`

### 5.3 Matriz SQL local (`repro/evidence/30_matrix_working_tree.out`, 24 casos)

Passaram: claim com sucesso e eventos iniciais; heartbeat do dono renova; heartbeat com `run_id` errado e com lock expirado → `false`; `finish` com status inválido → exceção; `finish` persiste `commit_sha`/`pull_request_url`/`tests_status`/`review_status`, marca `finished_at`, grava `run.completed`+`lock.released`, remove o lock; `finish` repetido → `false`; `recover` sem lock / lock vigente → `false`; `recover` com lock expirado → `true`, run `failed`/`LockExpired`, eventos `lock.expired`+`lock.recovered`, lock removido; `finish` tardio pós-recovery → `false`; novo claim após `failed` → ok; TTL nos limites; `run_id` inválido → `23514` sem linhas; conflito com run ativo → `23505` sem run órfão; `anon`/`authenticated` → `42501` na função e na tabela; `event_type` livre; trigger de `updated_at` dispara.

## 6. Achados e ajustes propostos

### 6.1 A1 — migration não é segura a mudança de assinatura (confirmado)

Aplicar a revisão working tree sobre uma base já aplicada no formato HEAD falha com `42P13 cannot change return type of existing function` e **aborta a transação inteira** (rollback limpo, sem estado parcial) — `repro/evidence/21_stage_crossapply.out`. Ajuste: `DROP FUNCTION IF EXISTS ...` antes dos `CREATE OR REPLACE` (torna reaplicável e permite mudar tipo de retorno). Validado em `35_stage_proposed.out` (duas aplicações seguidas com sucesso).

### 6.2 A2/A3 — `lock.rejected` inalcançável no conflito real (confirmado)

Na revisão working tree, o conflito real (issue já com run ativo) falha **antes** do handler: o primeiro insert (`agent_runs` com `status='queued'`) viola `uq_agent_runs_active_issue` → `23505` direto. Resultado: nenhum evento `lock.rejected` é gravado (`30_matrix_working_tree.out`, F17/F18), embora QA espere esse evento. O handler original só é exercitado no caso raro de lock órfão (F19). Ajuste proposto: envolver **run + lock** no mesmo bloco de exceção, marcar o run recém-criado como `failed` (nunca deixá-lo `queued`) e gravar `lock.rejected` **contra o detentor** do lock/issue, com `attempted_run_id` no payload. Validado em `35_stage_proposed.out` (P2/P5): retorna `false`, grava `lock.rejected` no run detentor, zero runs `queued` residuais, zero runs órfãos.

### 6.3 A4 — hardening da sequence

`anon`/`authenticated` ainda têm `USAGE, SELECT, UPDATE` em `agent_events_id_seq`. Sem grant de tabela não há exploração prática, mas o privilégio é desnecessário: proposto `REVOKE ALL ON SEQUENCE ... FROM anon, authenticated`.

### 6.4 F1 — `SUPABASE_URL` configurado aponta para o host errado (bloqueante de runtime)

O valor presente no ambiente do perfil (`.../profiles/devops/.env`, modo 600) é `db.tsfdsjmostggtxckmirr.supabase.co`, **sem esquema**. Dois problemas: (a) `SupabaseStore.__init__` exige prefixo `https://` e levanta `ValueError` — o runtime não sobe; (b) mesmo com `https://`, `db.<ref>.supabase.co` é o host Postgres direto e **não** serve PostgREST (certificado TLS com hostname mismatch comprovado). Valor correto para o cliente REST: `https://tsfdsjmostggtxckmirr.supabase.co`.

### 6.5 F2 — `blocked` bloqueia a issue até liberação humana (por desenho), sem caminho no runtime

`blocked` permanece na partial unique index, portanto nenhum claim novo entra para a issue (F18 confirmado) e o recovery só trata lock expirado. É coerente com "exige humano", mas falta o passo de liberação. Runbook documentado (aplicado em P4 e comprovado):

```sql
-- service_role / dashboard, após decisão humana
update public.agent_runs
   set status = 'canceled', finished_at = now(), error = 'ReleasedByOperator'
 where linear_issue_id = '<ISSUE>' and status = 'blocked';
```

Decisão pendente do orquestrador: manter o passo manual no runbook ou criar RPC dedicada (`hermes_release_run`) — regra de ciclo de vida, não alterada por esta task.

### 6.6 F3 — hotspot de arquivo / propriedade da migration

`/home/alexandre/hermes-agent-runtime` tem **cinco arquivos modificados e `kanban.py` não rastreado** (árvore suja) e o `sql/001_runtime_functions.sql` mudou no meio desta validação. A spec 009 atribui a migration a `devops` (este card), mas outra task (`t_e792d9f1`, integração do runtime) está editando os mesmos arquivos. Recomendação: congelar `sql/001_runtime_functions.sql` (owner devops), reconciliar a proposta A1–A4 e só então aplicar.

### 6.7 F4 — aplicação remota não autorizada neste run (bloqueio)

Sem autorização explícita para aplicar migration no projeto LoLCoach, a prova remota de `service_role`-only nas RPCs e o teste real de concorrência ficam pendentes. Condição de retomada em §8.

## 7. Riscos e rollback

- **Superfície do migration**: cria/recria 4 funções em `public` e revoga privilégio de sequence. Não cria/altera tabela, coluna, índice, constraint ou dado.
- **Não destrutivo e transacional**: o arquivo é um único `BEGIN … COMMIT`; falha em qualquer ponto faz rollback completo (observado em §6.1).
- **Rollback**: `drop function if exists public.hermes_claim_run(text,text,text,text,text,text,integer);` (idem heartbeat/finish/recover) e `grant usage, select, update on sequence public.agent_events_id_seq to anon, authenticated;` se A4 for aplicado.
- **Janela**: `DROP`+`CREATE` dentro da mesma transação não expõe estado intermediário aos chamadores.
- **Exposição via PostgREST**: funções novas em `public` aparecem no schema cache do PostgREST; o `REVOKE` das roles públicas é o que garante o acesso apenas por `service_role` (provado localmente em PostgREST 14.5 e remotamente hoje: `anon` recebe `42501`).
- **Exposição de segredo**: nada além de nomes de variáveis e hosts foi registrado; nenhuma credencial foi impressa, versionada ou anexada.
- **Bloqueio funcional conhecido**: run `blocked` exige liberação humana (§6.5).

## 8. Bloqueios, donos e condição de retomada

| # | Bloqueio | Dono | Condição de retomada |
|---|---|---|---|
| B1 | Aplicar `001_runtime_functions.sql` no projeto `tsfdsjmostggtxckmirr` (único ambiente vivo) — o card proíbe aplicação em produção | `orquestrador` / humano | autorização explícita de aplicação (com confirmação de que o projeto é o alvo), ou decisão de validar apenas em cópia |
| B2 | Prova remota de `service_role`-only e concorrência real nas RPCs | `devops` | depende de B1 |
| B3 | `SUPABASE_URL` incorreto no ambiente do perfil | `devops` (correção de configuração) | trocar para `https://tsfdsjmostggtxckmirr.supabase.co` |
| B4 | `lock.rejected` e `blocked` (§6.2/§6.5) | `dev-backend` (caller) + `devops` (migration) | decisão do orquestrador sobre adotar A2/A3 e o runbook |

## 9. Reprodução

Passo a passo e pré-requisitos em `specs/009-hermes-runtime/migration/repro/README.md`. Ajuste validado e pronto para revisão: `specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql` (base `684ba8a9` + A1–A4).
