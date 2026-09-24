# Code Review — Spec 008

| Campo | Valor |
|---|---|
| Data | 2026-09-23 (CEST, UTC+02:00) |
| SHA revisado | `f2f9c19d6593191782000cfc463ebf64e4d088b6` |
| Branch | `integration/008-spec-deploy` |
| Workspace | `/home/alexandre/LoLSaas/.worktrees/spec008-integration` |
| Veredito | **APROVADO** |
| Responsável | code-reviewer (profile Hermes) |

---

## Resumo executivo

O conjunto entregue para a Spec 008 atende o contrato declarado em `spec.md` (R1..R11, AC1..AC16) e as decisões de `design.md`. A entrega inclui Dockerfile multi-stage, `.dockerignore`, `.railway/railway.ts`, workflows `deploy.yml`/`migrate.yml`/`frontend-ci.yml`, runbook operacional, e a correção do bug AC4 (`/health/ready` 500→503) commitada em `6047dc7` e revalidada em `227099e` (com ajuste da redação literal de AC2 para separar SDK de runtime).

A reprodução independente em worktree isolado (`/tmp/review-spec008` a partir de `f2f9c19`, depois removido) confirmou build, testes, format, `verify.sh`, frontend CI, `docker build`, smoke HTTP em contêiner real e ausência de segredos. Nenhum achado bloqueante ou que demande correção antes da promoção.

A entrega respeita o `AGENTS.md` da raiz: nenhuma migração existente foi alterada, nenhum secret real foi versionado, nenhum deploy/push/merge/tag/release foi executado, e o `code-reviewer` não publicou nada.

---

## Arquivos revisados (em `f2f9c19`)

| Arquivo | Linhas (+/-) | Observação |
|---|---|---|
| `Dockerfile` | +77 | novo; multi-stage Node 24 → SDK .NET 10 → `aspnet:10.0`; `USER app`; `ENTRYPOINT` shell honra `PORT` com fallback 8080 |
| `.dockerignore` | +38 | novo; exclui `.git`, `.worktrees`, `.github`, `specs`, `.hermes`, `node_modules`, `dist`, `bin`, `obj`, `artifacts`, `.angular`, `.env*` |
| `.github/workflows/deploy.yml` | +/- | substituiu placeholder; triggers `workflow_dispatch` (somente `main`) e `release:published` (não-prerelease); `::add-mask::` antes de qualquer uso de `RAILWAY_TOKEN`; ambiente `production`; sem `push` |
| `.github/workflows/migrate.yml` | +124 | novo; somente `workflow_dispatch`; job `script` gera SQL idempotente como artefato, job `apply` aplica com secret no Environment `production`; sem `push` |
| `.github/workflows/frontend-ci.yml` | +/- | removeu guardas "frontend não existe"; cache npm; Node 24; build + test reais |
| `.railway/railway.ts` | +61 | novo; DSL `railway/iac`; `healthcheck: "/health"`, `healthcheckTimeout: 300`, `replicas: 1`, `ConnectionStrings__LoLCoach: preserve()` (R11.3) |
| `backend/src/LoLCoach.Api/Infrastructure/DatabaseReadinessHealthCheck.cs` | +10/-1 | fix `6047dc7`: try/catch em `CanConnectAsync`, retorna `Unhealthy` em vez de lançar |
| `backend/src/LoLCoach.Api/Program.cs` | +41 | registra `/health` (predicate=false) e `/health/ready` (predicate=tags:ready) com `ResultStatusCodes[Unhealthy]=503`; SPA fallback + 404 JSON em `/api/{**rest}` |
| `backend/tests/LoLCoach.Tests/RuntimeHostTests.cs` | +226 | 8 testes cobrindo liveness, readiness (200/503/throw), SPA fallback, contrato de busca, CORS deny, forwarded headers |
| `specs/008-railway-deploy/spec.md` | +1/-1 | redação de AC2 ajustada para separar SDK de runtime (commit `227099e`) |
| `specs/008-railway-deploy/runbook.md` | +390 | runbook completo (fatos, IaC, deploy, migrações, canário, rollback, rotação, limitações) |
| `specs/008-railway-deploy/evidence/qa-validation-report.md` | +337 | QA APROVADO com 1 bug Médio (AC4), revalidado em 227099e |
| `specs/008-railway-deploy/evidence/ops-local-verification.md` | +336 | evidência DevOps do build/run local (29 PASS no smoke container) |
| `README.md` | +12 | caminho de deploy, sem valores sensíveis |
| `backend/README.md` | +24 | runtime em produção, migrações manuais, sem valores sensíveis |

