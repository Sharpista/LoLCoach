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

## Decisão A - Boundary do runtime

**Escolhido:** usar `hermes-agent-runtime` como boundary server-side de lifecycle, mantendo a integração Linear/GitHub/Railway no orquestrador LoLSaas/Hermes.

```text
Linear issue + labels
        |
        v
Orquestrador Hermes (poll/filter/recovery)
        |
        | classify + execute(issue, dispatch)
        v
hermes-agent-runtime
        |
        | RPC service_role
        v
Supabase agent_runs / agent_execution_locks / agent_events
        |
        +--> dispatcher Hermes instalado -> Kanban/workers -> GitHub/CI/review
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

Eventos são append-only e devem permitir auditoria por `run_id` sem armazenar segredo.

Payload permitido:

- `commit_sha`
- `pull_request_url`
- `railway_deployment_id`
- `tests_status`
- `review_status`
- contagens de teste, nomes de comandos e exit code
- motivo sanitizado por código/tipo (`ExecutionBlocked`, `RunConflict`, `LockExpired`)

Payload proibido:

- connection string
- `SUPABASE_SERVICE_ROLE_KEY`
- token Linear/GitHub/Railway
- stdout completo de comandos que possam conter secrets
- raw exception message quando a origem possa embutir credenciais

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
