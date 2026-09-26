# Design 009 - Runtime Hermes Linear/Supabase/GitHub/Railway

Complementa `spec.md` (contrato) e `tasks.md` (execução). Decisões aqui são vinculantes para implementação; mudanças que alterem ciclo de vida, segurança ou ownership devem voltar para `Open Questions` antes de implementar.

## Contexto verificado

| Componente | Estado observado | Consequência |
|---|---|---|
| LoLSaas repo | Spec-driven obrigatório por `AGENTS.md`, `specs/README.md` e `orchestrator/ORCHESTRATOR.md` | Esta entrega só cria contrato; runtime não é implementado aqui |
| Runtime package | `/home/alexandre/hermes-agent-runtime`, `main@ed6a2d9`, biblioteca Python 3.11 sem dependências de runtime | Deve ser instalado/adaptado no ambiente do orquestrador, não copiado para frontend/app |
| SQL package | `sql/001_runtime_functions.sql` cria quatro RPCs `public.hermes_*` e grants para `service_role` | Precisa de owner `devops` nesta entrega e autorização por tocar Supabase compartilhado |
| Dispatcher real | Hermes Agent v0.21.2, `hermes kanban dispatch`, workers via `systemd-run --user --scope` | O runtime deve envolver o dispatcher instalado, não substituí-lo |
| Supabase real | Tabelas existem; funções `public.hermes_*` ausentes | `SupabaseStore.claim()` falharia hoje até a migration ser aplicada |
| Linear | LOL-56/57/58 `Todo`, LOL-59 `Done`; labels operacionais definem roteamento | Poller deve filtrar por status/labels antes de claim |
| Deploy Railway | Spec 008 define deploy controlado e autorizado | Runtime registra deployment id quando existir; não executa deploy por padrão |
| Pipeline oficial LOL-63 | Linear -> Orchestrator Hermes -> contexto/memória -> Supabase -> seleção de especialista -> implementação -> qualidade -> code-reviewer -> GitHub -> Railway | A boundary runtime precisa ser composta com gates e handoffs externos, não tratada como pipeline inteiro |

## Decisão A - Boundary do runtime

**Escolhido:** usar `hermes-agent-runtime` como boundary server-side de lifecycle, mantendo a integração Linear/GitHub/Railway no orquestrador LoLSaas/Hermes.

```text
Linear issue + labels
        |
        v
Orquestrador Hermes (poll/filter/context/recovery)
        |
        | context bundle redigido + classify + execute(issue, dispatch)
        v
hermes-agent-runtime
        |
        | RPC service_role
        v
Supabase agent_runs / agent_execution_locks / agent_events
        |
        +--> seleção de especialista -> Kanban/workers dev-backend/dev-frontend/devops
        |        |
        |        v
        |     qualidade -> code-reviewer -> GitHub/CI autorizado
        |
        +--> Railway deploy apenas quando task autorizada
```

**Justificativa:** o pacote já encapsula claim, heartbeat, finish, recovery e sanitização básica; o dispatcher instalado já sabe criar workers e preservar workspaces. Manter o pacote separado reduz acoplamento e evita importar credenciais Supabase no app .NET/Angular.

**Alternativas descartadas:**

| Alternativa | Por que não |
|---|---|
| Reimplementar lifecycle diretamente no orquestrador | Duplica código já testado e aumenta risco de corrida em claim/recovery |
| Usar apenas Kanban SQLite local como fonte de verdade | Não atende integração Linear/Supabase nem auditoria centralizada |
| Chamar workers diretamente do poller Linear sem runtime | Perde lock transacional e recuperação de expirados |
| Colocar runtime no LoLCoach.Api | Mistura automação operacional com produto web e exigiria service role no runtime da aplicação |

## Decisão A.1 - Pipeline oficial e contratos de handoff

**Escolhido:** o runtime é a fronteira transacional de run/lock/eventos, enquanto o pipeline oficial é uma composição orquestrada de handoffs verificáveis entre sistemas e perfis. Nenhuma etapa posterior pode inferir sucesso apenas de spawn ou claim; cada gate produz evidência própria.

