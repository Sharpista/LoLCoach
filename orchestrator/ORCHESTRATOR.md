# Orchestrator - Spec-Driven Execution Rules

## Pré-condições

Antes de delegar qualquer implementação:

1. Localizar a feature em `/specs/{id}-{name}`.
2. Confirmar `spec.md`, `design.md` e `tasks.md`.
3. Confirmar que os arquivos possuem conteúdo.
4. Ler `Open Questions`.
5. Validar dependências declaradas no front matter.
6. Identificar tasks pendentes.
7. Confirmar o issue Linear da demanda (ID/URL) e os campos de rastreabilidade no card; sem isso, não despachar (ver "Rastreabilidade Linear").

Quando a solicitação ainda for uma ideia, interromper a delegação e conduzir descoberta, suposições e priorização. Para uma feature pronta, confirmar também revisão red team, brief visual (se houver UI), cenários de teste e critérios de observabilidade.

Se houver pergunta em aberto ou dependência não concluída, definir `BLOCKED` e não implementar.

## Delegação

- Backend -> `dev-backend`
- Frontend -> `dev-frontend`
- Infra/DevOps -> `devops`, quando disponível
- Revisão -> `code-reviewer`

## Algoritmo

```text
0. Criar ou reutilizar o issue Linear; registrar ID/URL, workspace, branch, responsável, critérios, autorização e dependências no card.
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
14. Atualizar o issue Linear com SHA, PR, comandos, resultados, limitações e pendências — sem isso o card não fecha.
```

## Rastreabilidade Linear

Todo card do projeto tem um issue Linear correspondente: o issue é a referência externa da demanda e o card Hermes é o registro de execução. Linear relacionado: [LOL-71](https://linear.app/lolcoach/issue/LOL-71/tornar-explicita-a-sincronizacao-linear-hermes-no-orchestratormd).

0. **Antes do dispatch.** Criar o issue no workspace Linear do projeto ou reutilizar o issue que originou a demanda; não abrir issue duplicado para a mesma demanda.
1. **Registro no card Hermes.** O card informa, antes do dispatch: ID/URL do issue Linear, workspace e branch, responsável (assignee real), critérios de aceite, autorizações aplicáveis (publicação, deploy, secrets, produção) e dependências. Campo ausente é bloqueio, não suposição.
2. **Sincronização de estados.** Cada mudança de estado do card é refletida no issue Linear:

   | Card Hermes | Issue Linear | Condição |
   |---|---|---|
   | `ready` | Todo / backlog elegível | Issue criado ou reutilizado e card com os campos do item 1 |
   | `running` | In Progress | Dispatch confirmado (run/claim ativo), não apenas card criado |
   | `review` | In Review | Implementação concluída com evidência registrada; aguarda QA/review |
   | `done` | Done | QA e code review aprovados e critério de task concluída satisfeito |
   | `blocked` | Blocked | Impedimento registrado com motivo e condição de retomada |

   Só declarar a transição depois do fato observado; card criado não é card em execução.

3. **Atualização ao concluir.** O encerramento registra no issue Linear: SHA do commit, URL do PR quando houver, comandos executados, resultados observados, limitações e pendências com responsável. Sem autorização de publicação, o registro termina no commit local e no handoff.
4. **Limites.** Descrição e comentários do issue não recebem secrets, tokens, PII ou payload bruto. A sincronização não substitui autorização: deploy, release, alteração de secrets ou permissões, produção, destruição e descarte continuam dependendo de autorização explícita no escopo do card. Não alterar código de produto, migrations antigas ou histórico por causa da sincronização.

O runtime da Spec 009 não marca Linear como `Done`: `In Review` é o teto do fluxo automático e o fechamento pertence ao fluxo spec-driven após QA e code review. Enquanto o callback automático não estiver implementado, a transição é feita pelo orquestrador (ou pelo responsável autorizado) e o resultado fica no card.

## Critério de task concluída

```text
task marcada
+ código existente
+ build válido
+ testes aplicáveis passando
+ evidência versionada
+ issue Linear atualizado (SHA, PR, comandos, resultados, limitações, pendências)
= task concluída
```

## Mudança de escopo

Se um agente descobrir uma necessidade não prevista:

1. Não implementar automaticamente.
2. Registrar em `Proposed Changes` ou `Open Questions`.
3. Só continuar se a mudança for incorporada ao spec/design quando afetar comportamento ou arquitetura.
