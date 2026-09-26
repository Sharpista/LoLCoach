# Orchestrator - Spec-Driven Execution Rules

## Pré-condições

Antes de delegar qualquer implementação:

1. Localizar a feature em `/specs/{id}-{name}`.
2. Confirmar `spec.md`, `design.md` e `tasks.md`.
3. Confirmar que os arquivos possuem conteúdo.
4. Ler `Open Questions`.
5. Validar dependências declaradas no front matter.
6. Identificar tasks pendentes.
7. Confirmar o workspace do card: worktree isolado em `.worktrees/<task_id>` com branch própria — o checkout principal não é workspace de card (ver "Política de workspace (worktree isolado por card)" abaixo).

Quando a solicitação ainda for uma ideia, interromper a delegação e conduzir descoberta, suposições e priorização. Para uma feature pronta, confirmar também revisão red team, brief visual (se houver UI), cenários de teste e critérios de observabilidade.

Se houver pergunta em aberto ou dependência não concluída, definir `BLOCKED` e não implementar.

## Política de workspace (worktree isolado por card)

Vale para a criação e a execução de qualquer card — pelo dispatcher, por este orquestrador ou por agente delegado — antes de abrir o card e ao encerrá-lo.

1. Todo card que edite código recebe **worktree isolado** em `.worktrees/<task_id>`, com branch determinística própria — padrão do dispatcher `wt/<task_id>`, ou `$HERMES_KANBAN_BRANCH` quando o card for vinculado a um projeto.
2. O **checkout principal nunca é workspace de card**. Ele serve apenas para leitura/inspeção e para `git worktree add`.
3. Card que precise alterar o estado local do checkout principal exige **autorização explícita** do usuário/orquestrador e **um único autor** por branch.
4. Worktree de card concluído só é removido se `git status --porcelain -uall` estiver vazio, o branch/commit estiver preservado (integrado em `origin/develop` ou branch local mantida) e a lista de caminhos tiver sido comentada antes da remoção.
5. `git worktree remove --force` é proibido sem autorização; arquivo não rastreado exige preservação (cópia/bundle/commit) antes de qualquer remoção.
6. Antes de remover, confirmar no board que o card dono está `done`/`archived` e que nenhum run ativo usa o diretório.

Consequências operacionais na delegação:

- Ao criar o card, já definir o worktree isolado (`.worktrees/<task_id>`) e a branch determinística; card que edite código nunca é despachado com o checkout principal como workspace.
- Autorização e autoria única ficam registradas no próprio card (comentário) antes de qualquer toque no checkout principal.
- A remoção do worktree é passo explícito de encerramento: pré-voo de status/refs, comentário com a lista de caminhos e conferência do estado do card no board.
- Sem autorização de publicação (push/PR/merge), o encerramento da rodada termina no commit local e no handoff para QA/review.

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
