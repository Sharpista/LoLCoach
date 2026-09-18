# Evidência DevOps - Spec 008 (T-OPS-1..T-OPS-10)

Relatório do perfil `devops` para o card "Spec 008 — DevOps: empacotamento,
pipeline, IaC e runbook". Não substitui o relatório de QA
(`evidence/qa-validation-report.md`, T-QA-*) nem o parecer de code review
(`evidence/code-review-report.md`, T-CR-*).

## Ambiente e candidato

| Item | Valor |
|---|---|
| Data | 2026-09-18 (CEST, UTC+02:00) |
| Host | Linux 6.8.0-139-generic x86_64 |
| Docker | `29.8.0` |
| Workspace | `/home/alexandre/LoLSaas/.worktrees/spec008-ops` (worktree limpo) |
| Branch | `feat/008-deploy-pipeline` |
| Base | `feat/008-backend-health-spa` @ `370bb375fe54628ccc278659d62ca85e18761e43` (contém `/health`, `/health/ready` e o SPA same-origin) |
| **Commit do artefato** | **`b1237568d22e0273ed6cafa30b51d69ff373ac13`** (`b123756`) |
| Imagem local | `lolcoach:local` = `sha256:a083010d1cc2d50412c6f2aee423920f2fe86b2a579317da4bba1b9ac129482e`, 360 MB |
| Hash do `Dockerfile` avaliado | `8ebf9bc494c439de1b2a86d3deb038dca3c8f1457a57f76182c0998ee0df84cf` (idêntico ao commitado em `b123756`) |

Arquivos entregues (todos no commit `b123756`):

| Arquivo | Task |
|---|---|
| `Dockerfile` | T-OPS-1 |
| `.dockerignore` | T-OPS-2 |
| `.github/workflows/deploy.yml` | T-OPS-3 |
| `.github/workflows/migrate.yml` | T-OPS-4 |
| `.github/workflows/frontend-ci.yml` | T-OPS-5 |
| `specs/008-railway-deploy/runbook.md` | T-OPS-6, T-OPS-10 |
| `.railway/railway.ts` | T-OPS-9 |
| Higiene de secrets nos workflows + runbook | T-OPS-7 |

---

## 1. T-OPS-8 / AC1 - `docker build` real (executado: passou)

Comando (raiz do worktree, BuildKit):

```bash
DOCKER_BUILDKIT=1 docker build --progress=plain -t lolcoach:local .
```

Resultado: **exit code 0**. Estágios executados (log completo em
`/tmp/spec008-ops/docker-build.log`, preservado na estação):

```text
#10 [spa 1/6]     FROM docker.io/library/node:24-alpine@sha256:ebfe2f90...
#15 [spa 2/6]     WORKDIR /src/frontend
#16 [spa 3/6]     COPY frontend/package.json frontend/package-lock.json ./
#17 [spa 4/6]     RUN npm ci --include=dev
#18/#19           COPY frontend/ ./
                  RUN npm run build -- --configuration production
#9  [publish 1/6] FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:2fa828c6...
#14 [publish 3/6] COPY backend/ ./backend/
#20 [publish 4/6] COPY --from=spa /src/frontend/dist/frontend/browser/ ./backend/src/LoLCoach.Api/wwwroot/
#21 [publish 5/6] RUN dotnet restore ... (Restored in 47.71 sec)
#22 [publish 6/6] RUN dotnet publish ... -c Release --no-restore -o /app/publish /p:UseAppHost=false
#11 [runtime 1/3] FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:6a94333d...
#24              exporting manifest list sha256:a083010d1cc2d50412c6f2aee423920f2fe86b2a579317da4bba1b9ac129482e
                 naming to docker.io/library/lolcoach:local
EXIT=0
```

Aviso não bloqueante do `npm ci` (npm 11 exige aprovação explícita de
`install-scripts`): `@parcel/watcher`, `esbuild`, `lmdb`, `msgpackr-extract`. O
build funciona porque `@angular/build` resolve as dependências nativas por
plataforma; nenhum passo falhou.

Camadas finais relevantes (`docker history`):

```text
0B     ENTRYPOINT ["sh" "-c" "export ASPNETCORE_HTTP_PORTS=\"${PORT:-8080}\" && exec dotnet LoLCoach.Api.dll"]
0B     USER app
0B     EXPOSE [8080/tcp]
0B     ENV ASPNETCORE_HTTP_PORTS=8080
14.4MB COPY --chown=app:app /app/publish ./
```

