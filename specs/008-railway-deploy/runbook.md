# Runbook - Deploy do MVP LoLCoach na Railway (Spec 008)

Documento operacional da Spec 008. Complementa `spec.md` (contrato), `design.md`
(decisões) e `tasks.md` (T-OPS-1..T-OPS-10). Nenhum valor secreto aparece aqui:
apenas **nomes** de variáveis e comandos.

> **Regra de ouro:** nenhum deploy, `railway config apply`, tag, release, merge ou
> rotação de secret acontece sem autorização explícita do usuário para aquela
> execução. Este documento descreve o procedimento; não é uma autorização.

---

## 1. Fatos do ambiente (verificados em 2026-09-18)

| Item | Valor |
|---|---|
| Repositório | `Sharpista/LoLCoach` (privado) |
| Projeto Railway | `e940ef74-8e88-4f71-b816-587840a27480` (`sparkling-dream`) |
| Environment Railway | `b6b44236-a353-474b-8e8e-cbed18ef0ceb` (`production`) |
| GitHub Environment | `production` (aparece no GitHub como `sparkling-dream / production`) |
| Serviço no IaC | `api` (topologia de 1 serviço - `design.md`, Decisão A) |
| Imagem | `ghcr.io/sharpista/lolcoach:<sha-do-commit>` (tag imutável) |
| Dockerfile | `Dockerfile` na raiz do repositório (multi-stage: Node 24 → SDK .NET 10 → `aspnet:10.0`) |
| Porta | `PORT` injetada pela Railway; fallback local `8080` |
| Healthcheck | `healthcheckPath=/health`, timeout 300s |
| Região alvo | US East Metal (Virginia) - region identifier `us-east4-eqdc4a` |
| Réplicas | 1 |
| Banco | PostgreSQL Supabase (pooler externo, `ConnectionStrings__LoLCoach`) |
| App GitHub da Railway | instalado (`railway-app[bot]`) |
| Auto-deploy nativo | **DESABILITADO** (ver §7 - é pré-requisito do deploy manual) |
| Deployment legado registrado | id `6532521601`, sha `e80281a`, status `failure` (auto-deploy antigo; não é referência de rollback) |
| Railway CLI local | não instalada nesta estação (sem token) - ver §10 |
| Docker local | `29.8.0` (verificações de §9 executadas localmente) |

### Mapa de arquivos desta entrega

| Arquivo | Papel |
|---|---|
| `Dockerfile` | imagem única (API + SPA) |
| `.dockerignore` | contexto mínimo; nenhum secret no contexto de build |
| `.railway/railway.ts` | configuração versionada da plataforma (IaC) |
| `.github/workflows/deploy.yml` | build/push do GHCR + deploy na Railway |
| `.github/workflows/migrate.yml` | migrações controladas (manual) |
| `.github/workflows/frontend-ci.yml` | CI real do frontend |

Não existe `railway.json` nem `railway.toml` no repositório (AC15): o Config as
Code é depreciado e para de ser lido em **2026-12-01**.

---

## 2. Configuração do serviço

| Campo | Valor | Onde é definido |
|---|---|---|
| Fonte | repositório GitHub `Sharpista/LoLCoach`, branch `main` | `.railway/railway.ts` |
| Build | **Dockerfile da raiz** (sem build/start command no IaC, para não desviar do `ENTRYPOINT`) | `Dockerfile` + `.railway/railway.ts` |
| Porta de escuta | `ASPNETCORE_HTTP_PORTS=${PORT:-8080}` no `ENTRYPOINT` | `Dockerfile` |
| Healthcheck | `/health` (liveness; sem banco/Riot/Gemini) | `.railway/railway.ts` (`healthcheck`) |
| Timeout do healthcheck | 300s (padrão da plataforma; ajustável por `RAILWAY_HEALTHCHECK_TIMEOUT_SEC`) | `.railway/railway.ts` (`healthcheckTimeout`) |
| Região | US East Metal (Virginia) | painel (ver §3.4: o DSL beta não foi validado para placement) |
| Réplicas | 1 | `.railway/railway.ts` (`replicas`) |
| Domínio | `*.up.railway.app` gerado pelo Railway | painel (`railway domain`) |
| Reinício | sem migração no startup; `/health` não depende do banco (evita restart loop) | `Program.cs` (base `feat/008-backend-health-spa`) |