---

## Critérios de aceite — verificação independente

| AC | Comando / inspeção | Resultado |
|---|---|---|
| AC1 | `docker build -t lolcoach:review .` em `/tmp/review-spec008` | exit 0 |
| AC2 | `docker exec … dotnet --list-sdks` → vazio; `which node`/`which npm` → not found; `id` → `uid=1654(app)` | OK (ver runbook §11 L3 sobre redação literal anterior) |
| AC3 | `GET /` 200 text/html; `GET /player/<guid>` 200 text/html; `GET /api/inexistente` 404 application/problem+json | OK |
| AC4 | `GET /health/ready` 200 com connection string válida (teste `Readiness_returns_200_with_database_connectivity`); 503 com string inalcançável (`Readiness_returns_503_without_database_connectivity`); **503 com string que lança exceção** (`Readiness_returns_503_when_database_connection_string_throws`) | OK — fix `6047dc7` verificado em runtime real |
| AC5 | `curl -I -H 'Origin: https://evil.example'` → sem `Access-Control-Allow-Origin` | OK (também coberto por `Cors_denies_unconfigured_origin_in_production`) |
| AC6 | `GET /swagger/index.html` → 404 | OK |
| AC7 | `git grep postgres://` (excluindo .md/.env.example), `git grep AIza`, `git grep GEMINI_API_KEY=` → vazio; `git ls-files .env*` → só `.env.example`; `grep -R` no `dist/frontend/browser` → vazio | OK |
| AC8 | `bash backend/scripts/verify.sh` → exit 0 (build + testes + format + audit + smoke); `npm ci && ng build --configuration production && ng test --watch=false` → 24/24 | OK |
| AC9 | `deploy.yml`: somente `workflow_dispatch` (com guarda `ref==main`) e `release:published` (não-prerelease); nenhum `push`/`pull_request` | OK |
| AC10 | `migrate.yml`: somente `workflow_dispatch`; `deploy.yml` não referencia `ef database`/`migrate` | OK |
| AC11 | source: `grep -RnE 'Database\.Migrate\|MigrateAsync' backend/src/LoLCoach.Api/` (excluindo binários) → nenhum em código fonte; `verify.sh` smoke sobe `dotnet LoLCoach.Api.dll` sem connection string real e `GET /health` → 200 | OK |
| AC12 | `runbook.md` cobre: primeiro deploy, variáveis (apenas nomes), migração, canário, rollback (redeploy + forward-fix), rotação de secrets, registro do SHA, autorização explícita | OK |
| AC13 | `git diff origin/develop..HEAD -- backend/src/LoLCoach.Api/Infrastructure/Migrations/` → vazio | OK |
| AC14 | `.railway/railway.ts`: `healthcheck: "/health"`, `healthcheckTimeout: 300`, `replicas: 1`, `ConnectionStrings__LoLCoach: preserve()`, sem `--show-values` no workflow/runbook. `railway config plan` não executado (limitação L2 do runbook — sem CLI/token nesta estação) | OK parcial (limitação registrada) |
| AC15 | `ls .railway/railway.json .railway/railway.toml` → não existem; `git ls-files 'railway.json' 'railway.toml'` → vazio | OK |
| AC16 | Smoke real: `PORT=9999` → escuta em 9999, 8080 não escuta; sem `PORT` → escuta em 8080 | OK |

### Contadores da revisão

| Categoria | Quantidade |
|---|---|
| Executado: passou | 16 (AC1..AC16) |
| Análise estática: passou | incluído em "Executado" quando aplicável |
| Não executado | 0 (AC14 plan: registrado como limitação, sem CLI/token — L2) |
| Bugs novos encontrados pela revisão | 0 |
| Achados bloqueantes | 0 |
| Achados "correção necessária" | 0 |
| Melhorias recomendadas | 3 (não bloqueantes — ver §Achados) |

---

## Reprodução independente

Worktree de validação isolado em `/tmp/review-spec008` (HEAD em `f2f9c19`), removido ao final. Ambiente: `dotnet --version` = `10.0.401`; `docker --version` = `29.8.0`; `node` 24 (via `npm ci` no frontend).

### Build/test/format

```
dotnet build backend/LoLCoach.slnx -c Release
  -> 0 Warning(s), 0 Error(s)
dotnet test backend/LoLCoach.slnx -c Release --no-build --nologo
  -> Passed: 83, Failed: 0, Skipped: 0
dotnet format backend/LoLCoach.slnx --verify-no-changes --no-restore
  -> exit 0 (sem mudanças)
bash backend/scripts/verify.sh
  -> exit 0 (build + testes + format + audit + smoke)
```