## 2. T-OPS-8 / AC2, AC3, AC5, AC6, AC16 e R1.1/R1.3/R2.1/R3.x/R4.2/R9.1 - `docker run` real (executado: passou)

Script de smoke executado: `/tmp/spec008-ops/smoke_container.sh`
(`ASPNETCORE_ENVIRONMENT=Production`, connection string sintética e inalcançável,
para exercitar o caminho de banco fora sem depender de credencial real).

Exit code: **0** — **PASS=29 FAIL=0**. Saída real, na íntegra:

```text
==============================================================
SPEC 008 - SMOKE DO CONTAINER (T-OPS-8)
data: 2026-09-18T23:26:55+02:00
imagem: sha256:a083010d1cc2d50412c6f2aee423920f2fe86b2a579317da4bba1b9ac129482e 359864579
host: Linux 6.8.0-139-generic x86_64 / docker 29.8.0
==============================================================

--- [1] AC2: conteudo da imagem final (sem Node, sem SDK .NET) ---
$ docker run --rm --entrypoint which lolcoach:local node   # checagem literal do AC2
exit=1
$ docker run --rm --entrypoint which lolcoach:local dotnet  # runtime da base aspnet:10.0 (esperado presente)
/usr/bin/dotnet
exit=0
$ docker run --rm --entrypoint dotnet lolcoach:local --list-sdks   # nenhum SDK instalado
saida=[]
PASS  AC2: sem SDK .NET na imagem (dotnet --list-sdks vazio)     esperado= obtido=
PASS  AC2: node ausente na imagem final                          esperado=AUSENTE obtido=AUSENTE
PASS  AC2: npm ausente na imagem final                           esperado=AUSENTE obtido=AUSENTE
$ docker run --rm --entrypoint id lolcoach:local   # usuario nao-root (R1.3)
uid=1654(app) gid=1654(app) groups=1654(app)
PASS  R1.3: UID nao-root                                         esperado=1654 obtido=1654
PASS  R1.3: usuario app                                          esperado=app obtido=app
conteudo publicado: [/app/LoLCoach.Api.dll
/app/wwwroot/index.html
chunk-A765QQKJ.js
chunk-A765QQKJ.js.br
chunk-A765QQKJ.js.gz
chunk-D4FILXC2.js
chunk-D4FILXC2.js.br]
PASS  R1.1: imagem contem a API publicada                        contem=LoLCoach.Api.dll
PASS  R1.1: imagem contem o index.html do SPA em wwwroot         contem=index.html

--- [2] R2.1/T-OPS-1: ENTRYPOINT honra PORT com fallback 8080 ---
PASS  R2.1: fallback sem PORT                                    esperado=8080 obtido=8080
PASS  R2.1: PORT propagada                                       esperado=9999 obtido=9999

--- [3] subida real (ASPNETCORE_ENVIRONMENT=Production, porta 8080) ---
aguardou 2 segundo(s) ate /health responder
$ docker exec spec008-smoke-8080 cat /proc/1/comm   # PID 1 (prova o exec no ENTRYPOINT)
dotnet
PASS  ENTRYPOINT: PID 1 e o dotnet (recebe SIGTERM no redeploy)  esperado=dotnet obtido=dotnet

--- [4] AC3/AC5/AC6: contrato HTTP em Production ---
PASS  AC3: GET /health status                                    esperado=200 obtido=200
PASS  AC3: GET /health corpo JSON                                esperado={"status":"healthy"} obtido={"status":"healthy"}
PASS  AC3: GET / status                                          esperado=200 obtido=200
PASS  AC3: GET / content-type                                    contem=text/html
PASS  AC3: GET /player/<guid> status                             esperado=200 obtido=200
PASS  AC3: GET /player/<guid> content-type                       contem=text/html
PASS  AC3: GET /api/inexistente status                           esperado=404 obtido=404
PASS  AC3: GET /api/inexistente e JSON (ProblemDetails)          contem=application/problem+json
PASS  AC3: ProblemDetails tem traceId                            contem=traceId
PASS  AC6: /swagger/index.html em Production                     esperado=404 obtido=404
PASS  AC6: /swagger/v1/swagger.json em Production                esperado=404 obtido=404
PASS  AC5: sem Access-Control-Allow-Origin p/ origem nao permitida esperado= obtido=
PASS  R4.2: index.html com base href raiz                        esperado=<base href="/" obtido=<base href="/"
PASS  R3.2: /health/ready com banco inalcancavel                 esperado=503 obtido=503
PASS  R3.1: /health continua 200 com banco fora                  esperado=200 obtido=200

--- [5] AC16: PORT injetada (9999) ---
aguardou 2 segundo(s) na porta 9999
PASS  AC16: /health na porta injetada por PORT                   esperado=200 obtido=200
portas em LISTEN dentro do container (hex->dec): [9999 ]
PASS  AC16: container escutando 9999                             contem=9999
PASS  AC16: container NAO escutando o fallback 8080              esperado= obtido=

--- [6] logs da aplicacao (stdout, sem credencial) ---
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /app
fail: Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService[103]
      Health check postgresql with status Unhealthy completed after 1907.0825ms with message 'PostgreSQL is unreachable.'
PASS  R9.1: log sem a connection string de smoke                 esperado= obtido=

--- [7] limpeza ---
containers removidos: spec008-smoke-8080 spec008-smoke-9999

==============================================================
RESULTADO: PASS=29 FAIL=0
==============================================================
SMOKE_EXIT=0
```

