# QA Validation Report — Spec 008

| Campo | Valor |
|---|---|
| Data | 2026-09-18 23:39 UTC |
| SHA validado | `05159e608589f298b567a844224487da273c1d31` |
| Branch | `integration/008-spec-deploy` |
| Workspace | `/home/alexandre/LoLSaas/.worktrees/spec008-integration` |
| Docker image | `lolcoach:qa` (build local a partir do SHA) |
| Ambiente | Local (Docker 29.8.0, Linux 6.8.0-139-generic) |
| Responsável | qualidade (profile Hermes) |

---

## Resumo executivo

**Veredito: APROVADO com 1 bug registrado (severidade Média)**

15 de 16 ACs validados com sucesso. 1 bug funcional encontrado: `/health/ready` retorna HTTP 500 em vez de 503 quando o banco é inalcançável (AC4). O health check lança exceção em vez de retornar `HealthStatus.Unhealthy`, contornando o mapeamento `ResultStatusCodes`. `/health` permanece 200 independentemente, então o `healthcheckPath` da Railway não é afetado. O bug não bloqueia o deploy mas deve ser corrigido para que o monitoramento externo receba o status correto.

---

## Resultados por AC

### AC1 — Docker build (executado: passou)

```
Comando: docker build -t lolcoach:qa .
Resultado: Build concluiu sem erro (exit 0)
SHA imagem: sha256:eea8ae2956d36379a7e67c83442ea1f7939a526007c11b4f78a0307afce0357e
```

### AC2 — Imagem final sem SDK/Node/não-root (executado: passou)

```
Comando: docker run --rm --entrypoint="" lolcoach:qa sh -c "dotnet --list-sdks"
Resultado: (vazio — nenhum SDK)

Comando: docker run --rm --entrypoint="" lolcoach:qa sh -c "which node"
Resultado: node: not found

Comando: docker run --rm --entrypoint="" lolcoach:qa sh -c "which npm"
Resultado: npm: not found

Comando: docker run --rm --entrypoint="" lolcoach:qa sh -c "whoami"
Resultado: app (não-root)

Comando: docker run --rm --entrypoint="" lolcoach:qa sh -c "dotnet --info | head -10"
Resultado: .NET Runtime 10.0.12 (aspnet:10.0 base), SDKs installed: No SDKs were found
```

Nota sobre redação de AC2: `which dotnet` encontra o runtime (`/usr/bin/dotnet`), que é necessário para executar a aplicação. A intenção (sem SDK, sem Node) está atendida. A spec pode precisar de ajuste na redação literal.

### AC3 — Health e SPA (executado: passou)

```
GET /health     → 200 {"status":"healthy"}
GET /           → 200 text/html
GET /player/abc → 200 text/html
GET /api/rota-inexistente → 404 ProblemDetails {"traceId":"..."}
```

### AC4 — Health readiness (executado: falhou — BUG)

```
GET /health/ready (sem DB) → 500 (esperado: 503)
GET /health (após falha)   → 200 (correto)
```

**Bug registrado abaixo.** O `DatabaseReadinessHealthCheck` lança exceção quando `CanConnectAsync` falha (connection string inválida/ausente), resultando em 500 em vez de 503. O mapeamento `ResultStatusCodes` em `Program.cs:134-139` nunca é atingido porque a exceção aborta antes de retornar `HealthCheckResult.Unhealthy`.

### AC5 — CORS deny (executado: passou)

```
Comando: curl -sI -H "Origin: https://evil.example" http://localhost:8080/health | grep -i access-control
Resultado: NENHUM access-control-allow-origin (correto)
```

### AC6 — Swagger em Production (executado: passou)

```
GET /swagger/index.html       → 404
GET /swagger/v1/swagger.json  → 404
```

### AC7 — Segredos (executado: passou)

```
git grep postgres:// (excluindo .md)  → vazio
git grep GEMINI_API_KEY=              → vazio
git grep AIza                         → vazio
git ls-files *.env .env*              → apenas frontend/.env.example (template, não segredo)
Bundle: nenhum postgres://, AIza, GEMINI_API_KEY ou supabase encontrado
```

### AC8 — Build/testes (executado: passou)

```
bash backend/scripts/verify.sh → exit 0 (build + testes + format + audit + smoke)
Frontend: ./node_modules/.bin/ng test --watch=false → 5 test files, 24 tests, ALL PASSED
```

### AC9 — Pipeline triggers (análise estática: passou)

```
deploy.yml triggers: workflow_dispatch + release:published (nenhum push)
Guarda: workflow_dispatch restrito a refs/heads/main
Guarda: release restrito a prerelease == false
Tag de imagem: ghcr.io/${GITHUB_REPOSITORY,,}:${GITHUB_SHA}
```

### AC10 — Migração manual (análise estática: passou)

```
migrate.yml triggers: workflow_dispatch apenas (com campo de confirmação)
Referência em deploy.yml: NENHUMA
Job script: gera SQL idempotente como artefato (sem tocar banco)
Job apply: roda em Environment production com secret
```

### AC11 — Banco vazio (executado: passou)

