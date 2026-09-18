---
id: 008
name: railway-deploy
status: READY
depends_on:
  - 001
  - 002
  - 003
  - 004
  - 005
  - 006
  - 007
---

# Spec 008 - Deploy do MVP na Railway

## Objetivo

Definir o contrato verificável de empacotamento e publicação do MVP LoLCoach na Railway: uma única imagem Docker, um único serviço público, Supabase como PostgreSQL gerenciado, health check para a plataforma, deploy somente sob autorização explícita e migrações controladas, sem expor secrets e com rollback definido.

## Base verificada (develop @ 5fb34f7)

- Specs 001 a 007 com `status: DONE`.
- Backend: .NET 10 / ASP.NET Core, solução `backend/LoLCoach.slnx`, projeto `backend/src/LoLCoach.Api`.
- Frontend: Angular 22 + Vitest; build de produção em `frontend/dist/frontend/browser` (`outputHashing: all`); base da API em runtime via `frontend/src/app/core/config/api-config.ts`.
- Persistência: PostgreSQL Supabase já provisionado (`aws-0-us-east-1.pooler.supabase.com:6543`), consumido por `ConnectionStrings__LoLCoach`; detalhes em `backend/SUPABASE.md`.
- Endpoints existentes: `POST /api/players/search`, `POST /api/players/{id}/matches/sync`, `GET /api/players/{id}/analysis`; ProblemDetails com `traceId`.
- CORS já implementado com política nomeada `AllowedOrigins` e negação por padrão fora de Development (`backend/src/LoLCoach.Api/Program.cs`).
- Ausentes hoje (verificado por busca no repositório): Dockerfile/.dockerignore, `wwwroot`/arquivos estáticos, endpoint de health, `UseForwardedHeaders`, alvo real de hosting. `.github/workflows/deploy.yml` é um placeholder sem alvo.
- `.github/workflows/frontend-ci.yml` ainda contém comentários/guardas afirmando que o frontend não existe (desatualizado).

## Escopo

- Empacotamento do MVP em uma imagem Docker única (API + SPA).
- Configuração de runtime do serviço Railway por variáveis de ambiente.
- Configuração versionada da plataforma (`.railway/railway.ts`), com `railway.json`/`railway.toml` proibidos.
- Health checks para a plataforma e para monitoramento externo.
- Serviço do SPA na mesma origem da API.
- Pipeline de deploy manual/release, com aprovação e sem auto-deploy.
- Migração de banco controlada, auditável e forward-only.
- Rollback documentado e observabilidade mínima.
- Runbook operacional.

## Requisitos

### R1 - Empacotamento

1.1 Deve existir um `Dockerfile` na raiz do repositório que produza **uma única imagem** contendo a API e o bundle estático do Angular.
1.2 O build deve ser multi-stage: SDK .NET 10 e Node apenas nos estágios de build; a imagem final usa a base de runtime `aspnet:10.0`, sem SDK, sem Node/npm e sem código-fonte.
1.3 O processo deve rodar como usuário não-root.
1.4 Nenhum secret pode ser passado como `ARG`/`ENV` de build nem ficar gravado em camadas da imagem.
1.5 `.dockerignore` deve excluir `.git`, `.worktrees`, `node_modules`, `dist`, `bin`, `obj`, `artifacts`, `.angular` e `.env*`.

### R2 - Runtime e configuração

2.1 O processo escuta na porta injetada por `PORT` (a Railway injeta essa variável e usa o mesmo valor para roteamento e para o healthcheck), com fallback `8080` quando `PORT` não existir: `ASPNETCORE_HTTP_PORTS=${PORT:-8080}` no `ENTRYPOINT`. `EXPOSE 8080` apenas documenta o fallback local. Não fixar `--urls` nem usar target port divergente do `PORT`.
2.2 O serviço publicado roda com `ASPNETCORE_ENVIRONMENT=Production`.
2.3 Toda configuração vem de variáveis de ambiente; nenhum valor sensível em `appsettings*.json` versionado.
2.4 Variáveis obrigatórias: `ConnectionStrings__LoLCoach` (secret) e `ASPNETCORE_ENVIRONMENT`. Todas as demais têm default seguro no código.

### R3 - Health check

3.1 `GET /health` responde `200` com corpo JSON (`{"status":"healthy"}`) sem depender de banco, Riot ou Gemini; é o `healthcheckPath` da Railway. A plataforma consulta o endpoint **apenas durante a subida de um deploy** (não é monitoramento contínuo), com timeout padrão de 300s ajustável por `RAILWAY_HEALTHCHECK_TIMEOUT_SEC`.
3.2 `GET /health/ready` verifica conectividade com o PostgreSQL: `200` quando alcançável, `503` quando não. Não pode ser usado como `healthcheckPath`, para não gerar restart loop por indisponibilidade transitória do banco — e como a visão de health da plataforma é só de deploy, é esse endpoint que serve ao monitoramento externo.
3.3 Nenhuma dependência de pacote nova é necessária: a API de HealthChecks pertence ao framework compartilhado `Microsoft.AspNetCore.App`.
3.4 Health checks não podem logar connection string nem credenciais.
3.5 A origem das requisições de healthcheck (`healthcheck.railway.app`) deve ser aceita: manter `AllowedHosts: "*"` ou incluir esse hostname explicitamente caso `AllowedHosts` seja restringido.