Leitura do resultado por cenário exigido no card:

| Cenário pedido | Resultado | Como foi provado |
|---|---|---|
| `GET /health` → 200 JSON | **passou** | corpo exatamente `{"status":"healthy"}` |
| `GET /` → `text/html` | **passou** | 200 + `content-type: text/html` |
| `GET /player/<guid>` → `text/html` | **passou** | 200 + `text/html` (deep-link do SPA) |
| `GET /api/inexistente` → 404 JSON | **passou** | 404 + `application/problem+json` com `traceId` |
| `/swagger` → 404 | **passou** | `/swagger/index.html` e `/swagger/v1/swagger.json` = 404 |
| Sem `Access-Control-Allow-Origin` p/ `Origin` não permitido | **passou** | header ausente com `Origin: https://evil.example` |
| `PORT` respeitada (`PORT=9999`) | **passou** | `-p 19999:9999` + `/health` 200 em 9999; `/proc/net/tcp` só 9999 |
| Ausência de `node`/`dotnet` na imagem final | **passou com ressalva** | `node`/`npm` ausentes; `dotnet` **presente** (`/usr/bin/dotnet`) — é o runtime da base `aspnet:10.0`, obrigatório para executar a aplicação; **nenhum SDK** instalado (`dotnet --list-sdks` vazio). Ver desvio D2. |

Complementos executados no mesmo smoke (não exigidos pelo card, mas relevantes):
`/health/ready` → 503 com banco inalcançável e `/health` permanecendo 200 (R3.2/R3.1);
`index.html` com `<base href="/">` (R4.2); PID 1 = `dotnet` (o `exec` do ENTRYPOINT
preserva sinais, importante no redeploy); usuário `app` UID 1654 (R1.3); logs em
stdout sem a connection string (R9.1).

## 3. T-OPS-3/4/5/7/10, AC9, AC10, AC15 - verificação estática dos workflows (executado: passou)

Script: `/tmp/spec008-ops/check_workflow_rules.py` (lê os arquivos do commit
`b123756` e remove linhas de comentário antes de procurar padrões, para não
confundir documentação com uso).

```text
PASS  AC9 deploy.yml sem push/pull_request  [triggers: on: workflow_dispatch: release: types: [published] permissions: contents: read]
PASS  AC9 migrate.yml sem push/pull_request  [triggers: workflow_dispatch apenas]
PASS  R7.2 deploy.yml tem workflow_dispatch  [presente]
PASS  R7.2 deploy.yml tem release: published  [types: [published]]
PASS  R7.2 deploy guard restringe dispatch a main  [if: github.ref == 'refs/heads/main']
PASS  R7.3 deploy.yml usa Environment production  [environment.name=production]
PASS  R7.4 auto-deploy documentado como desabilitado  [comentario de cabecalho menciona auto-deploy DESABILITADO]
PASS  R7.5 deploy.yml aplica add-mask no token  [add-mask antes do `railway up`]
PASS  R7.5 migrate.yml aplica add-mask na connection string  [add-mask antes do `database update`]
PASS  R7.5 nenhum secret ecoado (sem echo de valor)  [secrets apenas em env:]
PASS  R6.2 migrate.yml tem workflow_dispatch  [somente dispatch]
PASS  R6.3 migrate.yml publica artefato de revisao  [script idempotente + upload-artifact]
PASS  R6.2/R6.4 migrate.yml aplica forward-only  [database update (sem downgrade)]
PASS  AC10 deploy.yml NAO referencia migrate.yml  [nenhuma ocorrencia]
PASS  T-OPS-5 frontend-ci sem guarda hashFiles  [guardas removidas do codigo do workflow]
PASS  T-OPS-5 frontend-ci com cache npm pelo lockfile  [cache npm + lockfile]
PASS  T-OPS-5 frontend-ci roda build e teste de verdade  [ci -> build -> test]
PASS  AC15 nao existe railway.json/railway.toml  [ausentes na raiz]
PASS  R11.4 T-OPS-10 nenhum --show-values em CI  [ausente dos workflows]
PASS  R11.4 T-OPS-10 proibicao de --show-values documentada no runbook  [runbook proibe explicitamente o flag]
PASS  T-OPS-7 nenhum .env versionado  [.env ausente]

TOTAL=21 PASS=21 FAIL=0
RULES_EXIT=0
```

