# Tasks 009 - Runtime Hermes Linear/Supabase/GitHub/Railway

Estado da spec: `READY` (ver `spec.md`). Nenhuma implementação começa antes de o orquestrador confirmar `spec.md`, `design.md`, `tasks.md` e a ausência de Open Questions bloqueantes.

Responsáveis canônicos: `orchestrator`, `dev-backend`, `devops`, `github-profile`, `qualidade`, `code-reviewer`.

Regra herdada de `AGENTS.md`/`ORCHESTRATOR.md`: task marcada + artefato existente + verificação aplicável passando + evidência versionada. Sem evidência, a task volta ao responsável. Nenhuma task abaixo autoriza push, merge, deploy production ou aplicação de migration sem autorização explícita.

## 0. Especificação e design (concluído nesta entrega)

- [x] **T-DOC-1** Criar `specs/009-hermes-runtime/spec.md` com objetivo, escopo, requisitos, critérios de aceite, dependências e `Open Questions` sem pendência bloqueante.
- [x] **T-DOC-2** Criar `specs/009-hermes-runtime/design.md` com arquitetura, lifecycle, lock/heartbeat/recovery, eventos, routing, segurança, observabilidade, ownership de migration e limites de deploy.
- [x] **T-DOC-3** Criar este `tasks.md` com decomposição por perfil, dependências e evidências.
- [x] **T-DOC-4** Registrar evidência de inspeção em `specs/009-hermes-runtime/evidence/inspection.md`.
- [x] **T-DOC-5** Atualizar contrato de payload JSON dos eventos: allowlist por evento, dados proibidos, compatibilidade RPC/migration, cenários de QA e evidência em `specs/009-hermes-runtime/evidence/event-payload-contract-review.md`.

## 1. Banco / runtime SQL - devops

- [ ] **T-DB-1** Confirmar autorização explícita para alterar o Supabase compartilhado do LoLSaas e registrar o aprovador. Sem autorização, bloquear a task.
- [ ] **T-DB-2** Executar probe read-only do catálogo: tabelas `agent_runs`, `agent_execution_locks`, `agent_events`, constraints necessárias e funções `public.hermes_*`. Registrar resultado redigido, sem connection string ou service role key.
- [ ] **T-DB-3** Revisar `sql/001_runtime_functions.sql` do pacote `hermes-agent-runtime` contra o schema real: transações, advisory lock, grants, status permitidos, contrato JSON de payloads por evento e ausência de `SECURITY DEFINER`.
- [ ] **T-DB-4** Aplicar `sql/001_runtime_functions.sql` como migration controlada após autorização. Não aplicar via app startup, Railway deploy ou script automático sem operador.
- [ ] **T-DB-5** Pós-aplicação: confirmar assinaturas e grants das funções `hermes_claim_run`, `hermes_heartbeat_run`, `hermes_finish_run`, `hermes_recover_expired_lock`; `anon`/`authenticated`/`public` sem EXECUTE e `service_role` com EXECUTE.
- [ ] **T-DB-6** Testar concorrência em ambiente seguro: duas tentativas de claim para a mesma issue, uma deve ganhar e a outra deve falhar sem run órfão, preservando evento `lock.rejected` com payload permitido.
- [ ] **T-DB-7** Testar recovery de lock expirado e tentativa tardia de finish do worker antigo; confirmar `lock.expired` e `lock.recovered` com payload JSON não nulo.
- [ ] **T-DB-8** Validar compatibilidade/reaplicabilidade da migration para payloads: eventos sem campos adicionais gravam `{}`; readers usam `coalesce(payload, '{}'::jsonb)` para dados legados; não há alteração que exija secrets ou downtime.

## 2. Orquestrador / integração Linear - orchestrator

