---
id: 009
name: hermes-runtime
status: READY
depends_on:
  - 008
linear_issues:
  - LOL-56
  - LOL-57
  - LOL-58
  - LOL-59
---

# Spec 009 - Runtime Hermes Linear/Supabase/GitHub/Railway

## Objetivo

Definir o contrato implementável para integrar o orquestrador Hermes às issues Linear do LoLSaas usando o pacote `hermes-agent-runtime`, Supabase como estado transacional de execução, GitHub como fonte de código/review/CI e Railway como alvo de deploy autorizado. A integração deve impedir execução concorrente da mesma issue, registrar ciclo de vida e eventos auditáveis, exigir gates de qualidade/review antes de mover trabalho, recuperar locks expirados com segurança e nunca aplicar migrações ou deploys sem autorização explícita.

## Base verificada

| Fato | Evidência |
|---|---|
| Branch desta spec | `docs/LOL-56-hermes-runtime` no workspace `/home/alexandre/LoLSaas/.worktrees/t_18845087` |
| Base local inicial | `c7a48ad8bc61c0ca5b42ecc87f96e953e52119d8` |
| Dependência upstream | `t_c07d3c53` preparou branch `feat/LOL-56-hermes-runtime` em `7696dabc6ba5123b2911b4da1103900a30ad01a7`, sem alterações próprias e com worktree limpo |
| Pacote de runtime | Repositório separado `/home/alexandre/hermes-agent-runtime`, remoto `Sharpista/hermes_agent_runtime`, `main@ed6a2d995116437b2098c9be12b576004ee0fd09`, testes `unittest` OK no upstream |
| Dispatcher instalado | Hermes Agent v0.21.2 (`939e45c9`), comando `hermes kanban dispatch`, spawn via `systemd-run --user --scope --unit=hermes-worker-kanban-<task>-run-<n>` |
| Banco Supabase | Tabelas `agent_runs`, `agent_execution_locks` e `agent_events` existem e constraints batem com o runtime; funções `public.hermes_*` ainda não existem |
| Issues Linear | LOL-56, LOL-57 e LOL-58 em `Todo`; LOL-59 em `Done` |
| Regras de projeto | `AGENTS.md`, `specs/README.md` e `orchestrator/ORCHESTRATOR.md` exigem spec/design/tasks sem Open Questions bloqueantes antes de implementar |

## Escopo

- Adaptar o orquestrador Hermes para buscar issues Linear elegíveis por labels operacionais e status.
- Usar `hermes-agent-runtime` como boundary de ciclo de vida, com `SupabaseStore` server-side.
- Aplicar a migração SQL do runtime em Supabase por owner único e com revisão, antes de ativar a integração.
- Registrar runs, locks, heartbeats, eventos, resultado de testes/review, commit/PR/deployment quando existirem.
- Roteamento determinístico de labels Linear para perfis Hermes.
- Recovery seguro de locks expirados antes de nova tentativa de claim.
- Gates de QA/review e limites claros para deploy Railway.
- Observabilidade operacional suficiente para auditar uma execução por `run_id`.

## Fora do escopo

- Reescrever o dispatcher Hermes instalado.
- Mover o código do pacote `hermes-agent-runtime` para dentro deste repositório.
- Criar PR, merge, release ou deploy real nesta etapa de especificação.
- Executar `sql/001_runtime_functions.sql` nesta task.
- Criar secrets, tokens ou variáveis reais em Linear, Supabase, GitHub ou Railway.
- Automatizar issues de `env:production`, `execution:human`, `execution:blocked` ou `risk:critical`.
- Marcar issues Linear como `Done` automaticamente; o runtime move no máximo até revisão/estado final de orquestração definido no design.

## Requisitos

### R1 - Elegibilidade e classificação Linear

1.1 Uma issue só pode iniciar execução automática quando `status = Todo`, possuir exatamente um label `agent:*` conhecido, possuir `execution:auto`, não possuir `risk:critical` e não possuir `env:production`.
1.2 Labels suportados nesta spec:

