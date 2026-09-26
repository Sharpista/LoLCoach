# LOL-62 - Contrato de persistência de eventos acumulados

Data: 2026-09-25T05:13:33+02:00
Workspace: `/home/alexandre/LoLSaas`
Base antes das alterações LOL-62: `2efcaf2369ac9973d73e5226dac70173fa33918a`
Card Kanban: `t_709a5057`
Issue Linear: `LOL-62`

## Escopo validado

Esta revisão atualizou somente documentação da Spec 009 para definir o contrato de persistência dos eventos acumulados pelo adapter antes da finalização do run. Não houve alteração de código runtime, banco, secrets, deploy, push ou PR.

Arquivos atualizados nesta revisão:

- `specs/009-hermes-runtime/spec.md`
- `specs/009-hermes-runtime/design.md`
- `specs/009-hermes-runtime/tasks.md`
- `specs/009-hermes-runtime/evidence/lol-62-event-accumulation-contract.md`

Observação de contenção: `specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql` já estava modificado no workspace antes desta revisão e não foi alterado por este card.

## Fontes inspecionadas

- `AGENTS.md`: confirmou regra spec-driven e proibição de implementar sem spec/design/tasks.
- `specs/009-hermes-runtime/spec.md`, `design.md`, `tasks.md`: base da Spec 009 após LOL-61.
- `specs/009-hermes-runtime/evidence/event-payload-contract-review.md`: contrato anterior de allowlist de payloads.
- `specs/009-hermes-runtime/evidence/orchestrator-adapter-verification.md`: comportamento existente de `AgentRuntime`, `ExecutionResult.events` e `KanbanDispatcherAdapter`.
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/runtime.py`: atualmente persiste `result.events` antes do gate de sucesso, mas eventos observados por exceções do adapter não chegam ao runtime.
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/kanban.py`: atualmente acumula `kanban.dispatched`, `kanban.status_changed`, `kanban.failed` e `kanban.timeout`; nos caminhos de exceção, esses eventos precisam ser preservados por erro/envelope eventful futuro.
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/payloads.py`: allowlist e validação de payload por evento.
- `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/supabase.py`: `store.event` e `store.finish` são chamadas separadas; a spec agora exige ordem explícita antes de `finish`.

## Decisões documentadas

1. Eventos do adapter são uma fila ordenada por run e devem ser persistidos em sucesso, `blocked`, `failed` e timeout.
2. `finish` só pode ocorrer depois da validação, deduplicação e persistência dos eventos acumulados.
3. `kanban.timeout` precede `run.failed` com erro final sanitizado `TimeoutError`.
4. Falhas/bloqueios eventful devem carregar eventos acumulados em estrutura explícita, sem depender de `str(exc)`.
5. Erros persistidos continuam códigos curtos (`type(exc).__name__`, `error_code` permitido), sem stdout/stderr, stack trace, body externo, título/comentário Linear, tokens ou PII.
6. Deduplicação é determinística: preferir `(event_type, kanban_task_id, status, kanban_outcome)` quando houver task; caso contrário, usar payload normalizado canônico. A primeira ocorrência vence e a ordem entre eventos distintos é preservada.
7. A implementação futura deve ter testes cobrindo sucesso, gate bloqueado, falha técnica, timeout, erro eventful sanitizado, deduplicação e ordem `event -> finish`.

## Cenários de teste derivados

- Sucesso: `agent.dispatched`, eventos `kanban.*` e `kanban.completed` são persistidos antes de `run.completed`/`lock.released`.
- Gate ausente: `kanban.completed` ou `kanban.blocked` observado pelo adapter é persistido antes de `run.blocked`.
- Falha técnica: `kanban.failed` é persistido antes de `run.failed`; `error`/`error_code` é código curto sanitizado.
- Timeout: `kanban.timeout` é persistido antes de `run.failed` com `TimeoutError`.
- Payload inválido: campo fora da allowlist impede sucesso e gera finalização sanitizada, sem vazar payload bruto.
- Deduplicação: dois eventos idênticos de polling geram uma única linha lógica; mudanças reais de status não são colapsadas.
- Ordem: testes com store fake registram chamadas e confirmam que todos os `store.event(...)` acumulados antecedem `store.finish(...)`.

## Evidência de verificação

Comandos executados nesta revisão:

```bash
pwd && git status --short && git rev-parse HEAD && git branch --show-current
git diff -- specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql && git diff --stat
date --iso-8601=seconds && git rev-parse HEAD && git status --short
```

Resultado observado:

- Workspace confirmado: `/home/alexandre/LoLSaas`, branch `develop`, HEAD `2efcaf2369ac9973d73e5226dac70173fa33918a`.
- Antes das edições LOL-62, havia modificação local em `specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql`; esta revisão não tocou esse arquivo.
- Após as edições, alterações esperadas em `spec.md`, `design.md`, `tasks.md` e neste arquivo de evidência.
- Verificação aplicada: revisão estática de coerência entre Spec 009, adapter/runtime do pacote externo e contrato de payloads. Não foram executados testes .NET porque o card é exclusivamente documental.

## Limitações

- Nenhum código foi implementado nesta etapa.
- Nenhuma migration Supabase foi aplicada.
- Nenhum secret foi lido, impresso ou validado.
- A implementação futura ainda precisa alterar o pacote runtime/adapter para carregar eventos nos caminhos de exceção e provar o contrato com testes automatizados.

## Veredito

A Spec 009 permanece `READY`, com `Open Questions` sem pendências bloqueantes. O contrato agora explicita persistência de eventos acumulados para sucesso, blocked, failed e timeout, erro eventful sanitizado, ordem antes de `finish`, deduplicação e testes esperados.
