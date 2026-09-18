# Tasks 008 - Deploy do MVP na Railway

Estado da spec: `READY` (ver `spec.md`). Nenhuma implementação começa antes de o orquestrador confirmar `spec.md`, `design.md`, `tasks.md` e a ausência de Open Questions bloqueantes.

Responsáveis: `dev-backend` (backend), `dev-frontend` (frontend/empacotamento do bundle), `github-profile` (pipeline/DevOps, Dockerfile e documentação operacional), `qualidade` (QA), `code-reviewer` (revisão final). `devops` pode assumir qualquer item marcado com `github-profile` se o perfil estiver disponível no board.

Regra de conclusão herdada do `AGENTS.md`/`ORCHESTRATOR.md`: task marcada **+** código existente **+** build válido **+** testes aplicáveis passando. Sem evidência, a task volta ao responsável.

## 0. Especificação e design (concluído nesta entrega)

- [x] T-DOC-1 Criar `specs/008-railway-deploy/spec.md` com `status: READY`, requisitos testáveis e AC1–AC13.
- [x] T-DOC-2 Criar `specs/008-railway-deploy/design.md` com topologia, alternativas descartadas, configuração, migração, rollback e riscos.
- [x] T-DOC-3 Criar este `tasks.md` com dependências, responsáveis e evidências.

## 1. Backend - dev-backend

- [ ] **T-BE-1** `GET /health` (liveness): `AddHealthChecks()` + `MapHealthChecks("/health")` em `Program.cs`, resposta `200` com JSON `{"status":"healthy"}`, sem dependência de banco/Riot/Gemini. Sem pacote novo (API do framework compartilhado). *Aceite:* HostTest com `WebApplicationFactory` retorna 200 e o corpo esperado mesmo com `ConnectionStrings__LoLCoach` sintética inexistente.
- [ ] **T-BE-2** `GET /health/ready` (readiness): verifica conectividade via `PlayerDbContext.Database.CanConnectAsync()`; `200` quando alcançável, `503` quando não; nunca logar connection string. *Aceite:* teste com banco indisponível resulta em 503 e log sem credencial.
- [ ] **T-BE-3** Servir o SPA: `UseStaticFiles` + fallback para `index.html` (`MapFallbackToFile`), preservando `404` JSON para `/api/**` inexistente e o comportamento dos endpoints atuais. Dica: um mapeamento explícito de `/api/{**rest}` com ProblemDetails 404 tem precedência sobre o catch-all do fallback (segmento literal `api` é mais específico) — cobrir com teste. *Aceite:* `GET /` e `GET /player/<guid>` → `index.html`; `GET /api/nao-existe` → 404 JSON; `POST /api/players/search` inalterado.
- [ ] **T-BE-4** Proxy/host: garantir esquema/host corretos atrás do proxy (`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`; se o smoke mostrar esquema `http`, adicionar `UseForwardedHeaders` explícito). *Aceite:* smoke com cabeçalho `X-Forwarded-Proto: https` reflete host/esquema corretos e não abre exceção.
- [ ] **T-BE-5** Testes: cobrir `/health`, `/health/ready` (com e sem banco), fallback SPA, `/api` inexistente e CORS deny-by-default em ambiente não-Development. Não remover nem ignorar testes existentes.
- [ ] **T-BE-6** Verificação: `dotnet build`, `dotnet test` e `dotnet format --verify-no-changes` verdes; contrato de `POST /api/players/search`, `POST /{id}/matches/sync` e `GET /{id}/analysis` inalterado; migrações existentes intactas.

## 2. Frontend / empacotamento - dev-frontend

- [ ] **T-FE-1** Confirmar build de produção: `npm ci && npm run build` gera `frontend/dist/frontend/browser` com `index.html` e assets com hash (`outputHashing: all`), pronto para ser copiado ao `wwwroot`. Registrar tamanho do bundle inicial contra os budgets do `angular.json`.
- [ ] **T-FE-2** Auditoria de segredo no bundle: `environment.ts`/`environment.prod.ts` mantêm `geminiApiKey` vazio (`''`); nenhuma chave, connection string ou token embutido no bundle; `window.LOLCOACH_API_URL` continua sendo apenas override opcional. *Aceite:* `grep` no bundle não encontra credencial.
- [ ] **T-FE-3** Validar same-origin: as chamadas usam caminho relativo (`/api/...`) em produção e o deep-link/refresh na rota de cliente `/player/:id` funciona servido pela API (executar após T-BE-3).
- [ ] **T-FE-4** Rodar `npm test` (vitest) e reportar 0 falhas; nenhum teste existente removido ou ignorado.

## 3. Pipeline / DevOps - github-profile

