# Design 008 - Deploy do MVP na Railway

Complementa `spec.md` (contrato) e `tasks.md` (execução). Decisões aqui são vinculantes para a implementação: cada escolha traz a justificativa e a alternativa descartada.

## Contexto verificado

| Fato | Evidência |
|---|---|
| API .NET 10 única, sem `wwwroot` e sem arquivos estáticos | `backend/src/LoLCoach.Api/` (sem diretório `wwwroot`); ausência de `UseStaticFiles`/`MapFallbackToFile` no código |
| Frontend Angular 22 compila para `dist/frontend/browser` | `frontend/angular.json` (`@angular/build:application`, `outputHashing: all`) e build existente em `frontend/dist/frontend/browser` |
| Produção usa caminho relativo | `frontend/src/environments/environment.prod.ts` (`apiUrl: ''`) e `api-config.ts` (`window.LOLCOACH_API_URL` como override) |
| CORS nega por padrão fora de Development | `Program.cs` (política `AllowedOrigins`, `SetIsOriginAllowed(_ => false)` quando não há origens) |
| Persistência é PostgreSQL Supabase via pooler | `backend/SUPABASE.md`; `Program.cs` lê `ConnectionStrings:LoLCoach` (Npgsql) |
| Startup não migra nem abre conexão | `backend/README.md` ("O startup não migra nem abre conexão automaticamente"); `Program.cs` não chama `Migrate()` |
| Sem health check e sem Dockerfile | busca no repositório não encontra `/health` em código nem `Dockerfile`/`.dockerignore` |
| `deploy.yml` é placeholder sem alvo | `.github/workflows/deploy.yml` (comentários "TARGET PENDENTE DE DECISÃO") |

## Decisão A - Topologia: um único serviço Railway, uma única imagem

**Escolhido:** uma imagem Docker multi-stage (Node + SDK .NET no build; runtime `aspnet:10.0`) contendo a API **e** o bundle Angular, publicada como **um** serviço Railway (`api`), com o SPA servido pela própria API na mesma origem. Supabase permanece como Postgres externo gerenciado. Uma réplica.

**Justificativa:**

1. O frontend já foi projetado para mesma origem: `environment.prod.ts` define `apiUrl: ''` (relativo) justamente prevendo o serviço atrás do mesmo domínio. Um segundo serviço introduziria CORS de produção, cookies/headers adicionais e configuração de runtime (`window.LOLCOACH_API_URL`) sem ganho para um MVP.
2. CORS sai do caminho crítico: a política deny-by-default existente continua válida e testável (R5), sem precisar liberar origem pública.
3. Um único artefato imutável (imagem por SHA) simplifica deploy, rollback, healthcheck e diagnóstico: o que foi testado é exatamente o que sobe.
4. Menos superfície de secrets: um serviço, um conjunto de variáveis.
5. Volume do MVP (projeto pessoal) não exige escala independente de frontend e backend.

**Alternativas descartadas:**

| Alternativa | Por que não |
|---|---|
| Dois serviços Railway (API + site estático) | Exige CORS com origem pública, configuração de runtime no bundle e dois rollbacks independentes; sem benefício no MVP. Vira decisão futura registrada em "Fora do escopo". |
| Container nginx reverso-proxy na frente da API | Duplica camada de proxy que a Railway já oferece (TLS, roteamento, domínio), aumentando imagem e modos de falha. |
| Build estático Angular separado (`@angular/build` + nginx) | Mesmo problema de topologia dupla; o objetivo do MVP é mínimo de peças móveis. |

## Decisão B - Empacotamento da imagem

```text
estágio 1 (node:24-alpine)          estágio 2 (sdk:10.0)                  estágio 3 (aspnet:10.0)
  frontend/ npm ci                    copia bundle do estágio 1            copia publish do estágio 2
  npm run build --configuration       para src/LoLCoach.Api/wwwroot         ENV ASPNETCORE_HTTP_PORTS=8080
    production (sem --base-href)      dotnet publish -c Release            EXPOSE 8080, USER app (não-root)
  -> dist/frontend/browser            -> /app/publish                      ENTRYPOINT dotnet LoLCoach.Api.dll
```