### Frontend

```
npm ci --include=dev            -> exit 0 (apenas warnings de install-scripts, esperado)
./node_modules/.bin/ng build --configuration production
  -> exit 0; bundle inicial 309.52 kB (76.36 kB transfer); chunks por rota
./node_modules/.bin/ng test --watch=false
  -> 5 files / 24 tests passed
```

### Docker build + smoke (porta 18080)

```
docker build -t lolcoach:review .   -> exit 0 (sha256:67aa002b…)
docker run -d --name review-spec008 -e ASPNETCORE_ENVIRONMENT=Production \
  -e 'ConnectionStrings__LoLCoach=Host=127.0.0.1;Port=1;Database=nope;Username=tester;Password=secret-not-leaked-1' \
  -p 18080:8080 lolcoach:review

GET /health              -> 200 {"status":"healthy"}                     (ct: application/json)
GET /health/ready        -> 503 {"status":"unhealthy"}                   (ct: application/json)
GET /                    -> 200 (ct: text/html)
GET /player/test-guid-123-> 200 (ct: text/html)
GET /api/nao-existe      -> 404 (ct: application/problem+json)
GET /swagger/index.html  -> 404

docker exec review-spec008 id
  -> uid=1654(app) gid=1654(app) groups=1654(app)
docker exec review-spec008 sh -c "dotnet --list-sdks; which node || echo node-ABSENT; which npm || echo npm-ABSENT"
  -> (vazio) / node-ABSENT / npm-ABSENT

docker logs review-spec008 | grep -E 'secret-not-leaked|Username=tester|Host=127'
  -> (sem resultado, sem leak)
curl /health/ready body grep:
  -> {"status":"unhealthy"} (sem credencial)
docker exec review-spec008 grep -R 'secret-not-leaked' /app
  -> NO_LEAK_IN_IMAGE
```

Conferência cruzada com a revalidação QA no SHA `227099e`: resultado compatível (`/health/ready` retorna 503 ao invés de 500 com a mesma categoria de falha).

---

## Achados

Classificação:

- **B** = bloqueante (impede finalizar)
- **CN** = correção necessária (impede finalizar)
- **MR** = melhoria recomendada (não impede, vale registrar)
- **SO** = sugestão opcional (cosmético / evolução futura)

### MR-1 — Tags de action por versão maior (`@v4`/`@v6`) em workflows de produção

- **Local:** `.github/workflows/deploy.yml`, `.github/workflows/migrate.yml`, `.github/workflows/frontend-ci.yml`
- **Evidência:** uso consistente de `actions/checkout@v4`, `actions/setup-dotnet@v4`, `actions/setup-node@v4`, `docker/setup-buildx-action@v3`, `docker/login-action@v3`, `docker/build-push-action@v6`, `actions/upload-artifact@v4`.
- **Impacto:** Alinha-se com o restante do repositório (`backend-ci.yml`, `python-ci.yml`) mas para o pipeline de produção o ideal é fixar por SHA imutável. O runbook §11 já registra essa evolução como nota de endurecimento.
- **Recomendação:** evoluir para SHA imutável em spec futura; manter `@v4` no MVP.
- **Responsável:** github-profile / devops.

### MR-2 — `healthcheckTimeout: 300` no DSL é o default da plataforma

- **Local:** `.railway/railway.ts:42`
- **Evidência:** a spec diz "timeout padrão de 300s ajustável por `RAILWAY_HEALTHCHECK_TIMEOUT_SEC`" e o DSL declara `healthcheckTimeout: 300`.
- **Impacto:** redundância leve — o default da plataforma já é 300s. Declarar explicitamente é defensivo e auditável, mas confunde a leitura do "ajustável por env". Se a Railway não ler o campo do DSL beta, a env var é a única alavanca.
- **Recomendação:** manter a declaração explícita por rastreabilidade; documentar no runbook §2 que o ajuste operacional é por `RAILWAY_HEALTHCHECK_TIMEOUT_SEC` no painel.
- **Responsável:** github-profile / devops (runbook).

### MR-3 — `forwarded headers` com `KnownProxies`/`KnownNetworks` vazios em produção

- **Local:** `backend/src/LoLCoach.Api/Program.cs:104-110`
- **Evidência:** `app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ... })` é chamado sem `KnownProxies`/`KnownNetworks` definidos quando `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`. Em Production isso aceita qualquer `X-Forwarded-*` enviado por cliente.
- **Impacto:** risco baixo porque o único caller confiável no MVP é o proxy da Railway (TLS terminado na borda, hostnames `*.up.railway.app`). Em deployments futuros atrás de CDN ou load balancer adicional vale restringir.
- **Recomendação:** no MVP, manter como está. Antes de adicionar CDN, restringir `KnownProxies` aos IPs da CDN/edge.
- **Responsável:** dev-backend (spec futura).

