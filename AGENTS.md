# AGENTS.md

## Frontend stack (LoLSaas)

- Framework: Angular 22
- TypeScript 6
- RxJS 7.8
- Tests: Vitest
- Frontend directory: `/home/alexandre/LoLSaas/frontend`
- Do not use React or Vite in this project.

## Regra Spec-Driven

Nenhum agente deve implementar uma feature sem que existam:

- `spec.md`
- `design.md`
- `tasks.md`
- seção `Open Questions` sem pendências

Para ideias ainda ambíguas, faça primeiro descoberta de produto e mapeamento de suposições. Só depois transforme o resultado em `spec.md`; antes de `READY`, faça uma revisão red team da especificação. Use `specs/README.md` como roteiro.

## Fluxo obrigatório

```text
READ -> VALIDATE -> PLAN -> DELEGATE -> VERIFY -> TEST -> REVIEW -> DONE
```

Nunca executar diretamente:

```text
REQUEST -> IMPLEMENT
```

## Regras

- Implementar apenas requisitos existentes em `spec.md`.
- Respeitar decisões técnicas existentes em `design.md`.
- Executar apenas tasks atribuídas.
- Não adicionar dependências sem justificativa.
- Não modificar migrations antigas.
- Não remover ou ignorar testes existentes.
- Se uma dúvida alterar regra de negócio ou arquitetura, registrar em `Open Questions` e bloquear a implementação.
- Uma task só é concluída quando houver código correspondente, build válido e testes aplicáveis passando.
- Code review só começa após todas as tasks de implementação estarem concluídas e os testes passarem.
- Features de frontend devem consultar `.hermes/DESIGN.md`, registrar o brief visual em `design.md` e validar acessibilidade, estados de erro e comportamento responsivo.
- Use cenários de teste derivados dos critérios de aceitação; para UI, valide também no navegador quando a mudança depender de layout, interação ou estados visuais.
- Falhas devem seguir diagnóstico sistemático antes de alterar código. Não mascarar erro com retry, fallback ou mudança de teste sem registrar a causa.
- Antes de deploy, conferir configuração, segredos, migrações, rollback e observabilidade. Após deploy, executar o canário definido na task e registrar o resultado.
- Antes da revisão final, executar uma passagem de annealing: remover complexidade acidental, duplicação, TODO sem issue e código morto sem mudar o escopo.
- Revisões de segurança devem procurar vazamento de segredo, PII, logs sensíveis, autorização, validação de entrada e dependências; contexto e limitações devem ficar registrados na evidência.

## Estados

- `DRAFT`: documentação incompleta.
- `READY`: pronta para implementação.
- `IN_PROGRESS`: implementação em andamento.
- `BLOCKED`: há dependência ou pergunta em aberto.
- `REVIEW`: implementação concluída e aguardando revisão.
- `DONE`: implementação e revisão concluídas.

## Shared Project Memory Rules

All Hermes agents working on this repository use OpenViking as the shared project memory.

### Before starting a task

Search OpenViking when the task may depend on:

- architecture decisions
- previous implementation decisions
- project conventions
- known bugs and fixes
- integration behavior
- important directories
- framework or dependency choices

Use `viking_search` before re-discovering information that may already exist.

### When discovering durable project knowledge

When you discover information likely to help another agent later, store it using `viking_remember`.

Examples of information that should be remembered:

- architecture decisions
- technology and framework choices
- important project paths
- API contracts
- database conventions
- recurring bugs and their solutions
- infrastructure configuration decisions
- authentication behavior
- integration behavior
- coding conventions
- decisions made during implementation

Before calling `viking_remember`:

1. Verify that the information is correct.
2. Make the memory understandable without the current conversation.
3. Include the project name (`LoLSaas`) when useful.
4. Prefer concise factual memories.
5. Search first when uncertain whether the memory already exists, and avoid unnecessary duplication.

### Do not remember

Do not store:

- temporary terminal output
- transient compilation errors
- one-off debugging observations
- secrets, API keys, passwords, or tokens
- temporary task status
- speculative information

### Shared knowledge

OpenViking is the shared durable memory for this project. Local `MEMORY.md` is agent-specific and must not be treated as the authoritative shared project knowledge.