Por que `/health` e não `/health/ready` como `healthcheckPath`: a Railway consulta
o healthcheck **apenas na subida de um deploy**. Usar o endpoint que fala com o
Postgres transformaria uma oscilação do pooler Supabase em restart loop.
Monitoramento contínuo externo deve bater em `/health/ready` (200/503).

### 2.1 Variáveis do serviço (apenas nomes)

| Variável | Sensível | Valor esperado | Origem |
|---|---|---|---|
| `ConnectionStrings__LoLCoach` | **sim** | já configurado na plataforma (Supabase) | painel Railway; no IaC aparece como `preserve()` |
| `ASPNETCORE_ENVIRONMENT` | não | `Production` | `.railway/railway.ts` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | não | `true` (TLS/host corretos atrás do proxy) | `.railway/railway.ts` |
| `PORT` | não | injetada pela plataforma | Railway (não declarar) |
| `CORS__AllowedOrigins` | não | intencionalmente **ausente/vazio** (deny-by-default, R5.1) | default de `appsettings.json` |
| `Swagger__Enabled` | não | intencionalmente **ausente** (Swagger desligado fora de Development, R9.3) | default do código |
| `GEMINI_API_KEY` / `AiCoach__Enabled` | sim | **não configurar** no MVP (relatório determinístico) | fora do escopo |

Secrets do GitHub Environment `production` (apenas nomes):

| Secret | Usado por | Observação |
|---|---|---|
| `RAILWAY_TOKEN` | `deploy.yml` | **não existe hoje no repositório** - precisa ser criado pelo usuário (project token do environment `production`) |
| `CONNECTIONSTRINGS__LOLCOACH` | `migrate.yml` | já existe no repositório |

Nenhum `.env` é criado, lido ou versionado em nenhum passo deste runbook.

---

## 3. Fluxo de IaC (gate de drift)

```text
.railway/railway.ts --(railway config plan)--> diff revisável (valores redigidos)
                                                    |
                              aprovação explícita do usuário
                                                    v
                    railway config apply  (execução HUMANA, nunca do agente)
```

Pré-requisitos em uma estação autorizada:

```bash
curl -fsSL agents.railway.com | sh          # instala a Railway CLI
railway login                               # ou exportar RAILWAY_TOKEN (project token)
railway link                                # projeto e940ef74-... / environment b6b44236-...
npm install --no-save railway               # SDK do DSL (import "railway/iac")
```

Nota verificada: `npm install --no-save railway` funciona sem `package.json` na
raiz (cria apenas `node_modules/`, que já está no `.gitignore`). O pacote `railway`
(3.11.0) é dependência **de ferramenta**, não do runtime da aplicação.

### 3.1 Plan (verificação; seguro, não muda nada)

```bash
railway config plan
railway config plan --detailed-exit-code    # 0 = sem drift, 2 = há mudanças pendentes
```

- Saída esperada na primeira aplicação: criação do serviço `api` e das variáveis
  não secretas. Os valores aparecem **redigidos** (`«hidden»`) por padrão.
- **`--show-values` é proibido** em CI, em log compartilhado e em qualquer
  automação (R11.4 / T-OPS-10). Se for indispensável revisar um valor não secreto,
  faça em terminal local, sem redirecionar saída para arquivo.
- Antes de aplicar, revise o diff procurando por: deleção de serviço, deleção de
  variável, deleção de volume e alteração de `source`. Qualquer um desses itens
  exige confirmação extra do usuário.