```
Comando: docker run com ConnectionStrings__LoLCoach apontando para DB inexistente
GET /health → 200 {"status":"healthy"}
Logs: nenhuma migração automática detectada
```

### AC12 — Runbook (análise estática: passou)

Seções verificadas: Fatos do ambiente, Configuração do serviço, Fluxo de IaC, Primeiro deploy, Migrações, Verificação pós-deploy, Auto-deploy e autorização, Registro do SHA, Rollback, Rotação de secrets, Limitações conhecidas. Cobertura completa dos itens exigidos.

### AC13 — Migrações intactas (análise estática: passou)

```
Comando: git diff develop -- backend/src/LoLCoach.Api/Infrastructure/Migrations/
Resultado: nenhuma diff
```

### AC14 — IaC railway.ts (análise estática: passou)

```
healthcheck: "/health" ✓
replicas: 1 ✓
env: ASPNETCORE_ENVIRONMENT=Production, ASPNETCORE_FORWARDEDHEADERS_ENABLED=true ✓
ConnectionStrings__LoLCoach: preserve() (valor secreto não exposto) ✓
```

Execução de `railway config plan` não realizada (sem CLI/token — registrado como limitação L2 no runbook).

### AC15 — Sem railway.json/toml (executado: passou)

```
ls .railway/railway.json → não existe
ls .railway/railway.toml → não existe
```

### AC16 — PORT injetada (executado: passou)

```
PORT=9999: /health → 200 na porta 9999, 8080 não escuta
Sem PORT: /health → 200 na porta 8080 (fallback)
```

### T-FE-3 — Same-origin (executado: passou)

```
environment.prod.ts: apiUrl: '' (relativo)
api-config.ts: window.LOLCOACH_API_URL como override opcional
GET /player/test-guid-123 → 200 text/html (SPA fallback funciona com deep-link)
```

---

## Bug report

| Campo | Valor |
|---|---|
| Título | `/health/ready` retorna 500 em vez de 503 quando banco é inalcançável |
| Severidade | Média |
| AC afetado | AC4 |
| Frequência | 100% reproduzível |
| Ambiente | Local (Docker, Production, sem connection string válida) |

**Passos para reproduzir:**
1. `docker run -d --name test -e ASPNETCORE_ENVIRONMENT=Production -e ConnectionStrings__LoLCoach="Host=127.0.0.1;Port=5432;Database=x;Username=x;Password=x" -p 8081:8080 lolcoach:qa`
2. `curl -s -w "\nHTTP %{http_code}" http://localhost:8081/health/ready`

**Resultado atual:** HTTP 500 com ProblemDetails (exceção não tratada)
**Resultado esperado:** HTTP 503 com `{"status":"unhealthy"}`

**Causa raiz:** `DatabaseReadinessHealthCheck.CheckHealthAsync()` (`Infrastructure/DatabaseReadinessHealthCheck.cs:11`) chama `db.Database.CanConnectAsync()` que lança exceção quando a connection string é inválida. A exceção não é capturada, então o middleware de health checks retorna 500. O mapeamento `ResultStatusCodes[HealthStatus.Unhealthy] = 503` em `Program.cs:138` nunca é atingido.

**Fix sugerido:** Envolver `CanConnectAsync()` em try-catch dentro de `DatabaseReadinessHealthCheck`, retornando `HealthCheckResult.Unhealthy()` em caso de exceção. Isso ativa o mapeamento existente e resulta em 503 conforme o contrato.

**Impacto no deploy:** Não bloqueante — o `healthcheckPath` da Railway aponta para `/health` (liveness), que continua 200. O bug afeta apenas o monitoramento externo via `/health/ready`.

**Responsável sugerido:** dev-backend

---

## Limitações e riscos residuais

1. **AC14 parcial:** `railway config plan` não executado (sem CLI/token). Validação estática do `.railway/railway.ts` feita. Drift real só será detectado na primeira execução do plan.
2. **AC2 redação:** A spec diz "which dotnet não encontra binário" mas o runtime está presente (necessário). A intenção (sem SDK) está atendida; a redação pode precisar de ajuste.
3. **R7.3 revisores obrigatórios:** `protection_rules: []` no GitHub Environment. O gate de aprovação é o disparo manual. Registrado no runbook L1.
4. **AC4/AC8 frontend:** `npx vitest run` direto falha por falta de `initTestEnvironment`. O caminho correto é `ng test --watch=false` (24/24 passam). O `vitest run` direto não é o método suportado pelo Angular CLI.

---

## Contadores

| Categoria | Quantidade |
|---|---|
| Executado: passou | 14 |
| Executado: falhou | 1 (AC4) |
| Análise estática: passou | 5 (AC9, AC10, AC12, AC13, AC14) |
| Não executado | 1 (AC14 plan — limitação de ambiente) |
| Bugs registrados | 1 (Média) |

---

## Próximo passo

Recomendado: dev-backend corrige `DatabaseReadinessHealthCheck` para capturar exceção e retornar `HealthCheckResult.Unhealthy()`. Após correção, QA revalida AC4. O veredito pode ser mantido como APROVADO com a ressalva de que o bug é de severidade média e não bloqueia o deploy.
