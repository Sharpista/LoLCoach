# Evidência de implementação - runtime Hermes / adapter Kanban

## Contexto

- Card: `t_e792d9f1` / LOL-56, LOL-57, LOL-58
- Workspace LoLSaas: `/home/alexandre/LoLSaas/.worktrees/t_e792d9f1`
- Branch LoLSaas: `feat/LOL-56-runtime-integration`
- Base LoLSaas: `c7a48ad8bc61c0ca5b42ecc87f96e953e52119d8`
- Pacote runtime alterado: `/home/alexandre/hermes-agent-runtime`
- Branch runtime: `main`
- Base runtime antes das alterações: `ed6a2d995116437b2098c9be12b576004ee0fd09`

## O que foi implementado

- `KanbanDispatcherAdapter` em `src/hermes_agent_runtime/kanban.py`, como boundary injetável para o dispatcher Kanban real.
- O adapter não reimplementa nem substitui o dispatcher Hermes: ele recebe callbacks de start/read-only snapshot, registra eventos estruturados com `run_id` e só retorna `ExecutionResult` depois de estado terminal do Kanban.
- `AgentRuntime` agora bloqueia label `agent:*` desconhecido, registra evento `lock.rejected` via store fake quando há `RunConflict`, e persiste eventos retornados pelo dispatch antes de aplicar o gate de qualidade/review.
- `SupabaseStore.claim()` passou a esperar retorno booleano da RPC `hermes_claim_run`; `false` vira `RunConflict` sanitizado.
- `sql/001_runtime_functions.sql` ajustado para criar o run como `queued`, promover para `running` após lock adquirido e persistir `lock.rejected`/`run.failed` em conflito sem criar lock ativo órfão.
- Exports e README do pacote atualizados.
- Testes unitários adicionados para adapter Kanban, bloqueio de label desconhecido, auditoria de lock rejeitado e persistência de eventos antes do gate ausente.

## Arquivos alterados no pacote `/home/alexandre/hermes-agent-runtime`

- `README.md`
- `sql/001_runtime_functions.sql`
- `src/hermes_agent_runtime/__init__.py`
- `src/hermes_agent_runtime/kanban.py`
- `src/hermes_agent_runtime/runtime.py`
- `src/hermes_agent_runtime/supabase.py`
- `tests/test_runtime.py`

## Arquivos adicionados no workspace LoLSaas

- `specs/009-hermes-runtime/spec.md`
- `specs/009-hermes-runtime/design.md`
- `specs/009-hermes-runtime/tasks.md`
- `specs/009-hermes-runtime/evidence/inspection.md`
- `specs/009-hermes-runtime/evidence/orchestrator-adapter-verification.md`

## Verificações executadas

| Comando | Diretório | Resultado |
|---|---|---|
| `pwd && git status --short && git rev-parse HEAD && git branch --show-current && git status --branch --short` | `/home/alexandre/LoLSaas/.worktrees/t_e792d9f1` | passou; workspace correto, branch `feat/LOL-56-runtime-integration`, base `c7a48ad8bc61c0ca5b42ecc87f96e953e52119d8` |
| `PYTHONPATH=src python3 -m unittest discover -s tests -v && python3 -m compileall -q src` | `/home/alexandre/hermes-agent-runtime` | passou; 10 testes executados, 10 OK, compileall sem erros |
| `which hermes && hermes --version` | `/home/alexandre/LoLSaas/.worktrees/t_e792d9f1` | passou; Hermes Agent v0.21.2 (`939e45c9`), install `/home/alexandre/.hermes/hermes-agent` |
| inspeção estática de `gateway/kanban_watchers_dispatcher.py` e `hermes_cli/kanban_db_dispatch.py` | `/home/alexandre/.hermes/hermes-agent` | executado; confirmou dispatcher instalado separado e resultado `DispatchResult.spawned` não deve ser tratado como sucesso final |

## Não executado

- Migração Supabase: não aplicada por escopo; depende do owner `devops`/LOL-59 e autorização explícita.
- Probe real Supabase/Linear: não executado para evitar uso de credenciais nesta task.
- `dotnet build` / `dotnet test`: não aplicável à alteração principal; o escopo alterou o pacote Python `hermes-agent-runtime` e documentação de spec, sem código .NET do produto LoLCoach.
- Dispatcher real com spawn de workers: não executado; testes cobrem o contrato do adapter com snapshots fake para não abrir workers reais nem tocar o board além desta task.

## Limitações e dependências

- A RPC `public.hermes_claim_run` passa a retornar `boolean`; a aplicação da migration precisa validar compatibilidade no banco compartilhado antes de uso real.
- `KanbanDispatcherAdapter` é uma boundary injetável: a integração concreta com leitura do board real deve fornecer callbacks que convertam o estado Kanban para `KanbanTaskSnapshot`.
- Ativação real com `SupabaseStore.claim()` continua bloqueada até as funções `public.hermes_*` existirem no Supabase.