### 3.2 Apply (ação humana, autorizada)

```bash
railway config apply                       # interativo: confirma o diff exibido
railway config apply --yes --confirm-destructive   # só com autorização explícita
```

`apply` roda um plan novo imediatamente antes e recusa aplicar contra estado
obsoleto. `--yes` isolado não remove recursos: mudança destrutiva exige
`--confirm-destructive`.

### 3.3 Reconciliação do serviço existente

A integração GitHub da Railway já criou um serviço (deployment `6532521601`). O
IaC declara o serviço `api`:

1. Rode `railway config plan` e confira se aparece **criação** de serviço.
2. Se o serviço existente tiver outro nome, renomeie-o para `api` no painel
   **antes** do `apply` - caso contrário o plan vai propor um serviço novo e o
   antigo (com auto-deploy ligado) continuaria existindo.
3. Confirme que os nomes do projeto (`sparkling-dream`) e do environment
   (`production`) batem com o painel; ajuste o arquivo se a CLI mostrar outro nome.

### 3.4 Campos não expressos no DSL (registro obrigatório)

| Campo | Situação |
|---|---|
| Região (`us-east4-eqdc4a`) | selecionada no painel do serviço; o formato de placement do DSL (`replicas: { "<region>": n }`) é beta e não foi validado sem a CLI. O `plan` deve mostrar a região como campo gerenciado pelo painel, sem drift. |
| Toggle de auto-deploy | permanece no painel (§7); não existe campo no DSL. |
| Domínio `*.up.railway.app` | gerado pela plataforma, não entra no IaC (a doc exclui domínios gerados). |

---

## 4. Primeiro deploy (procedimento)

Pré-condições (todas obrigatórias):

- [ ] `main` contém o `Dockerfile` e `.railway/railway.ts` (merge aprovado).
- [ ] Secret `RAILWAY_TOKEN` criado no GitHub Environment `production`
      (project token do environment `production`; não usar account token).
- [ ] §7 concluída: auto-deploy nativo **desligado** no painel.
- [ ] §3.1 executada e diff revisado sem deleções inesperadas.
- [ ] Autorização explícita do usuário para executar o deploy.

Execução:

1. **Imagem** - dispare o workflow `deploy` (Actions → deploy → Run workflow,
   branch `main`). O job `build` publica `ghcr.io/sharpista/lolcoach:<sha>` no GHCR
   (tag imutável por SHA, R7.6). O mesmo resultado é obtido por GitHub Release
   não-prerelease.
2. **Deploy** - o job `deploy` roda no Environment `production` e executa
   `railway up --service api --environment production`. Ele usa o **mesmo SHA** do
   job de build (mesmo commit do workflow), portanto o código implantado e a
   imagem publicada apontam para o mesmo commit.
3. **Observar** - acompanhe os logs do build/deploy:

   ```bash
   railway logs --build --service api
   railway logs --service api
   ```

4. **Registrar o SHA publicado** (§8) e executar a verificação pós-deploy (§6).

Deploy alternativo (sem GitHub Actions), apenas com autorização e CLI autenticada:

```bash
git fetch && git checkout <sha-autorizado>
railway up --service api --environment production --message "manual <sha>"
```

### 4.1 Artefato, imagem e rastreabilidade

Entenda a diferença entre os dois artefatos, para não confundir "imagem publicada"
com "o que está rodando":

| Artefato | Quem produz | Para que serve |
|---|---|---|
| `ghcr.io/sharpista/lolcoach:<sha>` | job `build` do workflow (GHCR) | rastreabilidade commit → imagem, auditoria e promoção manual/rollback por imagem |
| build do serviço `api` na Railway | `railway up` (a partir do mesmo commit) | o que efetivamente roda em `production` |

- Ambos partem do **mesmo SHA**: o job `deploy` faz checkout do commit do workflow,
  então `railway up` envia exatamente a árvore que gerou a imagem do GHCR.