| Prefixo | Valores aceitos | Default |
|---|---|---|
| `agent:` | `orchestrator`, `backend`, `frontend`, `devops`, `qa`, `review`, `github` | sem default; obrigatório |
| `execution:` | `auto`, `human`, `blocked` | `auto` |
| `risk:` | `low`, `medium`, `high`, `critical` | `low` |
| `env:` | `local`, `preview`, `staging`, `production` | `local` |

1.3 Issues sem agent label, com agent label ambíguo, com status diferente de `Todo`, com label operacional desconhecido ou que exijam humano devem virar bloqueio registrado para orquestrador, sem spawn de worker.
1.4 O mapping canônico de agentes é:

| Linear label | Perfil Hermes |
|---|---|
| `agent:orchestrator` | `orchestrator` |
| `agent:backend` | `dev-backend` |
| `agent:frontend` | `dev-frontend` |
| `agent:devops` | `devops` |
| `agent:qa` | `qualidade` |
| `agent:review` | `code-reviewer` |
| `agent:github` | `github-profile` |

### R2 - Runtime e instalação

2.1 O pacote `hermes-agent-runtime` deve ser instalado no ambiente server-side do orquestrador, nunca no frontend ou em bundle público.
2.2 A integração deve usar `AgentRuntime` e injetar um callback de dispatch que chama o dispatcher Hermes instalado, preservando `ExecutionContext.run_id` em logs, eventos e handoffs.
2.3 A integração deve ser síncrona enquanto o contrato real do dispatcher instalado for síncrono; qualquer adaptação assíncrona deve ser encapsulada fora do event loop do Hermes.
2.4 Dependências de runtime do pacote permanecem na biblioteca padrão Python; dependências de ferramentas podem ficar restritas ao ambiente de execução.

### R3 - Supabase, migration e ownership

3.1 As tabelas `agent_runs`, `agent_execution_locks` e `agent_events` são a fonte de verdade para estado operacional.
3.2 A migração `sql/001_runtime_functions.sql` do pacote cria as funções `public.hermes_claim_run`, `public.hermes_heartbeat_run`, `public.hermes_finish_run` e `public.hermes_recover_expired_lock`.
3.3 A aplicação/validação da migração nesta entrega é responsabilidade única de `devops` (card LOL-59), com review de `code-reviewer` ou `dev-backend`, por ser banco compartilhado.
3.4 A migração deve ser aplicada uma única vez com autorização explícita do usuário/owner do banco; esta spec não autoriza aplicação automática.
3.5 Antes de aplicar, deve haver verificação read-only no catálogo confirmando as três tabelas e ausência/presença das funções esperadas; depois de aplicar, deve haver leitura confirmando assinaturas e grants.
3.6 Apenas `service_role` pode executar as RPCs; `public`, `anon` e `authenticated` não podem ter EXECUTE.
3.7 A service role key fica apenas no ambiente server-side do orquestrador (`SUPABASE_SERVICE_ROLE_KEY`), nunca em logs, artifacts, issue, PR, appsettings ou bundle.

### R4 - Claim, lock e concorrência

4.1 `claim` deve inserir run e lock numa única transação; se já houver lock ativo, a segunda execução não pode despachar worker nem criar run órfão.
4.2 A chave operacional de concorrência é `linear_issue_id`; apenas um run ativo por issue pode existir.
4.3 O TTL inicial do lock deve ser configurável, default `600s`, com limite mínimo `10s` e máximo `86400s` nas RPCs.
4.4 O dispatcher deve chamar recovery de lock expirado antes de tentar novo claim para a mesma issue.
4.5 Uma falha de claim deve ser tratada como conflito operacional e registrada sem vazar payloads do Supabase.

### R5 - Heartbeat e recovery

5.1 O runtime deve emitir heartbeat periódico antes de metade do TTL; default `30s` para TTL `600s`.
5.2 Heartbeat perdido ou perda de ownership deve interromper a conclusão normal e marcar o run como `failed` ou `blocked` conforme o erro.
5.3 `hermes_recover_expired_lock(issue_id)` só pode liberar lock com `expires_at <= now()` sob row/advisory lock.
5.4 Recovery deve marcar o run antigo como `failed` com erro `LockExpired`, emitir eventos `lock.expired` e `lock.recovered`, e remover o lock.
5.5 Se o worker antigo tentar finalizar após recovery, `finish` deve retornar falso e exigir revisão do orquestrador.