Observação de método: isto é **análise estática** dos arquivos YAML. Não substitui
a execução real dos workflows no GitHub Actions (ver "não executado" na seção 6).

Validação de sintaxe YAML (`/tmp/spec008-ops/check_yaml.py`, PyYAML `safe_load`):

```text
OK      .github/workflows/deploy.yml: chaves=['name', 'on', 'permissions', 'jobs']
OK      .github/workflows/migrate.yml: chaves=['name', 'on', 'permissions', 'jobs']
OK      .github/workflows/frontend-ci.yml: chaves=['name', 'on', 'jobs']
OK      .github/workflows/backend-ci.yml: chaves=['name', 'on', 'jobs']
OK      .github/workflows/python-ci.yml: chaves=['name', 'on', 'jobs']
RESULTADO: todos os workflows com YAML válido
YAML_EXIT=0
```

## 4. T-OPS-9 / AC14 (parcial), AC15 - IaC verificada contra o SDK real

`railway.json`/`railway.toml` continuam ausentes (AC15 ✓). O `.railway/railway.ts`
não pôde ser avaliado por `railway config plan` (sem CLI/token - ver desvio D1),
mas foi **verificado estaticamente contra os tipos reais do SDK**:

```bash
npm install --no-save railway typescript     # railway 3.11.0 (@railway/iac tipos reais)
npx tsc --noEmit --strict --module esnext --moduleResolution bundler --target es2022 railway.ts
```

```text
TSC_EXIT=0
```

O type-check confirma contra a API publicada: `defineRailway`, `project`, `service`,
`github`, `preserve`, `healthcheck`, `healthcheckTimeout`, `replicas`, `env` e
`build: { builder: "DOCKERFILE", dockerfilePath: "Dockerfile" }`.

Campos declarados: serviço `api`; `source: github("Sharpista/LoLCoach", { branch: "main" })`;
build a partir do `Dockerfile` da raiz; `healthcheck: "/health"`; `healthcheckTimeout: 300`;
`replicas: 1`; `env` com `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED`
(valores não secretos) e `ConnectionStrings__LoLCoach: preserve()` (apenas o nome; o
valor permanece na plataforma).

## 5. Higiene de secrets (T-OPS-7)

| Verificação | Resultado | Evidência |
|---|---|---|
| Nenhum secret em `ARG`/`ENV` do `Dockerfile` | passou | inspeção do `Dockerfile`; única `ENV` é `ASPNETCORE_HTTP_PORTS` |
| `::add-mask::` antes do uso | passou | `check_workflow_rules.py` (deploy e migrate) |
| Nenhum secret ecoado | passou | procura por `echo "${{ secrets.` nos workflows: nenhuma ocorrência |
| Nenhum `.env` criado/versionado | passou | `git status` limpo; `.gitignore` mantém `.env*`; script `T-OPS-7` |
| Secrets citados apenas por nome | passou | `RAILWAY_TOKEN` e `CONNECTIONSTRINGS__LOLCOACH` no runbook (tabela §2.1), sem valores |
| `--show-values` proibido e não usado | passou | ausente dos workflows; proibição explícita no runbook |

## 6. Estado das verificações

**passed** (executado, resultado observado):

- `docker build` da raiz → exit 0 (AC1/T-OPS-1/T-OPS-2/T-OPS-8).
- Smoke do contêiner em `Production` → 29/29 (AC2 com ressalva, AC3, AC5, AC6, AC16,
  R1.1, R1.3, R2.1, R3.1, R3.2, R4.2, R9.1).