- A CLI não possui flag para "deployar esta imagem"; por isso o caminho suportado é
  `railway up` (build pela plataforma a partir do `Dockerfile`). Se a promoção da
  imagem do GHCR for preferida no futuro, use o painel/API para apontar o serviço
  para `ghcr.io/sharpista/lolcoach:<sha>` e registre a mudança no runbook - o
  `railway config plan` passa a mostrar a mudança de `source`.
- O `--message` acima é o que amarra a deployment ao SHA no histórico da Railway.

---

## 5. Migrações (execução manual, forward-only)

Pré-condições:

- [ ] Deploy da aplicação compatível com o schema atual já concluído (expand/contract).
- [ ] Script SQL revisado (§5.1).
- [ ] Autorização explícita do usuário para aplicar no banco de produção.
- [ ] Backup/restore do Supabase conhecido antes da execução (o Supabase já mantém
      PITR; confirme que a retenção cobre a janela de reversão necessária).

### 5.1 Gerar e revisar o script (sem tocar produção)

Actions → `migrate` → Run workflow (branch `main`), campo `confirmacao` deixado
em branco (a confirmação só é usada pelo job de aplicação).

O job `script`:

- roda `dotnet ef migrations script --idempotent` com uma connection string
  **sintética** (não abre conexão);
- publica `migrations-idempotent-sql` como artefato de revisão.

Revise o SQL do artefato: tipos de coluna, índices, `NOT NULL` em tabela com dados,
locks longos e operações irreversíveis. Migração existente **nunca** é editada
(AC13): correção é sempre uma migração nova.

### 5.2 Aplicar

Run workflow novamente com `confirmacao = aplicar-migracoes` (o job `apply` roda no
Environment `production` e usa `CONNECTIONSTRINGS__LOLCOACH`):

```bash
dotnet ef database update --project src/LoLCoach.Api --startup-project src/LoLCoach.Api   # equivalente local
```

O workflow é `workflow_dispatch` puro: **nenhum** passo de `deploy.yml` o invoca
(AC10). O startup do contêiner nunca migra (R6.1).

---

## 6. Verificação pós-deploy (canário)

Execute contra o domínio do serviço (`railway domain` mostra o endereço):

| # | Verificação | Esperado |
|---|---|---|
| 1 | `curl -fsS https://<dominio>/health` | `200` com `{"status":"healthy"}` |
| 2 | `curl -fsS -o /dev/null -w '%{http_code}' https://<dominio>/` | `200` com `text/html` |
| 3 | `curl -fsS -o /dev/null -w '%{content_type}' https://<dominio>/player/<guid>` | `text/html` (deep-link/refresh do SPA) |
| 4 | `curl -s -o /dev/null -w '%{http_code}' https://<dominio>/api/nao-existe` | `404` JSON (ProblemDetails com `traceId`) |
| 5 | `curl -s -o /dev/null -w '%{http_code}' https://<dominio>/swagger/index.html` | `404` |
| 6 | `curl -sI -H 'Origin: https://evil.example' https://<dominio>/health \| grep -i access-control-allow-origin` | **nenhuma** saída |
| 7 | `curl -fsS https://<dominio>/health/ready` | `200` (banco alcançável); `503` se o pooler estiver indisponível - investigar antes de declarar sucesso |
| 8 | `railway logs --service api \| tail` | logs em stdout, sem credencial/connection string |
| 9 | Bundle servido | `curl -s https://<dominio>/ \| grep -i '<base href="/">'` |

Fluxo crítico de negócio (uma vez, com dados sintéticos quando aplicável):
busca de jogador → importação de partidas → análise. Falha persistente (5xx,
`/health/ready` em 503 contínuo ou regressão funcional) aciona o rollback (§7 de
rollback abaixo).

---

## 7. Auto-deploy e autorização

