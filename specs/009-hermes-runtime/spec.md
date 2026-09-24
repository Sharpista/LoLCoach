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

6.3 Payloads de eventos podem conter ids, SHAs, URLs de PR/deployment e status; não podem conter tokens, connection strings, service role key, raw exception com segredo ou PII desnecessária.
6.4 Status finais aceitos no runtime: `completed`, `failed`, `blocked`, `canceled`.
6.5 Para workers `dev-backend`, `dev-frontend` e `devops`, `ExecutionResult` precisa ter `tests_status = passed` e `review_status = approved` antes de `completed`.

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
- AC4 O contrato de run lifecycle cobre claim, lock, heartbeat, finish, recovery, status finais e eventos mínimos.
- AC5 A spec declara que a migração `sql/001_runtime_functions.sql` pertence a `devops` nesta entrega, requer autorização e não é aplicada nesta task.
- AC6 A spec declara gates de QA/review antes de concluir código e limita deploy Railway a ação autorizada.
- AC7 A spec explicita tratamento de segurança para service role key, tokens, logs e payloads.
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