```text
Linear Todo + labels válidos
  -> Orchestrator valida spec/design/tasks/Open Questions/dependências
  -> Orchestrator monta contexto/memória redigidos
  -> Supabase claim/lock/eventos iniciais pelo runtime
  -> seleção de especialista por label/risco/ambiente
  -> worker implementa somente task atribuída
  -> qualidade executa/verifica critérios de aceite
  -> code-reviewer aprova ou pede rework
  -> GitHub publica PR/CI somente quando autorizado
  -> Railway faz deploy somente por task autorizada e gate de ambiente
```

| Handoff | Contrato de entrada | Contrato de saída | Gate que libera próxima etapa |
|---|---|---|---|
| Linear -> Orchestrator | Issue em `Todo`, labels conhecidas, link/escopo da spec | decisão `eligible`/`blocked`/`ignored` com motivo | política R1 aprovada |
| Orchestrator -> Contexto/memória | Spec/design/tasks, AGENTS, ORCHESTRATOR, memórias OpenViking pertinentes, histórico Kanban | bundle redigido e suficiente para o worker | R11 sem dados proibidos |
| Contexto/memória -> Supabase | `linear_issue_id`, `run_id`, `agent`, TTL e payloads permitidos | run/lock/eventos iniciais ou conflito | claim transacional aceito |
| Supabase -> Especialista | lock ativo, labels validadas, workspace/branch | task Kanban com assignee real | task materializada e `kanban_task_id` registrado |
| Especialista -> Qualidade | diff/SHA candidato, evidências de build/testes, limitações | relatório QA aprovado/reprovado/bloqueado | `tests_status` derivado de execução real |
| Qualidade -> Code reviewer | relatório QA, diff/SHA, spec/design/tasks | parecer aprovado/rework/bloqueado | `review_status=approved` para concluir implementação |
| Code reviewer -> GitHub | autorização de publicação, SHA verificável, branch limpa ou dirty state registrado | PR/checks ou bloqueio explícito | CI/checks requeridos passados quando aplicável |
| GitHub -> Railway | task de deploy, ambiente permitido, rollback/runbook | deployment id/status redigido | autorização humana para production; preview/staging conforme contrato |

Contexto e memória não entram integralmente em `agent_events.payload`; eventos persistem identificadores e status permitidos, enquanto evidências versionadas/Kanban guardam o contexto redigido necessário para auditoria.

## Decisão B - Estados e transições

```text
Linear Todo + labels elegíveis
  -> recover_expired_issue(issue_id)
  -> claim(run_id, lock ttl)
  -> Linear In Progress (opcional, se callback disponível)
  -> event agent.dispatched
  -> worker executa task Hermes
  -> gates de teste/review
  -> Linear In Review (para implementação) ou estado operacional definido
  -> finish(completed|failed|blocked|canceled)
  -> lock released
```

Status do runtime:

| Status | Uso |
|---|---|
| `running` | Run ativo após claim |
| `reviewing` | Reservado para etapas de revisão quando a tabela já usar esse estado |
| `completed` | Execução encerrou com gates exigidos satisfeitos |
| `failed` | Exceção, heartbeat perdido, lock expirado ou falha técnica |
| `blocked` | Política/entrada exige humano ou gate ausente |
| `canceled` | Cancelamento externo autorizado |

O runtime não marca Linear como `Done`. Para implementação, sucesso move para revisão/QA conforme o grafo de tarefas; fechamento final continua responsabilidade do fluxo spec-driven.

## Decisão C - Labels e roteamento

O poller só entrega ao runtime issues que passem pela política abaixo. A mesma política deve ser testada com unit tests antes de ligar o loop real.