- O bundle vai para `wwwroot` **antes** do `dotnet publish`, então o Web SDK o inclui no publish sem `<Content Include>` extra.
- `base href` permanece `/` (`frontend/src/index.html`), coerente com o serviço na raiz do domínio. Rotas de cliente atuais: `/` (busca) e `/player/:id` (dashboard).
- Node e SDK existem apenas nos estágios de build — a imagem publicada não os contém (AC2).
- Build não recebe secrets: nenhuma variável sensível em `ARG`/`ENV` de build (R1.4). `environment.prod.ts` tem `geminiApiKey: ''` (vazio, verificado); o bundle não pode ganhar chave em nenhuma fase.

## Decisão C - Runtime, porta e proxy

| Item | Valor | Motivo |
|---|---|---|
| Porta | `PORT` injetada pela Railway, fallback `8080`: `ASPNETCORE_HTTP_PORTS=${PORT:-8080}` no `ENTRYPOINT` | A doc da Railway informa que a `PORT` injetada é usada tanto para roteamento quanto para o healthcheck; ignorá-la exige declarar um `PORT` manual e pode resultar em `service unavailable` no healthcheck. O fallback 8080 mantém `docker run -p 8080:8080` previsível localmente |
| Ambiente | `ASPNETCORE_ENVIRONMENT=Production` | Desliga Swagger por default e ativa a política CORS restritiva |
| Cabeçalhos de proxy | `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` | TLS termina na borda da Railway; sem isso esquema/host vistos pela app são `http`/interno |
| Hosts | `AllowedHosts: "*"` mantido | Domínio `*.up.railway.app` e domínio futuro não exigem alteração de código |

Se o host não honrar `ASPNETCORE_FORWARDEDHEADERS_ENABLED` no smoke test, a task T-BE-4 habilita `UseForwardedHeaders` explicitamente com `XForwardedFor|XForwardedProto`. A escolha é verificável (AC3/AC4 não dependem disso; o smoke da task cobre o esquema).

## Decisão D - Health checks

| Rota | Uso | Comportamento |
|---|---|---|
| `/health` | `healthcheckPath` da Railway | `200` + `{"status":"healthy"}`; sem DB, Riot ou Gemini; nunca reinicia por banco fora |
| `/health/ready` | monitoramento externo / runbook | `200` quando `PlayerDbContext.Database.CanConnectAsync()` responde; `503` caso contrário |

- HealthChecks estão no framework compartilhado `Microsoft.AspNetCore.App` (aparecem na lista `packagesToPrune` do `project.assets.json` gerado pelo SDK 10.0.401): **nenhum pacote novo** é adicionado.
- Semântica da plataforma (doc oficial, `docs.railway.com/deployments/healthchecks`): a Railway consulta o `healthcheckPath` **apenas na subida de um deploy**, com timeout padrão de 300s (ajustável por `RAILWAY_HEALTHCHECK_TIMEOUT_SEC`), e as requisições vêm do host `healthcheck.railway.app` — por isso `AllowedHosts: "*"` é mantido (R3.5) e por isso a visão de saúde contínua precisa de monitor externo batendo em `/health/ready`.
- Separar *liveness* de *readiness* evita o modo de falha clássico de restart loop quando o pooler Supabase oscila: a plataforma só julga `/health`, e só durante o deploy.
- `/health/ready` não pode logar a connection string (R3.4); a falha é logada apenas com categoria/erro genérico.

## Decisão E - Migrações controladas

**Escolhido:** aplicação **fora do runtime**, por workflow manual `migrate.yml` (`workflow_dispatch`) em GitHub Environment protegido, com o secret já existente `CONNECTIONSTRINGS__LOLCOACH`.

```text
workflow_dispatch (main)
   -> dotnet ef migrations script --idempotent   (artefato de revisão, sem tocar produção)
   -> aprovação no Environment "production"
   -> dotnet ef database update                  (aplica somente o que falta)
```