### R6 - Ciclo de vida e eventos

6.1 Cada run tem `run_id` com prefixo `run_` e ULID Crockford de 26 caracteres.
6.2 Eventos mínimos obrigatórios:

| Evento | Momento |
|---|---|
| `run.created` | claim aceito |
| `lock.acquired` | lock inserido |
| `run.started` | run inicia |
| `agent.dispatched` | antes de chamar worker |
| `tests.completed` | quando houver verificação executada |
| `review.completed` | quando houver review executado |
| `deploy.requested` | quando deploy autorizado for solicitado |
| `deploy.completed` | quando deployment autorizado concluir |
| `run.completed` / `run.failed` / `run.blocked` / `run.canceled` | finalização |
| `lock.released` | finalização normal |
| `lock.expired` / `lock.recovered` | recovery |

6.3 Todo evento deve persistir `payload` JSONB não nulo, usando `{}` quando o evento não exigir campos adicionais. O payload deve ser objeto JSON, nunca array, string ou blob textual.
6.4 O contrato de payloads é fechado por tipo de evento; campos fora das listas abaixo devem ser descartados ou mover a spec de volta para `Open Questions` antes da implementação.

| Evento | Payload obrigatório | Payload opcional permitido |
|---|---|---|
| `run.created` | `{}` | nenhum |
| `lock.acquired` | `{"ttl_seconds": number, "expires_at": string}` | nenhum |
| `run.started` | `{}` | `linear_status_to`, `risk`, `execution_mode`, `environment` |
| `agent.dispatched` | `{"kanban_task_id": string}` quando houver task Kanban; `{}` apenas se o dispatch ainda não tiver task materializada | `dispatch_backend`, `status`, `assignee` |
| `kanban.dispatched` | `{"kanban_task_id": string, "status": string}` | `assignee`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.status_changed` | `{"kanban_task_id": string, "status": string}` | `assignee`, `kanban_outcome`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.completed` | `{"kanban_task_id": string, "status": string}` | `commit_sha`, `pull_request_url`, `railway_deployment_id`, `tests_status`, `review_status`, `assignee`, `kanban_outcome`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.blocked` | `{"kanban_task_id": string, "status": string}` | `tests_status`, `review_status`, `assignee`, `kanban_outcome`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.failed` | `{"kanban_task_id": string, "status": string, "kanban_outcome": string}` | `assignee`, `run_id`, `linear_issue_id`, `agent` |
| `kanban.timeout` | `{"kanban_task_id": string, "status": string}` | `assignee`, `run_id`, `linear_issue_id`, `agent` |
| `tests.completed` | `{"tests_status": "passed"|"failed"|"skipped"}` | `command`, `exit_code`, `total`, `passed`, `failed`, `skipped`, `duration_ms`, `artifact_path` |
| `review.completed` | `{"review_status": "approved"|"changes_requested"|"blocked"}` | `reviewer`, `artifact_path`, `pull_request_url` |
| `deploy.requested` | `{"environment": "preview"|"staging"|"production"}` | `requested_by`, `pull_request_url`, `commit_sha` |
| `deploy.completed` | `{"railway_deployment_id": string, "environment": "preview"|"staging"|"production", "deploy_status": "succeeded"|"failed"|"canceled"}` | `deployment_url`, `commit_sha`, `duration_ms` |
| `run.completed` | `{}` | `commit_sha`, `pull_request_url`, `railway_deployment_id`, `tests_status`, `review_status` |
| `run.failed` | `{"error": string}` | `error_code`, `kanban_task_id`, `kanban_outcome` |
| `run.blocked` | `{"error": string}` | `blocked_reason`, `kanban_task_id`, `tests_status`, `review_status` |
| `run.canceled` | `{}` | `canceled_by`, `reason` |
| `lock.released` | `{}` | `finished_status` |
| `lock.rejected` | `{"reason": "RunConflict"}` | `attempted_run_id` |
| `lock.expired` | `{"expired_at": string}` | `last_heartbeat_at`, `ttl_seconds` |
| `lock.recovered` | `{}` | `recovered_by`, `previous_run_id` |