- [ ] **T-OPS-1** `Dockerfile` na raiz, multi-stage (node 24 → `sdk:10.0` → `aspnet:10.0`), bundle Angular copiado para `wwwroot` antes do `dotnet publish`, `ENTRYPOINT` honrando `PORT` com fallback 8080 (`ASPNETCORE_HTTP_PORTS=${PORT:-8080}`, formato shell para expandir a variável), `EXPOSE 8080`, usuário não-root, sem secrets em `ARG`/`ENV`. *Aceite:* AC1, AC2 e AC16.
- [ ] **T-OPS-2** `.dockerignore` na raiz excluindo `.git`, `.worktrees`, `node_modules`, `dist`, `bin`, `obj`, `artifacts`, `.angular`, `.env*`.
- [ ] **T-OPS-3** Substituir o placeholder `.github/workflows/deploy.yml`: `workflow_dispatch` (somente branch `main`) e `release: published`; build e push da imagem no GHCR com tag imutável do SHA; deploy via Railway CLI no GitHub Environment `production` com revisores obrigatórios; nenhum job disparado por `push` em `develop`/`homologacao`; auto-deploy da Railway documentado como desabilitado. *Aceite:* AC9 e revisão estática do workflow.
- [ ] **T-OPS-4** Criar `.github/workflows/migrate.yml`: `workflow_dispatch` em Environment protegido; gera `dotnet ef migrations script --idempotent` como artefato de revisão e aplica `dotnet ef database update` com `CONNECTIONSTRINGS__LOLCOACH`; sem referência em qualquer passo automático de deploy. *Aceite:* AC10.
- [ ] **T-OPS-5** Atualizar `.github/workflows/frontend-ci.yml`: remover comentários/guardas obsoletos (o frontend existe), ativar cache npm por `package-lock.json` e garantir execução real de build e testes. Não alterar `backend-ci.yml` nem `python-ci.yml`.
- [ ] **T-OPS-6** `specs/008-railway-deploy/runbook.md`: configuração do serviço (build a partir do Dockerfile, porta via `PORT`, `healthcheckPath=/health`, timeout de healthcheck, região, réplicas), tabela de variáveis sem valores, fluxo de IaC (`railway config plan` → aprovação → `railway config apply`), primeiro deploy, execução da migração, verificação pós-deploy, rollback (redeploy da deployment anterior + forward-fix), rotação de secrets, checklist de autorização e registro do SHA publicado.
- [ ] **T-OPS-7** Higiene de secrets: nenhum secret ecoado nos workflows (`::add-mask::`), nenhum `.env` criado/versionado, lista de secrets necessários apenas por nome (`RAILWAY_TOKEN`, `CONNECTIONSTRINGS__LOLCOACH`). *Aceite:* AC7.
- [ ] **T-OPS-8** Executar `docker build`/`docker run` local para validar o artefato antes de qualquer publicação e anexar as saídas como evidência.
- [ ] **T-OPS-9** Versionar a configuração da plataforma em `.railway/railway.ts` (IaC): serviço a partir do Dockerfile da raiz, `healthcheck: "/health"`, réplicas e variáveis não secretas por nome; valores secretos permanecem na plataforma. Proibido criar `railway.json`/`railway.toml` (Config as Code depreciado, corte 2026-12-01). *Aceite:* AC14 e AC15.
- [ ] **T-OPS-10** Gate de drift: `railway config plan` (valores redigidos por padrão) registrado no pipeline/runbook como verificação; nenhum uso de `--show-values` em CI/log; `railway config apply` reservado a execução humana autorizada, com `--confirm-destructive` quando houver deleção. *Aceite:* revisão estática + AC14.

## 4. QA - qualidade

- [ ] **T-QA-1** Validar AC1–AC16 com comandos e saídas reproduzíveis em `specs/008-railway-deploy/evidence/qa-validation-report.md` (formato dos relatórios das specs 002/003/004), incluindo a checagem de IaC (AC14/AC15) e da porta injetada (AC16).
- [ ] **T-QA-2** Subir a imagem localmente com `ASPNETCORE_ENVIRONMENT=Production` e provar: `/health` 200, `/health/ready` 200/503, SPA em `/` e em rota de cliente, 404 JSON em `/api` inexistente, `/swagger` 404, ausência de `Access-Control-Allow-Origin` com `Origin` não permitido.
- [ ] **T-QA-3** Verificar ausência de segredos no repositório, no bundle e nos logs do contêiner; confirmar que nenhum `.env` está rastreado.
- [ ] **T-QA-4** Confirmar não regressão: `bash backend/scripts/verify.sh` verde e frontend (`npm ci`, build, testes) verde na mesma revisão.
- [ ] **T-QA-5** Confirmar AC11 (subir apontando para banco vazio não aplica migração e não bloqueia `/health`) e AC13 (migrações existentes intactas frente a `develop`).

## 5. Code review - code-reviewer

- [ ] **T-CR-1** Revisar o diff contra `spec.md`/`design.md`: topologia escolhida respeitada, nenhuma dependência nova injustificada, migrações antigas intocadas, nenhum secret, nenhum deploy executado sem autorização.
- [ ] **T-CR-2** Reproduzir build/testes por conta própria (não aceitar apenas relato do autor) e conferir os workflows de deploy/migração linha a linha quanto a gatilhos indevidos.
- [ ] **T-CR-3** Emitir parecer em `specs/008-railway-deploy/evidence/code-review-report.md` (APROVADO / REPROVADO com itens acionáveis). Somente com parecer aprovado a spec passa para `DONE`.

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

## Evidências a produzir

- `specs/008-railway-deploy/evidence/qa-validation-report.md` (T-QA-*)
- `specs/008-railway-deploy/evidence/code-review-report.md` (T-CR-3)
- `specs/008-railway-deploy/runbook.md` (T-OPS-6)
- Saídas de `docker build`/`docker run`, `dotnet test`, `dotnet format`, `npm run build`, `npm test` anexadas aos relatórios.

## Não fazer nesta feature

- Alterar requisitos das specs 001–007, contratos HTTP existentes ou migrações antigas.
- Criar/configurar o projeto Railway real, criar secrets ou escolher plano (decisão do usuário).
- Configurar homologação em nuvem, domínio customizado ou escala horizontal.
- Criar `railway.json`/`railway.toml` (Config as Code depreciado) ou executar `railway config apply` sem autorização.
- Habilitar o LLM (Gemini) em produção.
- Commitar a alteração local de `frontend/angular.json` do checkout principal (pertence ao usuário).

## Open Questions

Nenhuma. Ver `spec.md` (decisões do usuário com default adotado e itens fora do escopo).