- [ ] **T-ORCH-1** Instalar ou referenciar `hermes-agent-runtime` no ambiente server-side do orquestrador. Evidência: `python -c "import hermes_agent_runtime"` no ambiente correto, sem instalar no app web/frontend.
- [ ] **T-ORCH-2** Implementar poller/selector Linear para issues elegíveis: status `Todo`, labels `agent:*`, `execution:*`, `risk:*`, `env:*` conforme `spec.md`; issues inválidas devem bloquear/registrar motivo sem spawn.
- [ ] **T-ORCH-3** Implementar adapter de dispatch: `AgentRuntime.execute(issue, dispatch, move_status=...)` chama o dispatcher Hermes real, preserva `run_id` no contexto/logs/eventos e traduz retorno para `ExecutionResult`.
- [ ] **T-ORCH-4** Implementar callback de status Linear: mover para `In Progress` quando claim aceito e para `In Review`/estado operacional definido após gates; nunca marcar `Done` automaticamente.
- [ ] **T-ORCH-5** Implementar recovery loop: chamar `recover_expired_issue(issue_id)` antes de novo claim e em rotina periódica configurável.
- [ ] **T-ORCH-6** Sanitizar erros persistidos: armazenar tipo/código, nunca mensagem crua com potencial segredo.
- [ ] **T-ORCH-7** Configurar secrets server-side (`SUPABASE_URL`, `SUPABASE_SERVICE_ROLE_KEY`, tokens Linear/GitHub/Railway quando necessários) sem imprimir valores.
- [ ] **T-ORCH-8** Adicionar testes unitários para classificação de labels, bloqueios por política, conflito de claim, gates de qualidade, recovery e sanitização/allowlist de payloads.

## 3. Dispatcher Hermes / Kanban - dev-backend

- [ ] **T-BE-1** Mapear o contrato do dispatcher instalado (`hermes kanban dispatch`, runs, status, exit codes, systemd scope) e documentar como o adapter obtém resultado final sem confundir spawn com sucesso.
- [ ] **T-BE-2** Implementar a tradução Kanban -> `ExecutionResult`: `tests_status`, `review_status`, `commit_sha`, `pull_request_url`, `railway_deployment_id` quando aplicáveis, e eventos Kanban com payload restrito à allowlist da Spec 009.
- [ ] **T-BE-3** Garantir que workers de implementação não completem runtime como `completed` sem gates exigidos; se houver filhos QA/review no Kanban, o parent libera filhos e o runtime aguarda/consulta o resultado final conforme contrato definido.
- [ ] **T-BE-4** Adicionar logs estruturados com `run_id`, `linear_issue_id`, `agent`, `kanban_task_id` e status, sem secrets.
- [ ] **T-BE-5** Testar adapter com store fake e dispatcher fake cobrindo sucesso, falha, bloqueio por gate ausente, perda de lock e rejeição de payload com campo proibido/desconhecido.
- [ ] **T-BE-6** Verificar que nenhuma alteração toca migrations antigas do LoLCoach nem código de produto .NET sem task explícita.

## 4. GitHub / CI / Review - github-profile + code-reviewer

- [ ] **T-GH-1** Definir como `commit_sha` é capturado: SHA de árvore verificada, com status limpo ou dirty state explicitamente rejeitado/registrado.
- [ ] **T-GH-2** Definir contrato para `pull_request_url`: preencher apenas após PR real, sem inventar URL; PR publication exige autorização conforme task.
- [ ] **T-GH-3** Integrar leitura de CI/checks quando houver PR ou commit publicado; falhas viram evento e bloqueio/rework.
- [ ] **T-CR-1** Revisar implementação do poller/adapter/runtime contra `spec.md` e `design.md`, incluindo segurança de secrets e concorrência.
- [ ] **T-CR-2** Reproduzir testes relevantes ou justificar lacunas; emitir parecer `APROVADO`, `REPROVADO` ou `BLOQUEADO` com evidência versionada.

## 5. DevOps / Railway - devops

- [ ] **T-OPS-1** Documentar que `env:production` e deploy Railway exigem autorização humana; nenhuma execução automática deve passar por esse gate por default.
- [ ] **T-OPS-2** Quando houver task autorizada de deploy, adaptar o runtime para apenas registrar `railway_deployment_id` e eventos; execução real pertence ao pipeline/runbook da Spec 008.
- [ ] **T-OPS-3** Garantir que migrations Supabase do runtime e migrations de aplicação LoLCoach não rodem como efeito colateral de deploy.
- [ ] **T-OPS-4** Adicionar runbook de operação: consultar run por issue, lock ativo, heartbeat, timeline de eventos, recovery manual e rotação de secrets.