| Condição | Ação |
|---|---|
| `status != Todo` | Ignorar ou registrar bloqueio, sem claim |
| Nenhum `agent:*` ou mais de um `agent:*` conhecido | Bloquear para orquestrador |
| `execution:human` ou `execution:blocked` | Bloquear/aguardar humano |
| `risk:critical` | Bloquear para aprovação humana |
| `env:production` | Bloquear para aprovação/deploy gate |
| `agent:*` desconhecido | Bloquear e registrar label inválido |
| Elegível | Chamar `AgentRuntime.execute` |

Mapping canônico:

| Label | Worker |
|---|---|
| `agent:orchestrator` | `orchestrator` |
| `agent:backend` | `dev-backend` |
| `agent:frontend` | `dev-frontend` |
| `agent:devops` | `devops` |
| `agent:qa` | `qualidade` |
| `agent:review` | `code-reviewer` |
| `agent:github` | `github-profile` |

## Decisão D - Supabase RPCs como fronteira de concorrência

As operações críticas devem passar pelas RPCs do SQL do pacote:

| RPC | Responsabilidade | Propriedade de segurança |
|---|---|---|
| `hermes_claim_run` | Inserir `agent_runs`, `agent_execution_locks` e eventos iniciais | Transação única + advisory lock por issue |
| `hermes_heartbeat_run` | Renovar lock e heartbeat do run | Só renova se run ainda possui lock não expirado |
| `hermes_finish_run` | Finalizar run, persistir campos permitidos e liberar lock | Retorna false se ownership foi perdido |
| `hermes_recover_expired_lock` | Marcar run expirado como failed e remover lock | Só atua se `expires_at <= now()` sob lock |

A aplicação usa service role porque precisa transacionar tabelas operacionais sem expor RPCs a usuários finais. As funções permanecem `security invoker` e grants são removidos de `public`, `anon` e `authenticated`.

## Decisão E - Migration ownership

**Owner:** `devops` (card LOL-59, atuando como responsável único pela validação/aplicação da migration nesta entrega).

**Reviewer:** `code-reviewer` ou `dev-backend`.

**Fluxo exigido:**

1. Confirmar autorização explícita para banco Supabase compartilhado.
2. Executar leitura de catálogo: tabelas, constraints e existência das funções.
3. Revisar `sql/001_runtime_functions.sql` contra a schema real.
4. Aplicar como migration SQL controlada; não via startup de app, não via deploy Railway.
5. Confirmar funções, assinaturas e grants por leitura de catálogo.
6. Registrar evidência sem secrets.

Essa task de documentação não aplica a migration. Como as funções estão ausentes hoje, qualquer task que tente usar `SupabaseStore.claim()` antes do item acima deve bloquear por dependência.

## Decisão F - Heartbeat, TTL e recovery loop

- TTL default: `600s`.
- Heartbeat default: `30s`, sempre menor que metade do TTL.
- Recovery: antes de cada tentativa de claim e em loop periódico do orquestrador para issues candidatas.
- Falha de heartbeat: run não pode completar como sucesso; precisa finalizar `failed`/`blocked` com erro sanitizado.
- Worker antigo após recovery: `finish` retorna falso e o orquestrador deve registrar revisão manual porque o resultado é tardio.

```text
for issue in candidate_issues:
    runtime.recover_expired_issue(issue.id)
    try:
        runtime.execute(issue, dispatch, move_status=linear_update_status)
    except RunConflict:
        log conflict and skip
    except ExecutionBlocked:
        record blocked reason for orchestrator
```

## Decisão G - Eventos e payloads

Eventos são append-only e devem permitir auditoria por `run_id` sem armazenar segredo. A coluna `payload` é JSONB e deve receber objeto JSON não nulo. O writer deve normalizar payload ausente para `{}` e o reader deve tratar `NULL` legado como `{}` até todas as migrations e dados antigos estarem reconciliados.

### G1 - Modelo canônico

```json
{
  "event_type": "kanban.completed",
  "payload": {
    "kanban_task_id": "t_12345678",
    "status": "done",
    "commit_sha": "0123456789abcdef0123456789abcdef01234567",
    "tests_status": "passed",
    "review_status": "approved"
  }
}
```

