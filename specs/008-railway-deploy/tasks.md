# Tasks 008 - Deploy do MVP na Railway

Estado da spec: `DONE` (ver `spec.md` — verificação, pareceres e limitações estão no `Registro de conclusão` da própria spec). A implementação foi integrada em `develop` pela branch `integration/008-spec-deploy` (PRs #24 e #25) e a promoção documental para `DONE` ocorreu em 2026-10-01 no SHA `61b49ab`.

Responsáveis: `dev-backend` (backend), `dev-frontend` (frontend/empacotamento do bundle), `github-profile` (pipeline/DevOps, Dockerfile e documentação operacional), `qualidade` (QA), `code-reviewer` (revisão final). `devops` pode assumir qualquer item marcado com `github-profile` se o perfil estiver disponível no board.

Regra de conclusão herdada do `AGENTS.md`/`ORCHESTRATOR.md`: task marcada **+** código existente **+** build válido **+** testes aplicáveis passando. Sem evidência, a task volta ao responsável.

## 0. Especificação e design (concluído nesta entrega)

- [x] T-DOC-1 Criar `specs/008-railway-deploy/spec.md` com `status: READY`, requisitos testáveis e AC1–AC13.
- [x] T-DOC-2 Criar `specs/008-railway-deploy/design.md` com topologia, alternativas descartadas, configuração, migração, rollback e riscos.
- [x] T-DOC-3 Criar este `tasks.md` com dependências, responsáveis e evidências.

## 1. Backend - dev-backend

- [x] **T-BE-1** `GET /health` (liveness): `AddHealthChecks()` + `MapHealthChecks("/health")` em `Program.cs`, resposta `200` com JSON `{"status":"healthy"}`, sem dependência de banco/Riot/Gemini. Sem pacote novo (API do framework compartilhado). *Aceite:* HostTest com `WebApplicationFactory` retorna 200 e o corpo esperado mesmo com `ConnectionStrings__LoLCoach` sintética inexistente. → `backend/src/LoLCoach.Api/Program.cs:24,125`; teste `Health_returns_healthy_json_without_database_connectivity` (`RuntimeHostTests.cs`), verde.
- [x] **T-BE-2** `GET /health/ready` (readiness): verifica conectividade via `PlayerDbContext.Database.CanConnectAsync()`; `200` quando alcançável, `503` quando não; nunca logar connection string. *Aceite:* teste com banco indisponível resulta em 503 e log sem credencial. → `Program.cs:130-138` + `Infrastructure/DatabaseReadinessHealthCheck.cs`. QA APROVADO na revalidação de `227099e` (bug AC4 — 500 em vez de 503 — corrigido em `6047dc7`). **Deriva registrada (L5 da spec):** `9e7d7b4`/`fa03619` trocaram `CanConnectAsync` por `OpenConnectionAsync` e passaram a registrar a exceção em log; o contrato `503` continua coberto por `Readiness_returns_503_when_database_connection_string_throws`.
- [x] **T-BE-3** Servir o SPA: `UseStaticFiles` + fallback para `index.html` (`MapFallbackToFile`), preservando `404` JSON para `/api/**` inexistente e o comportamento dos endpoints atuais. Dica: um mapeamento explícito de `/api/{**rest}` com ProblemDetails 404 tem precedência sobre o catch-all do fallback (segmento literal `api` é mais específico) — cobrir com teste. *Aceite:* `GET /` e `GET /player/<guid>` → `index.html`; `GET /api/nao-existe` → 404 JSON; `POST /api/players/search` inalterado. → `Program.cs:122,146`; testes `Spa_fallback_serves_index_html_but_api_unknown_route_stays_problem_details` e `Search_endpoint_contract_remains_camel_case_after_spa_fallback`, verdes; smoke HTTP no contêiner em `evidence/ops-local-verification.md`.
- [x] **T-BE-4** Proxy/host: garantir esquema/host corretos atrás do proxy (`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`; se o smoke mostrar esquema `http`, adicionar `UseForwardedHeaders` explícito). *Aceite:* smoke com cabeçalho `X-Forwarded-Proto: https` reflete host/esquema corretos e não abre exceção. → `Program.cs:106-108`; teste `Forwarded_headers_enabled_accepts_proxy_scheme_header`, verde.
- [x] **T-BE-5** Testes: cobrir `/health`, `/health/ready` (com e sem banco), fallback SPA, `/api` inexistente e CORS deny-by-default em ambiente não-Development. Não remover nem ignorar testes existentes. → 8 testes de host em `backend/tests/LoLCoach.Tests/RuntimeHostTests.cs`; suíte completa verde na promoção.
- [x] **T-BE-6** Verificação: `dotnet build`, `dotnet test` e `dotnet format --verify-no-changes` verdes; contrato de `POST /api/players/search`, `POST /{id}/matches/sync` e `GET /{id}/analysis` inalterado; migrações existentes intactas. → `bash backend/scripts/verify.sh` exit 0 no SHA candidato `61b49ab` (build 0 warnings/0 errors; `Failed: 0, Passed: 87, Skipped: 0`; format sem alterações; auditoria de pacotes sem vulnerabilidades; smoke OK) e `git diff f2f9c19..HEAD -- backend/src/LoLCoach.Api/Infrastructure/Migrations/` vazio (AC13).

## 2. Frontend / empacotamento - dev-frontend

- [x] **T-FE-1** Confirmar build de produção: `npm ci && npm run build` gera `frontend/dist/frontend/browser` com `index.html` e assets com hash (`outputHashing: all`), pronto para ser copiado ao `wwwroot`. Registrar tamanho do bundle inicial contra os budgets do `angular.json`. → reexecutado no SHA candidato `61b49ab`: `ng build --configuration production` exit 0, bundle inicial 309,96 kB (83,82 kB de transferência) e lazy chunks `player-search`/`player-dashboard`.
- [x] **T-FE-2** Auditoria de segredo no bundle: `environment.ts`/`environment.prod.ts` mantêm `geminiApiKey` vazio (`''`); nenhuma chave, connection string ou token embutido no bundle; `window.LOLCOACH_API_URL` continua sendo apenas override opcional. *Aceite:* `grep` no bundle não encontra credencial. → `grep` em `frontend/dist/frontend/browser` para `postgres://`, `AIza…` e `GEMINI_API_KEY=` sem ocorrências no SHA candidato.
- [x] **T-FE-3** Validar same-origin: as chamadas usam caminho relativo (`/api/...`) em produção e o deep-link/refresh na rota de cliente `/player/:id` funciona servido pela API (executar após T-BE-3). → `environment.prod.ts` com `apiUrl: ''`; `GET /player/<guid>` → `200 text/html` no QA e no smoke do parecer.
- [x] **T-FE-4** Rodar `npm test` (vitest) e reportar 0 falhas; nenhum teste existente removido ou ignorado. → reexecutado no SHA candidato: `ng test --watch=false` exit 0, 5 arquivos / 30 testes (a evidência original registrava 24 testes em `227099e`; a suíte cresceu com o trabalho de dashboard/LOL-64 sem remoção de testes).

## 3. Pipeline / DevOps - github-profile

- [x] **T-OPS-1** `Dockerfile` na raiz, multi-stage (node 24 → `sdk:10.0` → `aspnet:10.0`), bundle Angular copiado para `wwwroot` antes do `dotnet publish`, `ENTRYPOINT` honrando `PORT` com fallback 8080 (`ASPNETCORE_HTTP_PORTS=${PORT:-8080}`, formato shell para expandir a variável), `EXPOSE 8080`, usuário não-root, sem secrets em `ARG`/`ENV`. *Aceite:* AC1, AC2 e AC16. → `Dockerfile` na raiz, inalterado desde `f2f9c19`; AC1/AC2/AC16 validados no QA e no parecer, com smoke real de `PORT=9999` e fallback 8080.
- [x] **T-OPS-2** `.dockerignore` na raiz excluindo `.git`, `.worktrees`, `node_modules`, `dist`, `bin`, `obj`, `artifacts`, `.angular`, `.env*`. → `.dockerignore` (inalterado desde `f2f9c19`) cobre todos os itens e ainda `.hermes`, `.github`, `specs` e `orchestrator`.
- [x] **T-OPS-3** Substituir o placeholder `.github/workflows/deploy.yml`: `workflow_dispatch` (somente branch `main`) e `release: published`; build e push da imagem no GHCR com tag imutável do SHA; deploy via Railway CLI no GitHub Environment `production` com revisores obrigatórios; nenhum job disparado por `push` em `develop`/`homologacao`; auto-deploy da Railway documentado como desabilitado. *Aceite:* AC9 e revisão estática do workflow. → `deploy.yml` com apenas `workflow_dispatch` + `release: [published]`, guarda de `main`, `::add-mask::` e tag `ghcr.io/${GITHUB_REPOSITORY,,}:${GITHUB_SHA}`; AC9 confirmado no QA e no parecer. Revisores obrigatórios não são suportados no plano atual (L1).
- [x] **T-OPS-4** Criar `.github/workflows/migrate.yml`: `workflow_dispatch` em Environment protegido; gera `dotnet ef migrations script --idempotent` como artefato de revisão e aplica `dotnet ef database update` com `CONNECTIONSTRINGS__LOLCOACH`; sem referência em qualquer passo automático de deploy. *Aceite:* AC10. → `migrate.yml` (jobs `script` e `apply`), somente `workflow_dispatch`, sem referência em `deploy.yml` (AC10).
- [x] **T-OPS-5** Atualizar `.github/workflows/frontend-ci.yml`: remover comentários/guardas obsoletos (o frontend existe), ativar cache npm por `package-lock.json` e garantir execução real de build e testes. Não alterar `backend-ci.yml` nem `python-ci.yml`. → `frontend-ci.yml` sem as guardas antigas, com cache npm e Node 24; `backend-ci.yml` e `python-ci.yml` intocados.
- [x] **T-OPS-6** `specs/008-railway-deploy/runbook.md`: configuração do serviço (build a partir do Dockerfile, porta via `PORT`, `healthcheckPath=/health`, timeout de healthcheck, região, réplicas), tabela de variáveis sem valores, fluxo de IaC (`railway config plan` → aprovação → `railway config apply`), primeiro deploy, execução da migração, verificação pós-deploy, rollback (redeploy da deployment anterior + forward-fix), rotação de secrets, checklist de autorização e registro do SHA publicado. → `runbook.md` cobre todas as seções (AC12) e registra as limitações L1/L2/L4.
- [x] **T-OPS-7** Higiene de secrets: nenhum secret ecoado nos workflows (`::add-mask::`), nenhum `.env` criado/versionado, lista de secrets necessários apenas por nome (`RAILWAY_TOKEN`, `CONNECTIONSTRINGS__LOLCOACH`). *Aceite:* AC7. → AC7 verificado no QA e no parecer e reconferido na promoção: `git grep` sem valores reais, único `.env` rastreado é `frontend/.env.example`, nenhum `.env` versionado.
- [x] **T-OPS-8** Executar `docker build`/`docker run` local para validar o artefato antes de qualquer publicação e anexar as saídas como evidência. → `evidence/ops-local-verification.md` (`docker build` + 29 verificações de smoke no contêiner). Não reexecutado na promoção porque `Dockerfile`/`.dockerignore` são idênticos a `f2f9c19`; a imagem não foi republicada.
- [x] **T-OPS-9** Versionar a configuração da plataforma em `.railway/railway.ts` (IaC): serviço a partir do Dockerfile da raiz, `healthcheck: "/health"`, réplicas e variáveis não secretas por nome; valores secretos permanecem na plataforma. Proibido criar `railway.json`/`railway.toml` (Config as Code depreciado, corte 2026-12-01). *Aceite:* AC14 e AC15. → `.railway/railway.ts` com `healthcheck: "/health"`, `healthcheckTimeout: 300`, `replicas: 1` e `ConnectionStrings__LoLCoach: preserve()`; `railway.json`/`railway.toml` inexistentes e não rastreados (AC15).
- [x] **T-OPS-10** Gate de drift: `railway config plan` (valores redigidos por padrão) registrado no pipeline/runbook como verificação; nenhum uso de `--show-values` em CI/log; `railway config apply` reservado a execução humana autorizada, com `--confirm-destructive` quando houver deleção. *Aceite:* revisão estática + AC14. → fluxo descrito no `runbook.md` e em `.railway/railway.ts`, sem `--show-values` em nenhum arquivo; a primeira execução de `railway config plan` continua pendente por falta de CLI/token (L2).

## 4. QA - qualidade

- [x] **T-QA-1** Validar AC1–AC16 com comandos e saídas reproduzíveis em `specs/008-railway-deploy/evidence/qa-validation-report.md` (formato dos relatórios das specs 002/003/004), incluindo a checagem de IaC (AC14/AC15) e da porta injetada (AC16). → `evidence/qa-validation-report.md` (primeira passada em `05159e6`, revalidação em `227099e`).
- [x] **T-QA-2** Subir a imagem localmente com `ASPNETCORE_ENVIRONMENT=Production` e provar: `/health` 200, `/health/ready` 200/503, SPA em `/` e em rota de cliente, 404 JSON em `/api` inexistente, `/swagger` 404, ausência de `Access-Control-Allow-Origin` com `Origin` não permitido. → evidências de QA, do parecer e `evidence/ops-local-verification.md`, todos com saídas HTTP registradas.
- [x] **T-QA-3** Verificar ausência de segredos no repositório, no bundle e nos logs do contêiner; confirmar que nenhum `.env` está rastreado. → seção de higiene do QA e do parecer; reconferido no SHA candidato (repositório e bundle limpos; logs do contêiner sem credencial).
- [x] **T-QA-4** Confirmar não regressão: `bash backend/scripts/verify.sh` verde e frontend (`npm ci`, build, testes) verde na mesma revisão. → reexecutado no SHA candidato `61b49ab`: verify.sh exit 0 (87/87 testes) e frontend exit 0 (build + 30/30 testes).
- [x] **T-QA-5** Confirmar AC11 (subir apontando para banco vazio não aplica migração e não bloqueia `/health`) e AC13 (migrações existentes intactas frente a `develop`). → AC11 no QA (logs sem migração, `/health` 200) e AC13 confirmado com `git diff f2f9c19..HEAD -- backend/src/LoLCoach.Api/Infrastructure/Migrations/` vazio.

## 5. Code review - code-reviewer

- [x] **T-CR-1** Revisar o diff contra `spec.md`/`design.md`: topologia escolhida respeitada, nenhuma dependência nova injustificada, migrações antigas intocadas, nenhum secret, nenhum deploy executado sem autorização. → `evidence/code-review-report.md` (APROVADO), 0 achados bloqueantes, 3 melhorias recomendadas (MR-1, MR-2, MR-3).
- [x] **T-CR-2** Reproduzir build/testes por conta própria (não aceitar apenas relato do autor) e conferir os workflows de deploy/migração linha a linha quanto a gatilhos indevidos. → reprodução independente em worktree isolado registrada no relatório (build, testes 83/83, format, `verify.sh`, frontend 24/24, `docker build` e smoke HTTP).
- [x] **T-CR-3** Emitir parecer em `specs/008-railway-deploy/evidence/code-review-report.md` (APROVADO / REPROVADO com itens acionáveis). Somente com parecer aprovado a spec passa para `DONE`. → parecer **APROVADO** no SHA `f2f9c19`; a promoção para `DONE` ocorreu em 2026-10-01 com a deriva pós-review registrada como L5 na spec.

## Dependências e ordem

```text
T-DOC-* (feito)
   |
   +--> T-BE-1, T-BE-2, T-BE-5 (health)         --+
   +--> T-BE-3 -------------> T-FE-3, T-BE-5     |
   |        |                                    |
   |        +--> T-OPS-1 (Dockerfile)            |
   |                    |                        |
   +--> T-BE-4, T-BE-6 -+--> T-OPS-2, T-OPS-8    +--> T-QA-1..T-QA-5 --> T-CR-1..T-CR-3 --> DONE
   +--> T-FE-1, T-FE-2, T-FE-4 ------------------+
   +--> T-OPS-3, T-OPS-4, T-OPS-5, T-OPS-9 --> T-OPS-6, T-OPS-7, T-OPS-10 --+
```

- T-OPS-1 depende de `T-BE-3` (a imagem só faz sentido com o SPA servido).
- T-OPS-9 depende de `T-OPS-1` (o serviço aponta para o Dockerfile da raiz) e `T-OPS-10` depende de `T-OPS-9`.
- `T-QA-*` depende de todos os itens de backend, frontend e pipeline marcados.
- `T-CR-*` só começa depois de `T-QA-*` aprovado (regra do `AGENTS.md`).
- Nenhum item desta spec publica, faz push, merge, tag ou deploy: a publicação real exige autorização explícita do usuário e ocorre fora deste plano.

## Evidências produzidas

- `specs/008-railway-deploy/evidence/qa-validation-report.md` (T-QA-*)
- `specs/008-railway-deploy/evidence/code-review-report.md` (T-CR-3, APROVADO em `f2f9c19`)
- `specs/008-railway-deploy/evidence/ops-local-verification.md` (T-OPS-8)
- `specs/008-railway-deploy/runbook.md` (T-OPS-6)
- Saídas de `docker build`/`docker run`, `dotnet test`, `dotnet format`, `npm run build`, `npm test` anexadas aos relatórios; reexecução no SHA candidato `61b49ab` registrada no `Registro de conclusão` de `spec.md`.

## Rastreabilidade final (promoção para DONE — 2026-10-01)

| Item | Valor |
|---|---|
| SHA candidato | `61b49ab72b71aa9075a00be1bb385aee8f5b169f` (= `origin/develop`) |
| Integração | branch `integration/008-spec-deploy`; PRs #24 (`c7a48ad`) e #25 (`7696dab`) |
| Parecer QA | APROVADO (`227099e`), após fix `6047dc7` do bug AC4 |
| Parecer de code review | APROVADO (`f2f9c19`), 0 bloqueantes |
| Artefatos de 008 fora do backend | `Dockerfile`, `.dockerignore`, `.railway/railway.ts`, `runbook.md`, `deploy.yml`, `migrate.yml`, `frontend-ci.yml` e READMEs idênticos a `f2f9c19` |
| Deriva pós-review | `9e7d7b4`/`fa03619` (`DatabaseReadinessHealthCheck.cs`), `b6b9a70` (`production.yml`) — registrada como L5 em `spec.md`; re-review do delta `f2f9c19..61b49ab` recomendado ao `code-reviewer` |
| Publicação | Não executada: push, PR, merge, tag, release e deploy continuam pendentes de autorização explícita do usuário |

## Open Questions

Nenhuma. Ver `spec.md` (decisões do usuário com default adotado e itens fora do escopo).