## 6. QA - qualidade

- [ ] **T-QA-1** Validar classificação de issues: elegíveis, sem agent, agent ambíguo, `execution:human`, `risk:critical`, `env:production`, label desconhecido.
- [ ] **T-QA-2** Validar concorrência: duas execuções simultâneas da mesma issue resultam em uma claim e um conflito sem dispatch do perdedor.
- [ ] **T-QA-3** Validar heartbeat/recovery: heartbeat renova lock; lock expirado é recuperado; finish tardio não sobrescreve resultado.
- [ ] **T-QA-4** Validar gates: backend/frontend/devops não finalizam `completed` sem `tests_status=passed` e `review_status=approved`.
- [ ] **T-QA-5** Validar segurança: nenhum secret em logs/eventos/erros; grants das RPCs restritos a `service_role`; tokens ausentes de artifacts.
- [ ] **T-QA-6** Validar observabilidade: consulta por `run_id`/issue mostra eventos, último heartbeat, final status e campos GitHub/Railway quando existem.
- [ ] **T-QA-7** Validar contrato de payloads: para `run.created`, `lock.acquired`, `agent.dispatched`/`kanban.*`, `tests.completed`, `review.completed`, `run.*`, `lock.rejected`, `lock.expired` e `lock.recovered`, payload é objeto JSON não nulo, contém obrigatórios, não contém campos proibidos e trata payload nulo legado como `{}` na leitura.

## Dependências e ordem

```text
T-DOC-* (feito)
  |
  +--> T-DB-1..T-DB-5 -------------+
  |                                 |
  +--> T-ORCH-1..T-ORCH-2 ----------+--> T-ORCH-3..T-ORCH-8 --> T-BE-1..T-BE-5
  |                                                                   |
  +--> T-GH-1..T-GH-3 -----------------------------------------------+
  +--> T-OPS-1..T-OPS-4 ---------------------------------------------+
                                                                      |
                                                            T-QA-1..T-QA-6
                                                                      |
                                                            T-CR-1..T-CR-2
```

- `T-ORCH-3` depende das RPCs aplicadas (`T-DB-4/T-DB-5`) para teste com Supabase real; antes disso só pode usar store fake.
- `T-BE-2/T-BE-3` dependem do contrato observado do dispatcher (`T-BE-1`) e da forma como o Kanban representa QA/review.
- `T-QA-*` só começa depois de banco, orquestrador, adapter, contrato de payloads e runbook suficientes existirem.
- `T-CR-*` só começa depois de QA ou com lacuna explicitamente bloqueada.

## Evidências a produzir

- `specs/009-hermes-runtime/evidence/db-runtime-functions.md` com probes e aplicação SQL redigidos.
- `specs/009-hermes-runtime/evidence/orchestrator-adapter-verification.md` com testes unitários/smoke do adapter.
- `specs/009-hermes-runtime/evidence/event-payload-contract-review.md` com revisão estática do contrato de payloads, campos proibidos, compatibilidade RPC/migration e cenários QA.
- `specs/009-hermes-runtime/evidence/qa-validation-report.md` com validação dos critérios QA.
- `specs/009-hermes-runtime/evidence/code-review-report.md` com parecer final.
- Runbook operacional citado por `T-OPS-4`.

## Não fazer nesta feature sem nova autorização

- Aplicar migration no banco compartilhado fora da task `devops` responsável por LOL-59.
- Criar ou expor secrets.
- Deploy Railway production.
- Marcar Linear `Done` automaticamente.
- Alterar migrations antigas ou código do produto LoLCoach sem task específica.
- Fazer push, merge, release ou tag.

## Open Questions

Nenhuma. Dependências conhecidas têm owner e ordem de execução definidos.