- **Auto-deploy nativo da Railway: DESABILITADO.** O app GitHub da Railway continua
  instalado para permitir `railway up` e builds do repositório, mas a opção
  *Auto Deploy* do serviço fica **desligada** no painel: o único gatilho é o
  workflow `deploy.yml` (dispatch em `main` ou Release publicada). `push` em
  `develop`/`homologacao` não publica nada (AC9).
- Verificação periódica: painel do serviço → *Settings* → *Source* → o toggle deve
  estar desligado. Se aparecer deployment com origem em push do GitHub, trate como
  incidente: desligue o toggle, apure o deployment e registre no histórico.
- Checklist de autorização para qualquer deploy:

  - [ ] Escopo autorizado, ambiente identificado (`production`) e SHA do commit definido.
  - [ ] `railway config plan` revisado (sem deleções inesperadas).
  - [ ] Imagem/tag do SHA publicada no GHCR.
  - [ ] Migração necessária já revisada e decidida (antes ou depois do deploy, nunca automática).
  - [ ] Janela e responsável pelo acompanhamento definidos.
  - [ ] Plano de rollback conhecido e exequível nesta execução.

---

## 8. Registro do SHA publicado

Todo deploy precisa deixar rastro. Preencha uma linha por deploy:

| Data (UTC) | Ambiente | Commit (sha) | Imagem | Deployment Railway | Operador | Resultado | Rollback? |
|---|---|---|---|---|---|---|---|
| - | production | - | `ghcr.io/sharpista/lolcoach:<sha>` | - | - | - | - |

Fontes para preencher:

- commit e imagem: job summary do workflow `deploy` e runs do Actions;
- deployment: `railway deployment list --service api` (id, status, horário);
- resultado: §6 (canário) e o histórico de incidentes.

---

## 9. Rollback

### 9.1 Aplicação (preferencial, rápido)

1. `railway deployment list --service api` para identificar o deployment anterior
   (id, status `SUCCESS`, data anterior ao incidente).
2. No painel do serviço → *Deployments* → selecione o deployment anterior →
   **Redeploy**. O artefato já existe (imagem por SHA), então não há rebuild e o
   schema não muda - válido porque as migrações são *expand/contract*.
3. `railway redeploy` redeploya apenas o **último** deployment; para voltar a um
   deployment mais antigo use o painel (ou a API) - a CLI não aceita um id alvo.
4. Após o redeploy, repita §6 (canário) e registre em §8 (coluna "Rollback: sim",
   com o SHA revertido e o motivo).

### 9.2 Migração (forward-fix apenas)

- **Proibido** downgrade destrutivo de schema (R8.2): não existe rollback
  automático de migração aplicada.
- Correção é uma **nova migração** (`dotnet ef migrations add ...`),
  compatível com a versão anterior da aplicação, revisada via §5.1 e aplicada via §5.2.
- Se a migração errada já corrompeu dados, a recuperação é restaurar o banco pelo
  Supabase (PITR/backup) **com autorização explícita** e então reaplicar as
  migrações corretas - trate como incidente, não como rollback de rotina.

### 9.3 Critério de acionamento

Erro 5xx persistente após o deploy, `/health/ready` em 503 contínuo sem causa
externa, ou regressão funcional confirmada no fluxo crítico. Registre horário,
SHA revertido, sintoma e evidência.

---

## 10. Rotação de secrets

| Secret | Como rotacionar | Passos |
|---|---|---|
| `ConnectionStrings__LoLCoach` (senha do Supabase) | painel do Supabase (Management API) | 1) rotacionar a senha; 2) atualizar a variável do serviço na Railway (nova connection string, pooler 6543); 3) atualizar o secret `CONNECTIONSTRINGS__LOLCOACH` no GitHub Environment `production`; 4) redeploy do serviço (`railway redeploy` ou novo deploy) para pegar o novo valor; 5) §6 (canário), com atenção a `/health/ready`; 6) registrar a rotação. |
| `RAILWAY_TOKEN` | painel da Railway (tokens) | 1) revogar o token antigo; 2) criar novo project token do environment `production`; 3) atualizar o secret no GitHub Environment `production`; 4) rodar `deploy` (dispatch em `main`) para validar; 5) registrar. |