Colunas (`run_id`, `linear_issue_id`, `agent`, `created_at`) continuam fonte de verdade; repeti-las no payload só é permitido quando o adapter Kanban já emite esses campos para facilitar correlação. Payloads não podem conter dados de produto ou corpo de mensagens.

### G2 - Allowlist por evento

| Evento | Campos obrigatórios | Campos opcionais |
|---|---|---|
| `run.created` | nenhum (`{}`) | nenhum |
| `lock.acquired` | `ttl_seconds`, `expires_at` | nenhum |
| `run.started` | nenhum (`{}`) | `linear_status_to`, `risk`, `execution_mode`, `environment` |
| `agent.dispatched` | `kanban_task_id` quando já existir task | `dispatch_backend`, `status`, `assignee` |
| `kanban.dispatched` | `kanban_task_id`, `status` | `assignee`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.status_changed` | `kanban_task_id`, `status` | `assignee`, `kanban_outcome`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.completed` | `kanban_task_id`, `status` | `commit_sha`, `pull_request_url`, `railway_deployment_id`, `tests_status`, `review_status`, `assignee`, `kanban_outcome`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.blocked` | `kanban_task_id`, `status` | `tests_status`, `review_status`, `assignee`, `kanban_outcome`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.failed` | `kanban_task_id`, `status`, `kanban_outcome` | `assignee`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.timeout` | `kanban_task_id`, `status` | `assignee`, `run_id`, `linear_issue_id`, `agent` |
| `tests.completed` | `tests_status` | `command`, `exit_code`, `total`, `passed`, `failed`, `skipped`, `duration_ms`, `artifact_path` |
| `review.completed` | `review_status` | `reviewer`, `artifact_path`, `pull_request_url` |
| `deploy.requested` | `environment` | `requested_by`, `pull_request_url`, `commit_sha` |
| `deploy.completed` | `railway_deployment_id`, `environment`, `deploy_status` | `deployment_url`, `commit_sha`, `duration_ms` |
| `run.completed` | nenhum (`{}`) | `commit_sha`, `pull_request_url`, `railway_deployment_id`, `tests_status`, `review_status` |
| `run.failed` | `error` | `error_code`, `kanban_task_id`, `kanban_outcome` |
| `run.blocked` | `error` | `blocked_reason`, `kanban_task_id`, `tests_status`, `review_status` |
| `run.canceled` | nenhum (`{}`) | `canceled_by`, `reason` |
| `lock.released` | nenhum (`{}`) | `finished_status` |
| `lock.rejected` | `reason` | `attempted_run_id` |
| `lock.expired` | `expired_at` | `last_heartbeat_at`, `ttl_seconds` |
| `lock.recovered` | nenhum (`{}`) | `recovered_by`, `previous_run_id` |

### G3 - Enums e validação

- `tests_status`: `passed`, `failed`, `skipped`.
- `review_status`: `approved`, `changes_requested`, `blocked`.
- `deploy_status`: `succeeded`, `failed`, `canceled`.
- `environment`: `local`, `preview`, `staging`, `production`, mas `deploy.*` só aceita `preview`, `staging` ou `production` quando houver autorização própria.
- `commit_sha`: SHA-1 hexadecimal de 40 caracteres.
- Timestamps: ISO-8601 UTC.
- URLs: HTTPS e sem query string sensível.
- Campos numéricos: inteiros não negativos, exceto `exit_code` que pode refletir o processo.

Campos desconhecidos devem ser rejeitados em testes de contrato ou descartados antes de persistir. Campos obrigatórios ausentes em evento aplicável devem falhar a validação do adapter/RPC, exceto `agent.dispatched` antes de task materializada, quando `{}` é permitido e um evento `kanban.dispatched` posterior deve preencher `kanban_task_id`.

### G3.1 - Eventos acumulados, falhas eventful e ordem de persistência

`AgentRuntime` deve tratar os eventos produzidos pelo adapter como uma fila ordenada por run, não como metadado acessório do sucesso. O adapter Kanban pode observar eventos úteis antes do desfecho final; esses eventos precisam sobreviver mesmo quando o desfecho é `blocked`, `failed` ou timeout.

Sequência obrigatória no runtime:

```text
claim SQL grava run.created + lock.acquired + run.started
runtime grava agent.dispatched
dispatch/adapter acumula eventos kanban.*
runtime valida + deduplica + persiste eventos acumulados
runtime chama finish(status, fields) para gravar run.<status> + lock.released
```

Consequências:

- `finish` não pode ser chamado antes de persistir os eventos acumulados do adapter.
- `kanban.completed` deve ser persistido mesmo quando o gate de implementação falhar e o runtime finalizar `run.blocked` por `tests_status`/`review_status` ausente.
- `kanban.blocked` deve ser persistido antes de `run.blocked`.
- `kanban.failed` deve ser persistido antes de `run.failed` quando o worker terminar com outcome técnico (`failed`, `spawn_failed`, `rate_limited` etc.).
- `kanban.timeout` deve ser persistido antes de `run.failed` com `TimeoutError` quando o adapter atingir deadline sem estado terminal.
- Erro durante validação/persistência dos eventos acumulados impede `completed`; o run finaliza `failed` com erro sanitizado se ainda possuir o lock.

Para desfechos não bem-sucedidos, o adapter deve retornar ou lançar uma estrutura eventful explícita que preserve `events` junto do tipo sanitizado do erro. A implementação pode usar exceção própria (`ExecutionEventError`, `DispatchEventError`) ou envelope equivalente, desde que `AgentRuntime` consiga extrair a lista de eventos sem ler mensagens cruas. O campo final `error`/`error_code` deve ser código curto (`ExecutionBlocked`, `RuntimeError`, `TimeoutError`, `PayloadValidationError`, `RunConflict`, `LockExpired`) e nunca `str(exc)` quando a origem puder conter segredo.

Deduplicação deve acontecer depois da validação/normalização do payload e antes de chamar `store.event`. A chave canônica é:

```text
se payload.kanban_task_id existe:
  (event_type, payload.kanban_task_id, payload.status, payload.kanban_outcome)