### R4 - Serviço do SPA (mesma origem)

4.1 A API serve os arquivos estáticos do bundle Angular publicado.
4.2 `GET /` e rotas de cliente do Angular (`/player/:id`, a rota de dashboard do `app.routes.ts`) devolvem `index.html` (SPA fallback), incluindo refresh direto na rota.
4.3 Rotas inexistentes sob `/api/**` continuam devolvendo erro JSON (404/ProblemDetails), nunca `index.html`.
4.4 Em produção o frontend continua usando `environment.apiUrl` relativo (`''`), chamando `/api/...` na mesma origem; `window.LOLCOACH_API_URL` permanece apenas como override opcional documentado.

### R5 - CORS

5.1 Em produção, sem origem externa configurada, `CORS__AllowedOrigins` permanece vazio e a política continua negando origem cruzada (nunca `AllowAnyOrigin`).
5.2 Requisição com `Origin` não permitido não pode receber `Access-Control-Allow-Origin` na resposta.

### R6 - Persistência e migrações controladas

6.1 O startup do contêiner não aplica migrações nem bloqueia a subida esperando o banco.
6.2 Migrações são aplicadas por etapa explícita e auditável, disparada manualmente (`workflow_dispatch`) em GitHub Environment protegido, nunca automaticamente no deploy.
6.3 Antes de aplicar, o SQL resultante (`dotnet ef migrations script`) é gerado e publicado como artefato de revisão.
6.4 Migrações são forward-only e compatíveis com a versão anterior da aplicação (expand/contract); migrações existentes não podem ser alteradas.

### R7 - Pipeline e autorização de deploy

7.1 `.github/workflows/deploy.yml` deixa de ser placeholder e passa a publicar a imagem e acionar a Railway.
7.2 O deploy só é acionado manualmente (`workflow_dispatch` a partir de `main`) ou pela publicação de uma GitHub Release; push em `develop`/`homologacao` não publica nada.
7.3 O job de deploy usa GitHub Environment protegido (`production`) com revisores obrigatórios.
7.4 O auto-deploy nativo da Railway (integração GitHub) permanece desabilitado; o gatilho é humano.
7.5 Credenciais do pipeline (`RAILWAY_TOKEN`, `CONNECTIONSTRINGS__LOLCOACH`) vêm de secrets do GitHub/Environment, com máscara em log (`::add-mask::`) e nunca ecoadas.
7.6 A imagem publicada é rastreável por tag imutável com o SHA do commit.

### R8 - Rollback

8.1 Procedimento documentado de rollback de aplicação (redeploy da deployment anterior na Railway) e de migração (correção forward-fix), com checklist e responsável.
8.2 Não existe rollback automático que reverta migração aplicada; downgrade destrutivo de schema é proibido.

### R9 - Observabilidade mínima

9.1 Logs da aplicação em stdout, capturados pela Railway, sem conteúdo sensível.
9.2 `/health` e `/health/ready` disponíveis para monitoramento externo; `traceId` em ProblemDetails mantido.
9.3 Swagger desligado em produção (`Swagger__Enabled` ausente ou `false`).
9.4 Fora do escopo: APM, métricas, dashboards e alertas.

### R10 - Documentação

10.1 Runbook em `specs/008-railway-deploy/runbook.md` com configuração do serviço (porta, healthcheckPath, região, variáveis), primeiro deploy, migração, rollback e rotação de secrets.
10.2 `README.md` e `backend/README.md` atualizados com o caminho de deploy, sem valores sensíveis.

### R11 - Configuração da plataforma versionada

11.1 A configuração do serviço (build a partir do Dockerfile, `healthcheckPath=/health`, réplicas e variáveis não secretas) é versionada em `.railway/railway.ts` (Infrastructure as Code, avaliada pelo Railway CLI).
11.2 `railway.json` e `railway.toml` são proibidos no repositório: Config as Code está depreciado, serviços novos não podem optar por ele e os arquivos legados deixam de ser lidos no corte definitivo de 2026-12-01.
11.3 Valores secretos nunca entram no arquivo de IaC: permanecem na plataforma e são referenciados apenas por nome (variáveis secretas preservadas pelo próprio Railway).
11.4 `railway config plan` (que redige valores por padrão) é o gate de drift; `railway config apply` exige autorização explícita do usuário, nunca usa `--show-values` em CI/log e, em mudança destrutiva, exige `--confirm-destructive`.

## Critérios de aceitação