Regras: nunca colar valor em issue/PR/log/chat; nunca commitar `.env`; o token
antigo precisa ser revogado (não apenas substituído) para não deixar credencial
ativa.

---

## 11. Limitações conhecidas e desvios (registrados, não silenciados)

| # | Item | Evidência | Impacto | Encaminhamento |
|---|---|---|---|---|
| L1 | **R7.3 - revisores obrigatórios no Environment**: não foi possível configurar required reviewers | `GET /repos/Sharpista/LoLCoach/environments` retorna `sparkling-dream / production` com `protection_rules: []`; a API de required reviewers retorna `403 Upgrade to GitHub Pro` (repositório privado) | O gate de aprovação do deploy fica sendo o disparo manual + o Environment (sem segunda pessoa obrigatória). A spec pede revisores obrigatórios; o plano atual do GitHub não oferece o recurso. | Decisão do usuário: (a) aceitar o desvio com o disparo manual como gate; (b) migrar para GitHub Team/Pro; (c) compensar com um gate externo (ex.: aprovação registrada no card antes do dispatch). Registrar a escolha na spec. |
| L2 | **AC14 - `railway config plan` não executado** | Railway CLI não instalada e sem token/CLI local (`which railway` → não encontrado) | O IaC foi verificado por análise estática (tipos do DSL) e não por `plan` real. Drift/aceitação dos campos ainda não foi provado contra a plataforma. | Retomada: com CLI autenticada e `railway link` no projeto `e940ef74-8e88-4f71-b816-587840a27480`, rodar `railway config plan --detailed-exit-code` e anexar a saída (valores redigidos) ao card. |
| L3 | **AC2 - redação literal vs. intenção** | `which node` → ausente na imagem final (correto). `which dotnet` → **presente** (`/usr/bin/dotnet`): é o *runtime* .NET, que é a própria base `aspnet:10.0` e é necessário para executar a aplicação. Não há SDK (`dotnet --list-sdks` vazio). | A checagem literal de AC2 ("`which dotnet` não encontra binários") não pode passar sem remover o runtime, o que quebraria a aplicação. A intenção (sem SDK/Node) é atendida. | Ajustar a redação de AC2 na spec (verificar ausência de **SDK**, não do runtime) - decisão de QA/orquestrador. Não remover o symlink `dotnet` para "passar" na checagem. |
| L4 | **Variáveis de deploy no GitHub Environment** | `RAILWAY_TOKEN` não existe no repositório | O workflow `deploy.yml` está correto, mas não pode ser executado com sucesso até o secret existir. | Ação do usuário: criar o secret. Nenhum agente cria/edita secrets ou Environments. |
| L5 | **Região e toggle de auto-deploy fora do DSL** | `.railway/railway.ts` (campos não expressáveis) | Configuração vive no painel; `plan` detecta drift, mas não a corrige. | Revisar no `apply` e registrar mudanças neste runbook. |

> **Nota de endurecimento (não bloqueante):** os workflows usam tags maiores de
> action (`@v4`) para manter consistência com `backend-ci.yml`/`python-ci.yml`.
> Fixar por SHA imutável é a evolução recomendada para o pipeline de produção.

---

## 12. Referências

- `specs/008-railway-deploy/spec.md` (R1..R11, AC1..AC16)
- `specs/008-railway-deploy/design.md` (Decisões A..J)
- `specs/008-railway-deploy/tasks.md` (T-OPS-1..T-OPS-10)
- `backend/SUPABASE.md` (banco gerenciado)
- Docs oficiais Railway: *Infrastructure as Code*, `railway config`, `railway up`,
  healthchecks, regions.