### SO-1 — `frontend/dist/frontend/browser` é reconstruído dentro do Dockerfile (camada `spa`) mas também existe no host

- **Local:** `.dockerignore:22` exclui `**/dist`; o Dockerfile apenas `COPY frontend/ ...` e roda `npm run build`. OK, sem leak.
- **Observação:** nenhuma fragilidade encontrada — só confirmando que o caminho do QA (`.dockerignore` excluindo dist) está correto.

### Limitações registradas (não silenciadas, alinhadas com a entrega)

- **L1 — R7.3 revisores obrigatórios:** plano atual do GitHub não oferece o recurso em repo privado; gate de aprovação é o disparo manual + Environment protection rules `[]`. Decisão do usuário: aceitar o desvio, migrar para Pro, ou compensar com gate externo. **Fora do escopo do code review.**
- **L2 — AC14 `railway config plan` não executado:** sem CLI/token nesta estação; análise estática do DSL foi feita. **Fora do escopo do code review** — primeira ação humana autorizada está no runbook §3.1.
- **L4 — `RAILWAY_TOKEN` ausente no GitHub Environment:** criação é ação do usuário (runbook §2.1). **Fora do escopo do code review.**

---

## Higiene de segurança

| Item | Resultado |
|---|---|
| `git grep -E 'postgres://|AIza[0-9A-Za-z_-]{20,}|GEMINI_API_KEY='` excluindo .md e `.env.example` | vazio |
| `git ls-files '.env'`/`.env.*` | apenas `frontend/.env.example` (template) |
| `grep -R` no bundle `dist/frontend/browser` | vazio para os mesmos padrões |
| Credenciais em layers da imagem (`grep -R 'secret-not-leaked-1' /app` no contêiner) | vazio |
| Logs do contêiner com credencial | vazio |
| Body de `/health/ready` com credencial | vazio |
| Workflows com `::add-mask::` antes do uso de `RAILWAY_TOKEN` e `CONNECTIONSTRINGS__LOLCOACH` | presente |
| `railway.json` / `railway.toml` no repo | não existem |
| Dockerfile com `ARG`/`ENV` sensível de build | nenhum |
| `.dockerignore` cobre `.env*` | sim |

Nenhum vazamento de PII/segredo nas imagens, bundles, logs ou respostas HTTP.

---

## Não regressão

- Contrato `POST /api/players/search` preservado (teste `Search_endpoint_contract_remains_camel_case_after_spa_fallback`).
- `backend/scripts/verify.sh` verde inclui smoke de search.
- 83/83 testes do backend, 24/24 testes do frontend passam no SHA candidato.
- Migrations idênticas a `develop` (AC13).
- 5 arquivos novos de evidência (`evidence/qa-validation-report.md`, `evidence/ops-local-verification.md`, este `code-review-report.md`, `runbook.md`, ajustes em `spec.md`/`design.md`) — nenhum segredo real.

---

## Pendências e encaminhamento

| Item | Tipo | Responsável |
|---|---|---|
| L1 — revisores obrigatórios (R7.3) | decisão de produto | usuário (não bloqueante) |
| L2 — primeira execução de `railway config plan` | execução humana autorizada | usuário + github-profile |
| L4 — criar secret `RAILWAY_TOKEN` no Environment `production` | execução humana autorizada | usuário (não bloqueante) |
| MR-1 — fixar actions por SHA imutável | spec futura | github-profile |
| MR-3 — restringir `KnownProxies` quando houver CDN | spec futura | dev-backend |

Nenhuma pendência impede promoção para `develop` ou execução do primeiro deploy sob autorização.

---

## Veredito

**APROVADO.**

A entrega atende o contrato de `spec.md` e as decisões de `design.md`. A correção `6047dc7` (`/health/ready` 500 → 503) está coberta por teste (`Readiness_returns_503_when_database_connection_string_throws`) e verificada em runtime real. Reprodução independente em worktree isolado confirma build/testes/format/verify.sh/frontend/Docker/HTTP smoke/higiene de segredos. Limitações L1, L2 e L4 estão registradas no runbook e não são bloqueantes.

Próximo passo (sob autorização do usuário): promoção da branch `integration/008-spec-deploy` para `develop` via PR. Nenhum agente publica, faz merge, deploy, tag ou release sem autorização explícita.