- Nunca no startup do contêiner e nunca como passo automático do deploy: mantém a decisão já existente de que o startup não migra (`backend/README.md`) e evita que um deploy suba código antes do schema.
- *Expand/contract*: migração compatível com a versão anterior permite rollback de aplicação sem rollback de banco (R8).
- Migrações antigas são imutáveis (AC13); correção de migração errada é sempre nova migração.
- Alternativa descartada: *pre-deploy command* da Railway executando `dotnet ef database update` na imagem de runtime — exigiria a ferramenta EF e a factory design-time na imagem publicada, ampliando dependências e superfície de credenciais no runtime.

## Decisão F - Pipeline e autorização

```text
GitHub                                                      Railway (env production)
  main + workflow_dispatch | release published                 serviço "api"
    job build (ambas plataformas)                                imagem ghcr.io/...:<sha>
      -> docker build (Dockerfile raiz)                          porta 8080
      -> push GHCR (tag = sha imutável)                          healthcheckPath /health
    job deploy (Environment "production", aprovação)             domínio *.up.railway.app
      -> railway up --service api --environment production
                                                                       |
                                                                       | pooler 6543 (TLS)
                                                                       v
                                                        Supabase PostgreSQL 17 (us-east-1)
```

- `workflow_dispatch` restrito a `main` + `release: published`: nenhum `push` publica (AC9).
- Environment `production` com revisores obrigatórios materializa "nunca deploy sem autorização explícita".
- Auto-deploy da integração GitHub da Railway permanece **desabilitado**; o único gatilho é o workflow.
- Segredos: `RAILWAY_TOKEN` (Environment) e `CONNECTIONSTRINGS__LOLCOACH` (migração). Nenhum valor em log; `::add-mask::` aplicado antes do uso.
- Tag por SHA garante rastreabilidade commit → imagem → deployment.

## Decisão G - Segurança de dados e configuração

| Nome | Onde vive | Sensível | Default desta spec |
|---|---|---|---|
| `ConnectionStrings__LoLCoach` | variável do serviço Railway (e secret do Environment para migração) | sim | obrigatório |
| `ASPNETCORE_ENVIRONMENT` | variável do serviço | não | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `ENTRYPOINT` do Dockerfile | não | `${PORT:-8080}` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | variável do serviço | não | `true` |
| `CORS__AllowedOrigins` | variável do serviço | não | ausente/vazio (deny-by-default) |
| `Swagger__Enabled` | variável do serviço | não | ausente/`false` |
| `GEMINI_API_KEY` / `AiCoach__Enabled` | variável do serviço | sim | ausente no MVP |
| `RAILWAY_TOKEN` | GitHub Environment `production` | sim | criado pelo usuário |
| `CONNECTIONSTRINGS__LOLCOACH` | GitHub Environment `production` | sim | já existente (usado pela migração) |

- `.env` e variantes permanecem ignorados (`.gitignore`); nenhum `.env` é criado ou versionado.
- Backend, bundle, logs e workflows não contêm credenciais (AC7).
- Rotação de credenciais: senha do Supabase pela Management API/`SUPABASE.md`; `RAILWAY_TOKEN` regenerado no painel. Procedimento no runbook.

## Decisão H - Rollback

1. **Aplicação:** redeploy da deployment anterior na Railway (a imagem por SHA já está no registry) — não requer rebuild nem alteração de banco, válido porque migrações são *expand/contract*.
2. **Migração:** proibido downgrade destrutivo; falha de schema é corrigida por migração *forward-fix* nova, com script revisado antes de aplicar (6.3).
3. **Critério de acionamento:** erro 5xx persistente após deploy, `/health/ready` em `503` contínuo, ou regressão funcional confirmada. Registro do incidente e do SHA revertido no runbook.

## Decisão I - Observabilidade mínima

- Logs estruturados do ASP.NET Core em stdout, coletados pela Railway (o contêiner não grava em disco: filesystem é efêmero).
- `/health` (plataforma) e `/health/ready` (monitor externo) como superfície de estado.
- `traceId` já presente em ProblemDetails (correlação de erro de request).
- Fora do escopo: APM, métricas, dashboards, alertas, tracing distribuído.

## Decisão J - Configuração da plataforma: IaC versionada

**Escolhido:** a configuração do serviço Railway é versionada em `.railway/railway.ts` (Infrastructure as Code, avaliada pelo Railway CLI), com valores não secretos declarados no arquivo e valores secretos mantidos na plataforma e referenciados apenas por nome.