- **AC1** `docker build -t lolcoach:local .` conclui sem erro a partir da raiz.
- **AC2** A imagem final não contém Node/npm nem SDK .NET (`docker run --rm lolcoach:local which node` e `which dotnet` não encontram binários) e a aplicação sobe na base `aspnet:10.0`.
- **AC3** Com `ASPNETCORE_ENVIRONMENT=Production` e connection string válida: `GET /health` → `200 {"status":"healthy"}`; `GET /` → `200 text/html`; `GET /player/<guid>` → `200 text/html`; `GET /api/rota-inexistente` → `404` JSON.
- **AC4** `GET /health/ready` → `200` com banco alcançável e `503` com connection string inválida, permanecendo `/health` em `200` nos dois casos.
- **AC5** Requisição com `Origin: https://evil.example` não retorna `Access-Control-Allow-Origin`.
- **AC6** Em Production, `GET /swagger/index.html` → `404`.
- **AC7** Nenhum secret real no repositório (`git grep` para `postgres://`, `AIza…`, `GEMINI_API_KEY=` não retorna valor real), nenhum `.env` rastreado, e o bundle em `frontend/dist/frontend/browser` não contém connection string nem chave.
- **AC8** `bash backend/scripts/verify.sh` verde (build, testes, format, auditoria de pacotes, smoke) e `npm ci && npm run build && npm test` verdes em `frontend`.
- **AC9** `push`/`pull_request` em `develop` e `homologacao` não dispara job de deploy.
- **AC10** O workflow de migração só roda por `workflow_dispatch`, publica o script SQL como artefato e não é referenciado em nenhum passo automático de deploy.
- **AC11** Subir o contêiner apontando para um PostgreSQL vazio não cria tabelas (nenhuma migração automática) e não impede `/health` de responder `200`.
- **AC12** Runbook cobre primeiro deploy, variáveis, migração, rollback, rotação de secrets e registra que todo deploy exige autorização explícita do usuário.
- **AC13** Nenhuma migração existente foi alterada (`backend/src/LoLCoach.Api/Infrastructure/Migrations/**` idêntica a `develop`).
- **AC14** `.railway/railway.ts` descreve o serviço com `healthcheck: "/health"`, réplicas e variáveis apenas por nome; `railway config plan` roda sem `--show-values` e não lista deleções inesperadas.
- **AC15** Não existe `railway.json` nem `railway.toml` no repositório.
- **AC16** Com `PORT` definida (ex.: `PORT=9999`), o contêiner escuta nela e responde `/health` nessa porta; sem `PORT`, usa `8080`.

## Evidência obrigatória

- `specs/008-railway-deploy/evidence/qa-validation-report.md` com comandos e saídas dos AC1–AC13.
- `specs/008-railway-deploy/evidence/code-review-report.md` com parecer do `code-reviewer`.
- `specs/008-railway-deploy/runbook.md` (R10).

## Fora do escopo

- Criar/configurar o projeto Railway real, escolher plano ou criar secrets (etapa do usuário, fora desta spec).
- Executar deploy, push, merge, tag ou release nesta etapa de especificação.
- Criar o Dockerfile nesta etapa (pertence às tasks de implementação).
- Criar `.railway/railway.ts`, workflows ou qualquer outro artefato de implementação nesta etapa.
- Configurar o projeto/serviço Railway real, importar (`railway config pull`) ou aplicar (`railway config apply`) configuração, criar domínio ou variáveis (ação humana autorizada).
- Alterar código de aplicação nesta etapa.
- Ambiente de homologação em nuvem, domínio customizado, CDN e escala horizontal.
- Habilitar o LLM (Gemini) em produção: o MVP publica com a fábrica determinística de relatório (spec 006).
- Substituir o Supabase pelo PostgreSQL da Railway.
- Preservar qualquer alteração local não commitada: `frontend/angular.json` modificado no checkout principal pertence ao usuário e **não** entra na branch desta spec.

## Decisões do usuário (não bloqueantes, com default adotado)

| Assunto | Default adotado nesta spec | Como alterar |
|---|---|---|
| Ambiente de homologação na Railway | Fora do escopo; apenas `production` | nova spec |
| Domínio customizado | Domínio `*.up.railway.app` do serviço | decisão do usuário, sem impacto no contrato |
| Região Railway | `us-east4` (Virginia), próxima ao Supabase `us-east-1`; nome exato confirmado na implementação | variável do serviço na Railway |
| AI coach / `GEMINI_API_KEY` | Desabilitado (relatório determinístico) | definir `GEMINI_API_KEY` e `AiCoach__Enabled=true` |
| Réplicas | 1 réplica | plano da Railway |
| Migração do banco | Workflow manual no GitHub Actions | execução manual do operador |
| Configuração da plataforma | IaC em `.railway/railway.ts` (beta do DSL) | painel + registro no runbook, se o DSL não expressar o campo |
| Plano/cobrança da Railway | Fora do escopo | decisão do usuário |
| Autenticação na API | Fora do escopo (MVP) | nova spec |

## Open Questions

Nenhuma pendência bloqueante. As decisões acima têm default seguro, estão marcadas como fora do escopo ou como decisão do usuário, e podem ser alteradas sem invalidar requisitos.