caso contrário:
  (event_type, json_payload_normalizado_com_chaves_ordenadas)
```

A primeira ocorrência vence. O algoritmo não pode reordenar eventos distintos, não pode colapsar mudanças de status diferentes e não pode mascarar payload inválido; payload inválido deve falhar antes de qualquer persistência parcial do mesmo evento.

### G4 - Dados proibidos e redaction

Payload proibido: connection string, `SUPABASE_SERVICE_ROLE_KEY`, tokens Linear/GitHub/Railway, headers/cookies, stdout/stderr bruto, stack trace bruto, payloads HTTP externos não redigidos, `.env`, logs com secrets, nomes/e-mails de usuários finais, título/descrição/comentários completos de Linear, PII de jogadores/usuários e qualquer dado de produto que não esteja na allowlist.

Erros persistidos devem usar tipo/código (`ExecutionBlocked`, `RunConflict`, `LockExpired`, `TimeoutError`, `RuntimeError`) e motivo operacional curto; mensagem crua de exceção só pode ser usada depois de redaction explícito e teste que prove ausência de segredo.

### G5 - Compatibilidade RPC/migration

As RPCs continuam recebendo `p_fields jsonb` em `hermes_finish_run`; a implementação deve filtrar `p_fields` para campos de `agent_runs` e gravar o evento `run.<status>` com payload derivado apenas da allowlist do evento final. `hermes_claim_run` deve gravar `run.created` e `run.started` com `{}` e `lock.acquired` com `ttl_seconds`/`expires_at`. `lock.rejected`, `lock.expired` e `lock.recovered` permanecem obrigatórios porque são usados para auditoria de concorrência e recovery.

Para migração reaplicável, alterar `insert into public.agent_events (...)` sem payload para inserir `payload = '{}'::jsonb` ou `jsonb_build_object(...)`, sem mudar assinatura pública das RPCs além do que já foi reconciliado pela Spec 009. Readers e queries de observabilidade devem usar `coalesce(payload, '{}'::jsonb)` durante a janela de compatibilidade.

### G6 - Observabilidade de payloads

Consultas e dashboards devem expor `event_type`, `payload`, `created_at` e colunas de correlação, mas views públicas ou artifacts só podem incluir payload redigido. A validação QA deve amostrar eventos de claim, dispatch, conclusão, conflito e recovery para confirmar que payload é objeto JSON, não nulo, sem campos proibidos e com campos obrigatórios por evento.

## Decisão H - Gates de qualidade e review

Para perfis de implementação (`dev-backend`, `dev-frontend`, `devops`), `AgentRuntime.execute` só pode finalizar `completed` quando `ExecutionResult.tests_status == "passed"` e `ExecutionResult.review_status == "approved"`.

O callback de dispatch é responsável por traduzir o resultado do Kanban/CI/review para `ExecutionResult`. Quando o grafo Kanban já tiver filhos de QA/review precriados, o parent deve completar para liberar os filhos, e o runtime só registra o gate como aprovado depois do retorno desses filhos ou do contrato equivalente do orquestrador.

## Decisão I - GitHub e CI

- Branch/worktree continuam governados pelo Kanban Hermes.
- `commit_sha` deve ser o SHA candidato realmente verificado, não apenas `HEAD` de árvore suja.
- `pull_request_url` só é preenchido quando PR existir.
- CI obrigatório depende da área alterada; para backend .NET, usar `backend/LoLCoach.slnx`, testes e format conforme specs existentes.
- Falha de CI/review gera `blocked` ou `failed` com evento acionável, nunca `completed`.

## Decisão J - Railway e deploy

Deploy não é parte do caminho automático padrão desta spec. Quando uma task futura autorizar deploy:

1. Issue deve trazer contrato explícito de ambiente e autorização.
2. `env:production` continua exigindo humano; não roda em `execution:auto` sem gate específico.
3. Runtime só persiste `railway_deployment_id` e eventos de deploy; Railway CLI/secrets pertencem a `devops`/`github-profile`.
4. Migration de banco continua separada do deploy.

## Decisão K - Observabilidade operacional

Consultas mínimas que a implementação/runbook deve oferecer:

```sql
-- último estado por issue
select run_id, linear_issue_id, agent, status, heartbeat_at, finished_at,
       commit_sha, pull_request_url, railway_deployment_id, tests_status, review_status, error