- Análise estática dos workflows → 21/21 (AC9, AC10, AC15, R6.2, R6.3, R7.2, R7.4,
  R7.5, R11.4, T-OPS-5, T-OPS-7).
- Sintaxe YAML dos 5 workflows → válida.
- Type-check do `.railway/railway.ts` contra o SDK `railway` 3.11.0 → exit 0.
- `npm install --no-save railway` sem `package.json` na raiz → exit 0 (receita do
  runbook conferida).

**not executed** (motivo explícito):

- `railway config plan` / `railway config apply` / deploy real: Railway CLI não
  instalada e sem token nesta estação; card proíbe executar contra o projeto real.
  É também o desvio D1 (R11.4/AC14 pendente de execução humana autorizada).
- Execução dos workflows no GitHub Actions (deploy/migrate/frontend-ci): exigiria
  push (proibido neste card) e, no caso do deploy, o secret `RAILWAY_TOKEN` que não
  existe. As checagens acima são estáticas.
- `/health/ready` → 200 com banco real e AC11 (contêiner apontando para PostgreSQL
  vazio não aplica migração): exigem banco alcançável; pertencem a T-QA-2/T-QA-5.
  Aqui foi provado apenas o ramo 503 + `/health` 200 (banco inalcançável).
- AC8/AC13 (suíte do backend/frontend e migrações intactas): escopo de QA/review; o
  card proíbe alterar código de aplicação e nada em `Infrastructure/Migrations` foi
  tocado (`git status` limpo, commit contém apenas os 7 arquivos da tabela acima).

**skipped**: nenhum item obrigatório foi pulado.

## 7. Desvios registrados (não silenciados)

| # | Item | Evidência | Impacto | Encaminhamento |
|---|---|---|---|---|
| D1 | **AC14** `railway config plan` não executado | `which railway` → não encontrado; sem token | IaC verificada por tipos, não por plan real; drift não comprovado | Retomada: com CLI autenticada e `railway link` no projeto `e940ef74-8e88-4f71-b816-587840a27480` / environment `b6b44236-a353-474b-8e8e-cbed18ef0ceb`, rodar `railway config plan --detailed-exit-code` e anexar a saída (valores redigidos) |
| D2 | **AC2 - redação literal** | `which dotnet` → `/usr/bin/dotnet` (runtime `.NET`, base `aspnet:10.0`, obrigatório); `dotnet --list-sdks` → vazio; `node`/`npm` ausentes | A checagem literal do AC2 não pode passar sem remover o runtime, o que quebraria a aplicação e contrariaria R1.2 | Ajustar a redação de AC2 para "sem **SDK** .NET e sem Node" (decisão de QA/orquestrador). Não remover o symlink `dotnet` para "passar" na checagem |
| D3 | **R7.3 - revisores obrigatórios no Environment** | `protection_rules: []`; API de required reviewers retorna 403 "Upgrade to GitHub Pro" (repo privado) | O gate de aprovação hoje é o disparo manual + Environment, sem segunda pessoa obrigatória | Decisão do usuário entre aceitar o desvio, migrar de plano ou compensar com gate externo; detalhado no runbook §11 (L1) |

## 8. Riscos e rollback

- **Rollback**: runbook §9 (redeploy da deployment anterior na Railway; migração é
  forward-fix, proibido downgrade destrutivo). Nada foi implantado, então não há
  artefato em produção a reverter neste card.
- **Risco residual 1**: `railway up` faz a Railway reconstruir o `Dockerfile` no
  build da plataforma; a imagem do GHCR fica como artefato imutável de
  rastreabilidade. Documentado no runbook §4.1.
- **Risco residual 2**: o toggle de auto-deploy não é expressável no DSL beta; o
  `apply` não o gerencia e ele precisa ser conferido no painel (runbook §7).
- **Risco residual 3**: nomes de projeto/serviço (`sparkling-dream`, `api`) e do
  serviço já existente devem ser reconciliados antes do primeiro `apply`
  (runbook §3.3).
- **Endurecimento recomendado (não bloqueante)**: fixar as actions por SHA nos
  workflows de produção; hoje usam tag maior (`@v4`) para manter consistência com
  `backend-ci.yml`/`python-ci.yml`.

## 9. Próximo passo

QA (`qualidade`) executar T-QA-1..T-QA-5 sobre o commit `b1237568d22e0273ed6cafa30b51d69ff373ac13`,
incluindo os ramos que exigem banco real (AC4 200, AC11) e a suíte completa (AC8/AC13).
