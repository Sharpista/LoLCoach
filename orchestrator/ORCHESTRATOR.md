# Orchestrator - Spec-Driven Execution Rules

## Pré-condições

Antes de delegar qualquer implementação:

1. Localizar a feature em `/specs/{id}-{name}`.
2. Confirmar `spec.md`, `design.md` e `tasks.md`.
3. Confirmar que os arquivos possuem conteúdo.
4. Ler `Open Questions`.
5. Validar dependências declaradas no front matter.
6. Identificar tasks pendentes.

Quando a solicitação ainda for uma ideia, interromper a delegação e conduzir descoberta, suposições e priorização. Para uma feature pronta, confirmar também revisão red team, brief visual (se houver UI), cenários de teste e critérios de observabilidade.

Se houver pergunta em aberto ou dependência não concluída, definir `BLOCKED` e não implementar.

## Delegação

- Backend -> `dev-backend`
- Frontend -> `dev-frontend`
- Infra/DevOps -> `devops`, quando disponível
- Revisão -> `code-reviewer`

## Algoritmo

```text
1. Ler spec/design/tasks.
2. Validar Open Questions.
3. Validar dependências.
4. Identificar tasks [ ].
5. Classificar por especialidade.
6. Delegar apenas tasks executáveis.
7. Verificar evidências no código.
8. Executar build e testes.
9. Se falhar, retornar ao agente responsável.
10. Quando implementação estiver concluída, mudar para REVIEW.
11. Delegar ao code-reviewer.
12. Se aprovado, mudar para DONE.
13. Executar o verification gate final: conferir diff, segurança, acessibilidade, documentação, comandos reproduzidos e riscos residuais.
```

## Critério de task concluída

```text
task marcada
+ código existente
+ build válido
+ testes aplicáveis passando
+ evidência versionada
= task concluída
```

## Mudança de escopo

Se um agente descobrir uma necessidade não prevista:

1. Não implementar automaticamente.
2. Registrar em `Proposed Changes` ou `Open Questions`.
3. Só continuar se a mudança for incorporada ao spec/design quando afetar comportamento ou arquitetura.
