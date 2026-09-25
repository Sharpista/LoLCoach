# LOL-61 - Revisão do contrato de payloads de eventos

Data: 2026-09-25T05:00:59+02:00
Workspace: `/home/alexandre/LoLSaas`
Base antes das alterações: `250557eb633e78029ce7610a844ed90a712e4ca5`
Primeiro commit local da revisão LOL-61: `6a145e47994ba22fa3392e58cea3611e15fa7930` (o SHA final do card deve ser confirmado por `git rev-parse HEAD` no encerramento)
Card Kanban: `t_074992a9`
Issue Linear: `LOL-61`

## Escopo validado

Esta revisão atualizou somente documentação da Spec 009 para definir o contrato JSON dos payloads de `agent_events`. Não houve alteração de código runtime, migrations aplicadas, banco, secrets, deploy, push ou PR.

Arquivos atualizados:

- `specs/009-hermes-runtime/spec.md`
- `specs/009-hermes-runtime/design.md`
- `specs/009-hermes-runtime/tasks.md`
- `specs/009-hermes-runtime/evidence/event-payload-contract-review.md`

## Fontes inspecionadas

- `AGENTS.md`: confirmou regra spec-driven, Open Questions sem pendências e evidência versionada.
- `specs/README.md`: confirmou fluxo de descoberta/spec/design/tasks/verificação/review.
- `orchestrator/ORCHESTRATOR.md`: confirmou pré-condições para delegar implementação e critério de task concluída.
- `specs/009-hermes-runtime/spec.md`, `design.md`, `tasks.md`: base da Spec 009.
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/runtime.py`: eventos `agent.dispatched`, `ExecutionResult.events`, status finais e gates.
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/kanban.py`: eventos `kanban.dispatched`, `kanban.status_changed`, `kanban.completed`, `kanban.blocked`, `kanban.failed`, `kanban.timeout` e campos emitidos pelo adapter.
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/supabase.py`: persistência de `payload` via PostgREST e `p_fields` no finish.
- `/home/alexandre/hermes-agent-runtime/sql/001_runtime_functions.sql`: eventos SQL atuais de claim, finish e recovery.
- `specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql`: proposta reaplicável já existente para `lock.rejected` e hardening.

## Decisões documentadas

1. `payload` deve ser sempre objeto JSONB não nulo; eventos sem campos adicionais usam `{}`.
2. O contrato é allowlist por `event_type`; campos não listados devem ser descartados ou exigir retorno da spec para `Open Questions` antes de implementar.
3. `lock.rejected` e `lock.expired` foram preservados como eventos obrigatórios de auditoria, com payloads mínimos: `reason` e `expired_at` respectivamente.
4. Readers devem tratar `payload NULL` legado como `{}` durante a transição (`coalesce(payload, '{}'::jsonb)`).
5. RPCs/migrations devem gravar payload explícito para eventos criados em SQL, sem alterar ownership ou aplicar migration neste card.
6. Dados proibidos incluem tokens, service role key, connection strings, headers/cookies, stdout/stderr bruto, stack trace bruto, `.env`, payloads externos não redigidos, PII e corpo de issues Linear.

## Campos permitidos por categoria

- Correlação operacional: `run_id`, `linear_issue_id`, `agent`, `assignee`, `kanban_task_id`.
- Versionamento/review/deploy: `commit_sha`, `pull_request_url`, `railway_deployment_id`, `deployment_url`.
- Gates e status: `tests_status`, `review_status`, `deploy_status`, `status`, `kanban_outcome`, `linear_status_to`.
- Métricas de verificação: `command`, `exit_code`, `total`, `passed`, `failed`, `skipped`, `duration_ms`, `artifact_path`.
- Lock/recovery: `ttl_seconds`, `expires_at`, `expired_at`, `last_heartbeat_at`, `recovered_by`, `previous_run_id`.
- Erros sanitizados: `error`, `error_code`, `blocked_reason`, `reason`.

## Cenários de teste derivados

- Claim aceito grava `run.created`, `lock.acquired` e `run.started` com payload objeto não nulo.
- Conflito de claim grava `lock.rejected` com `reason=RunConflict` sem run órfão.
- Dispatch/Kanban grava `kanban.*` com `kanban_task_id`, `status` e apenas campos da allowlist.
- Tests/review/deploy gravam somente status, contagens, IDs/URLs permitidas e caminhos de artifact não sensíveis.
- Finish grava `run.completed`/`run.failed`/`run.blocked`/`run.canceled` e `lock.released` com payload permitido.
- Recovery grava `lock.expired` com `expired_at` e `lock.recovered` com `{}` ou campos opcionais permitidos.
- Consultas de observabilidade usam `coalesce(payload, '{}'::jsonb)` para eventos legados.
- Varredura de segurança confirma ausência de tokens, connection strings, stdout/stderr bruto, stack traces e PII nos payloads persistidos.

## Evidência de verificação

Comandos executados nesta revisão:

```bash
git pull --ff-only
git status --short --branch
git rev-parse HEAD
git diff -- specs/009-hermes-runtime/spec.md specs/009-hermes-runtime/design.md specs/009-hermes-runtime/tasks.md
```

Resultado observado:

- `git pull --ff-only`: fast-forward de `f79608f` para `250557e`, trazendo a Spec 009 já existente para o workspace principal.
- `git rev-parse HEAD`: `250557eb633e78029ce7610a844ed90a712e4ca5` antes das alterações LOL-61.
- `git status --short`: somente arquivos de documentação da Spec 009 modificados/criados após esta revisão.
- Validação aplicada: revisão estática de coerência entre `spec.md`, `design.md`, `tasks.md`, runtime package e migration proposta.

## Limitações

- Não foram executados testes .NET porque o card é exclusivamente documental e não altera backend de produto.
- Não foi aplicada migration Supabase, não houve acesso a secrets e não houve deploy.
- A implementação futura ainda deve criar validação automatizada de payload/allowlist no pacote runtime/RPCs conforme `tasks.md`.

## Veredito

A Spec 009 permanece `READY`, com `Open Questions` sem pendências bloqueantes. O contrato de payloads de eventos agora possui campos permitidos por evento, dados proibidos, compatibilidade RPC/migration, cenários QA e observabilidade suficientes para liberar o card filho de implementação.
