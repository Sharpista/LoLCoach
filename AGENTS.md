# AGENTS.md

## Regra Spec-Driven

Nenhum agente deve implementar uma feature sem que existam:

- `spec.md`
- `design.md`
- `tasks.md`
- seção `Open Questions` sem pendências

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

## Estados

- `DRAFT`: documentação incompleta.
- `READY`: pronta para implementação.
- `IN_PROGRESS`: implementação em andamento.
- `BLOCKED`: há dependência ou pergunta em aberto.
- `REVIEW`: implementação concluída e aguardando revisão.
- `DONE`: implementação e revisão concluídas.