6.5 Tipos e formato dos campos permitidos:

| Campo | Tipo/formato | Observação |
|---|---|---|
| `run_id` | string `run_` + ULID | permitido como redundância operacional, mas a coluna `run_id` continua fonte de verdade |
| `linear_issue_id` | string de issue Linear (`LOL-61` ou id interno) | não incluir título/descrição da issue |
| `agent` / `assignee` / `reviewer` | string de perfil Hermes conhecido | sem nome pessoal quando não for necessário |
| `kanban_task_id` | string `t_...` | id interno Hermes |
| `status`, `linear_status_to`, `deploy_status`, `tests_status`, `review_status`, `kanban_outcome` | enum declarado nesta spec/design | valores desconhecidos devem virar `blocked`/`failed` sanitizado |
| `commit_sha` | SHA Git hexadecimal de 40 chars | não aceitar branch name no lugar de SHA |
| `pull_request_url`, `deployment_url` | URL HTTPS pública sem token/query secreto | remover query string se houver risco de token |
| `railway_deployment_id` | string id Railway | sem logs do deploy |
| `ttl_seconds`, `total`, `passed`, `failed`, `skipped`, `duration_ms`, `exit_code` | number inteiro | sem stdout bruto |
| `expires_at`, `expired_at`, `last_heartbeat_at` | timestamp ISO-8601 UTC | sem timezone local ambíguo |
| `command` | nome/comando sanitizado | sem env vars, tokens, connection strings ou argumentos secretos |
| `artifact_path` | caminho relativo no repo ou path de artifact não sensível | não apontar para `.env`, logs brutos ou diretórios com credenciais |
| `error`, `error_code`, `blocked_reason`, `reason`, `canceled_by`, `requested_by`, `recovered_by` | código/tipo curto sanitizado | sem mensagem crua de exceção quando a origem puder conter segredo |

6.6 Dados sempre proibidos no payload e em campos derivados: tokens Linear/GitHub/Railway, `SUPABASE_SERVICE_ROLE_KEY`, anon key quando combinada com endpoint privado, connection strings, headers HTTP, cookies, nomes/e-mails de usuários finais, título/descrição/comentários completos de Linear, stdout/stderr bruto, stack trace bruto, conteúdo de `.env`, secrets Railway/Supabase/GitHub, PII de jogadores/usuários e qualquer payload de request/resposta externa não redigido.
6.7 Compatibilidade RPC/migration: `hermes_claim_run`, `hermes_finish_run` e `hermes_recover_expired_lock` devem gravar payloads conforme este contrato. Migrações existentes que já criam eventos sem payload devem ser ajustadas de forma reaplicável para usar `{}` e não podem quebrar readers que ainda leem payload nulo; readers devem tratar `null` legado como `{}` durante a transição.
6.8 Status finais aceitos no runtime: `completed`, `failed`, `blocked`, `canceled`.
6.9 Para workers `dev-backend`, `dev-frontend` e `devops`, `ExecutionResult` precisa ter `tests_status = passed` e `review_status = approved` antes de `completed`.

### R7 - GitHub, QA e review gates

7.1 Implementações devem ocorrer em branch/worktree isolado conforme o board Hermes; PR/push só ocorrem quando a task permitir.
7.2 O resultado de execução deve registrar `commit_sha` quando houver alteração versionada e `pull_request_url` quando houver PR.
7.3 Gates mínimos para código:

| Perfil | Gate de teste | Gate de review |
|---|---|---|
| `dev-backend` | build/testes aplicáveis passados | review aprovado |
| `dev-frontend` | build/testes aplicáveis passados | review aprovado |
| `devops`/`github-profile` | verificação estática ou pipeline aplicável passado | review aprovado |
| `qualidade` | relatório QA aprovado ou bloqueado com evidência | não aplicável, salvo tarefa pedir |
| `code-reviewer` | reprodução ou inspeção com lacunas declaradas | parecer emitido |
| `orchestrator` | spec/design/tasks/gates verificados | não aplicável |

7.4 Falha de CI, teste ou review deve deixar a issue em estado de rework/review definido pelo orquestrador e registrar evento; não pode ser mascarada como sucesso.