**Justificativa (doc oficial, `docs.railway.com/infrastructure-as-code` e `/config-as-code`):** o Config as Code (`railway.json`/`railway.toml`) está **depreciado** — serviços novos não podem optar por ele e os arquivos legados deixam de ser lidos no corte definitivo de **2026-12-01**. Como o projeto ainda não tem serviço criado, começar no IaC evita uma migração obrigatória em semanas. Versionar a configuração também torna auditáveis `healthcheckPath`, réplicas e nomes de variáveis, que de outra forma viveriam só no painel.

**Fluxo:**

```text
.railway/railway.ts  --(railway config plan)-->  diff revisável (valores redigidos «hidden»)
                                                          |
                                        aprovação do usuário (apply é ação humana)
                                                          v
                              railway config apply [--yes] [--confirm-destructive]
```

- `railway config plan` é o gate de drift: por padrão os valores aparecem redigidos, então não há motivo para usar `--show-values` em CI ou em log compartilhado. `--detailed-exit-code` pode ser usado para falhar CI quando houver divergência.
- `railway config apply` nunca é executado por agente sem autorização explícita; mudanças destrutivas exigem `--confirm-destructive` (proteção adicional contra `--yes` isolado).
- Campo não expressável no DSL (que ainda está em beta) fica no painel e é registrado no runbook; `railway config plan` continua servindo para detectar divergência.
- `railway.json`/`railway.toml` são proibidos no repositório: um serviço não pode ser gerenciado pelos dois sistemas ao mesmo tempo.

## Ajustes de CI já identificados

- `frontend-ci.yml` está desatualizado (comentários e guardas `hashFiles` afirmando que o frontend não existe). A task T-OPS-5 remove as guardas obsoletas, ativa cache npm por `package-lock.json` e garante que build e testes rodem de fato.
- `deploy.yml` é substituído (T-OPS-3); nenhum outro workflow é alterado.
- Path filters permanecem: `backend/**` para backend, `frontend/**` para frontend, `**/*.py` para Python.

## Riscos e mitigações

| Risco | Mitigação |
|---|---|
| Deploy acidental em produção | Somente `workflow_dispatch`/release + Environment com revisor; auto-deploy Railway desligado |
| Migração aplicada sem revisão | Script SQL publicado como artefato antes do `update`; execução manual |
| Restart loop por banco indisponível | `healthcheckPath` = `/health` (sem dependência externa) |
| Secret vazando no bundle ou em log | `geminiApiKey` vazio em `environment.prod.ts`, API key só no backend, `::add-mask::`, AC7 |
| Imagem grande/lenta | Multi-stage com runtime `aspnet:10.0` sem SDK/Node; `.dockerignore` (R1.5) |
| CORS aberto "por conveniência" | Política deny-by-default preservada; AC5 |
| Divergência entre branch e produção | Tag de imagem por SHA + registro do SHA no runbook |
| DSL de IaC em beta | Campo não expressável vai para o painel e para o runbook; `railway config plan` detecta drift; migração oficial `railway config migrate` disponível |
| Healthcheck não contínuo (só no deploy) | `/health/ready` para monitor externo; runbook define verificação manual pós-deploy |
| API pública sem autenticação | Aceito no MVP (superfície de quota Riot/Gemini); autenticação e rate limiting ficam para spec futura |

## Dependências

- **Nenhuma dependência nova de runtime**: health checks vêm do framework compartilhado; nenhum pacote NuGet ou npm é adicionado. Ferramentas de build (Node, SDK) existem apenas em estágios de build da imagem.
- Ferramentas já no repositório: `dotnet-ef` (via `backend/dotnet-tools.json`) para migração, `backend/scripts/verify.sh` para verificação reprodutível.
- Ferramentas de plataforma usadas apenas no pipeline/runbook: Railway CLI (`railway up`, `railway config plan|apply|pull`), instalada no runner — nunca no runtime da aplicação.

## Open Questions

Nenhuma. Toda decisão não tomada está explicitamente fora do escopo ou listada como decisão do usuário em `spec.md`, com default seguro.
