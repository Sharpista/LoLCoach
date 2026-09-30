# LOL-63 - Reconciliação Spec 009 com pipeline oficial

## Escopo executado

Atualização documental, sem implementação de código, sem migration, sem deploy, sem push e sem PR.

Objetivo do card: reconciliar `specs/009-hermes-runtime/spec.md`, `design.md` e `tasks.md` com o fluxo oficial:

```text
Linear -> Orchestrator Hermes -> contexto/memória -> Supabase -> seleção de especialista -> dev-backend/dev-frontend/devops -> qualidade -> code-reviewer -> GitHub -> Railway
```

## Evidência de contexto lido

Arquivos inspecionados nesta execução:

- `AGENTS.md` carregado no contexto do agente.
- `orchestrator/ORCHESTRATOR.md`.
- `specs/README.md`.
- `specs/009-hermes-runtime/spec.md`.
- `specs/009-hermes-runtime/design.md`.
- `specs/009-hermes-runtime/tasks.md`.

OpenViking consultado para decisões prévias do projeto com a query `LoLSaas Spec 009 Hermes runtime pipeline Linear Orchestrator Supabase GitHub Railway decisions`; não houve resultados retornados.

## Alterações documentais

### `spec.md`

- Adicionado `LOL-63` ao front matter de issues vinculadas.
- Objetivo atualizado para declarar explicitamente o pipeline oficial completo.
- Escopo ampliado para cobrir contexto/memória antes da seleção e dispatch de especialista.
- Criada seção `Pipeline oficial reconciliado` com:
  - diagrama textual de transições verificáveis;
  - tabela de entradas, saídas e ownership por etapa;
  - classificação de gaps entre a boundary runtime atual e o pipeline completo.
- Adicionado `R11 - Contexto, memória e handoff oficial`.
- Adicionado `AC11` para validar diagrama, ownership, gaps e plano de implementação/QA.

### `design.md`

- Contexto verificado atualizado para registrar a decisão LOL-63.
- Diagrama da boundary do runtime atualizado para incluir contexto/memória, seleção de especialista, qualidade, code-reviewer e GitHub/CI autorizado.
- Adicionada `Decisão A.1 - Pipeline oficial e contratos de handoff` com contratos de entrada/saída e gates entre etapas.

### `tasks.md`

- Adicionado `T-DOC-7` como item documental concluído para LOL-63.
- Adicionados `T-ORCH-10` e `T-ORCH-11` para implementação futura de bundle de contexto/memória redigido e seleção de especialista.
- Adicionados `T-QA-9` e `T-QA-10` para validar fluxo ponta a ponta fake/controlado e gaps classificados.
- Dependências e evidências esperadas atualizadas para incluir LOL-63.

## Gaps classificados

| Gap | Classificação | Owner/plano |
|---|---|---|
| Runtime não decide labels Linear | Implementação do orquestrador | `T-ORCH-2`, `T-QA-1` |
| Contexto/memória não tinha contrato formal | Contrato de integração | `R11`, `T-ORCH-10`, `T-QA-9` |
| Funções `public.hermes_*` ausentes | Dependência operacional Supabase | `T-DB-1..T-DB-5`, owner `devops` |
| Seleção de especialista depende do mapeamento Linear/perfis | Roteamento | `R1`, `T-ORCH-11`, `T-QA-10` |
| QA/review não são garantidos pelo runtime isolado | Gate de processo | `R7`, `T-BE-3`, `T-QA-4` |
| GitHub/PR/CI exigem autorização e publicação real | Integração externa | `T-GH-1..T-GH-3` |
| Railway deploy não é caminho automático padrão | Segurança/ops | `R8`, `T-OPS-1..T-OPS-3` |

## Verificações

Executado:

- Inspeção estática dos arquivos de especificação e regras do projeto.
- Consulta OpenViking sem resultados relevantes.
- Atualização documental com status `READY` e `Open Questions` sem pendência bloqueante.

Não executado:

- `dotnet build` / `dotnet test`: não aplicável; esta task é documental e o aceite proíbe implementação de código.
- Migration Supabase: não aplicável e explicitamente fora de escopo.
- Deploy Railway: não aplicável e explicitamente fora de escopo.
- Push/PR: não autorizado nesta task.

## Estado git observado no início da execução

```text
branch: develop
HEAD: 2efcaf2369ac9973d73e5226dac70173fa33918a
status inicial relevante: spec.md, design.md e tasks.md já tinham alterações locais anteriores de LOL-62; esta execução preservou e estendeu essas alterações.
```

## Resultado

Spec 009 permanece `READY`. A reconciliação LOL-63 está documentada com transições verificáveis, ownership, contratos de handoff, gaps classificados e tasks de implementação/QA derivadas. Não há Open Questions bloqueantes.