### R8 - Railway e limites de deploy

8.1 Deploy Railway automático por issue é proibido sem label/estado e autorização explícita definidos em tarefa própria.
8.2 Issues `env:production` são bloqueadas para humano por padrão e não entram em execução automática.
8.3 Quando houver deploy autorizado, o runtime pode registrar `railway_deployment_id`, mas não decide plano, domínio, secret ou billing.
8.4 Migrations de aplicação e functions Supabase não rodam como efeito colateral de deploy.

### R9 - Segurança

9.1 O adapter Supabase deve usar HTTPS e service role server-side.
9.2 Exceptions persistidas em `agent_runs.error` devem ser sanitizadas por tipo ou código, nunca mensagem bruta quando ela puder conter segredo.
9.3 Logs e eventos devem usar `run_id`, `linear_issue_id`, `agent`, `status`, `commit_sha`, `pull_request_url` e `railway_deployment_id` como campos estruturados permitidos.
9.4 Busca/consulta de Linear, GitHub e Railway deve usar tokens do ambiente do orquestrador, com escopo mínimo e sem impressão em stdout.
9.5 A task que aplicar migration deve tratar qualquer log local contendo credencial como sensível e não anexar saída crua com secrets.

### R10 - Observabilidade e operação

10.1 Deve existir consulta/runbook para responder: issue em execução, lock ativo, último heartbeat, evento final, erro sanitizado, SHA/PR/deployment vinculados.
10.2 O dispatcher deve emitir logs com `run_id` em cada transição externa: claim, status Linear, spawn Hermes, QA/review, recovery, finish.
10.3 O recovery loop deve ter intervalo configurável e métrica/contagem de locks recuperados.
10.4 Toda execução bloqueada por política deve registrar motivo acionável para o orquestrador.

## Critérios de aceitação

- AC1 `spec.md`, `design.md` e `tasks.md` existem em `specs/009-hermes-runtime/`, com `status: READY` e `Open Questions` sem pendência bloqueante.
- AC2 A documentação declara arquitetura, boundaries e responsabilidades entre Linear, orquestrador Hermes, pacote runtime, Supabase, GitHub e Railway.
- AC3 A documentação inclui o contrato de labels e roteamento para perfis Hermes.
- AC4 O contrato de run lifecycle cobre claim, lock, heartbeat, finish, recovery, status finais, eventos mínimos e payload JSON permitido por evento.
- AC5 A spec declara que a migração `sql/001_runtime_functions.sql` pertence a `devops` nesta entrega, requer autorização e não é aplicada nesta task.
- AC6 A spec declara gates de QA/review antes de concluir código e limita deploy Railway a ação autorizada.
- AC7 A spec explicita tratamento de segurança para service role key, tokens, logs, payloads, PII e dados proibidos.
- AC7.1 A documentação define compatibilidade RPC/migration para payload `{}` não nulo e leitura de payload nulo legado como `{}` durante transição.
- AC8 `tasks.md` decompõe a implementação em tarefas por perfil com dependências e evidências esperadas.
- AC9 Não há alteração de código runtime, migrations antigas, banco, secrets, deploy, push ou PR nesta task.
- AC10 Evidência local registra inspeção de AGENTS/ORCHESTRATOR/specs, runtime package e estado git.

## Dependências

- `t_c07d3c53`: preparação do worktree e inspeção upstream (concluída).
- `devops`: validar/aplicar `sql/001_runtime_functions.sql` em Supabase compartilhado antes de ativar execution real com `SupabaseStore.claim()`.
- `orchestrator`: instalar o pacote no ambiente correto e adaptar o polling/dispatch Linear.
- `github-profile`/`devops`: configurar secrets e permissões de GitHub/Railway quando houver tasks de publicação.
- `code-reviewer` e `qualidade`: gates obrigatórios antes de promover automação para uso contínuo.

## Open Questions

Nenhuma pendência bloqueante. A ausência atual das funções `public.hermes_*` no Supabase é uma dependência operacional mapeada com owner (`devops`, card LOL-59) e gate de autorização, não uma pergunta de arquitetura.
