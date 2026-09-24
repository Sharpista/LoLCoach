# Evidência de inspeção - Spec 009 Hermes Runtime

## Contexto local

- Workspace: `/home/alexandre/LoLSaas/.worktrees/t_18845087`
- Branch: `docs/LOL-56-hermes-runtime`
- SHA no início da inspeção: `c7a48ad8bc61c0ca5b42ecc87f96e953e52119d8`
- Escopo desta task: documentação de spec/design/tasks. Nenhuma migration, deploy, push, PR ou alteração de runtime foi executada.

## Fontes inspecionadas

| Fonte | Evidência usada |
|---|---|
| `AGENTS.md` | Regra spec-driven; estados; fluxo READ -> VALIDATE -> PLAN -> DELEGATE -> VERIFY -> TEST -> REVIEW -> DONE; proibição de implementar sem spec/design/tasks |
| `specs/README.md` | Estrutura obrigatória `spec.md`, `design.md`, `tasks.md`; Open Questions bloqueantes; evidência mínima |
| `orchestrator/ORCHESTRATOR.md` | Pré-condições de delegação, algoritmo de execução, critério de task concluída |
| `specs/008-railway-deploy/*` | Estilo de frontmatter, requisitos, critérios de aceite, design com decisões e decomposição de tasks |
| Kanban parent `t_c07d3c53` | Runtime package separado, dispatcher real, estado do Supabase e Linear, dependência das RPCs ausentes |
| `/home/alexandre/hermes-agent-runtime/README.md` | Boundary do pacote, setup, variáveis, limitação de não pollar Linear/deploy/Done |
| `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/runtime.py` | `AgentRuntime`, classificação de labels, `ExecutionResult`, heartbeat, gates de teste/review, finish sanitizado |
| `/home/alexandre/hermes-agent-runtime/src/hermes_agent_runtime/supabase.py` | Uso de PostgREST/RPC, service role, tratamento de HTTPError sem body bruto |
| `/home/alexandre/hermes-agent-runtime/sql/001_runtime_functions.sql` | RPCs de claim, heartbeat, finish e recovery; grants para `service_role`; revokes de roles públicas |
| `/home/alexandre/hermes-agent-runtime/tests/test_runtime.py` | Cobertura existente de ULID, sucesso, conflito, falha, gate ausente e labels sensíveis |

## Achados consolidados

- O pacote `hermes-agent-runtime` é deliberadamente uma boundary server-side. Ele não busca Linear, não cria PR, não faz deploy Railway e não marca issue Done; o orquestrador deve injetar esses callbacks.
- O dispatcher instalado é separado do pacote e deve ser adaptado, não substituído.
- As tabelas Supabase já existem segundo o handoff upstream, mas as funções `public.hermes_*` ainda não existem. Isso é dependência operacional bloqueante para qualquer execução real com `SupabaseStore.claim()`.
- A migration SQL toca banco compartilhado e, nesta entrega, pertence a `devops`/LOL-59 com autorização explícita.
- O runtime já codifica política segura de labels: bloqueia status diferente de `Todo`, agent ambíguo/ausente, `execution` não-auto, `risk:critical` e `env:production`.
- Para `dev-backend`, `dev-frontend` e `devops`, `tests_status=passed` e `review_status=approved` são obrigatórios para conclusão.
- Eventos e erros precisam ser sanitizados para não gravar service role key, connection strings ou tokens.

## Verificações executadas nesta task

| Comando/ação | Resultado |
|---|---|
| `pwd && git status --short && git rev-parse HEAD && git branch --show-current` | workspace correto, branch `docs/LOL-56-hermes-runtime`, SHA `c7a48ad8bc61c0ca5b42ecc87f96e953e52119d8`; status limpo antes das edições |
| Busca de specs existentes em `specs/` | Specs 001-008 encontradas; 008 usada como referência de formato |
| Busca por `agent_runs`, `agent_execution_locks`, `agent_events` no repo LoLSaas | Nenhuma definição SQL/C# no repo principal; dependência está no pacote externo/runtime e banco real |
| Leitura de runtime package | Contratos de lifecycle, store Supabase, SQL e testes incorporados na spec/design/tasks |

## Não executado

- Testes do LoLSaas: não executados porque esta task altera apenas documentação de spec e não código da aplicação.
- Testes do pacote `/home/alexandre/hermes-agent-runtime`: não reexecutados nesta task; o handoff upstream registrou `PYTHONPATH=src python3 -m unittest discover -s tests -v` com 6 testes OK.
- Probes vivos em Supabase/Linear: não repetidos para evitar acesso a credenciais nesta task; evidência usada vem do parent `t_c07d3c53`.
- Migration SQL: não aplicada por escopo e falta de autorização explícita nesta task.
- Deploy Railway / GitHub PR / push: não executados por escopo.

## Veredito documental

A spec 009 está pronta para revisão/decomposição de implementação. Não há Open Questions arquiteturais pendentes; há uma dependência operacional explícita: aplicar as RPCs `public.hermes_*` por `devops`/LOL-59 antes de ativar execução real com Supabase.