from public.agent_runs
where linear_issue_id = :issue
order by created_at desc
limit 5;

-- lock ativo
select linear_issue_id, run_id, agent, heartbeat_at, expires_at
from public.agent_execution_locks
where linear_issue_id = :issue;

-- timeline
select created_at, event_type, payload
from public.agent_events
where run_id = :run_id
order by created_at;
```

Os comandos reais devem ser executados por ferramenta que redija secrets; exemplos SQL não incluem credenciais.

## Riscos e mitigação

| Risco | Mitigação |
|---|---|
| Duplo worker na mesma issue | RPC claim transacional + unique/lock por `linear_issue_id` |
| Worker antigo finaliza após recovery | `finish` valida ownership e retorna false |
| Service role vaza em log | adapter não imprime response body; tasks proíbem anexar stdout sensível |
| Deploy/migration acidental | `env:production` bloqueado, migration com owner devops, deploy autorizado fora do runtime |
| Gate de review ignorado | `ExecutionResult` exige tests/review para perfis de implementação |
| Runtime package diverge do orquestrador instalado | integração fica em adapter, com testes de contrato e smoke local antes de ativar |
| SQL não aplicado | tasks dependentes bloqueiam até funções `public.hermes_*` existirem |

## Open Questions

Nenhuma. As decisões operacionais pendentes têm default seguro e owner: SQL com `devops`, deploy com `devops`/`github-profile`, execução production com aprovação humana.
