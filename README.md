# LoLCoach - Spec-Driven Development

Projeto pessoal para analisar partidas de League of Legends e gerar pontos de melhoria a partir de dados da Riot API.

## Stack sugerida

- Backend: .NET / ASP.NET Core
- Frontend: Angular
- Banco: PostgreSQL
- ORM: EF Core
- Validação: FluentValidation
- Integrações HTTP: HttpClientFactory
- IA: opcional, somente após a camada determinística de analytics

## Estrutura

```text
/specs
  /001-player-search
    spec.md
    design.md
    tasks.md
  /002-match-import
    spec.md
    design.md
    tasks.md
  /003-analytics
    spec.md
    design.md
    tasks.md
  /004-dashboard
    spec.md
    design.md
    tasks.md
  /005-recommendations
    spec.md
    design.md
    tasks.md
  /006-ai-coach
    spec.md
    design.md
    tasks.md
  /007-swagger
    spec.md
    design.md
    tasks.md
/orchestrator
  ORCHESTRATOR.md
AGENTS.md
.hermes
  DESIGN.md
```

## Protocolo de desenvolvimento

O projeto usa um fluxo spec-driven documentado em [`specs/README.md`](specs/README.md). Ideias passam por descoberta, suposições e revisão red team antes de virar uma spec `READY`; implementação, testes, acessibilidade, segurança e operação precisam deixar evidência reproduzível. O contrato visual do frontend está em [`.hermes/DESIGN.md`](.hermes/DESIGN.md).

## Ordem recomendada

1. 001-player-search
2. 002-match-import
3. 003-analytics
4. 004-dashboard
5. 005-recommendations
6. 006-ai-coach
7. 007-swagger

As specs 001–007 existentes são a fonte de verdade do comportamento já decidido. Uma nova mudança deve criar ou atualizar a spec correspondente antes do código.

A IA não deve analisar diretamente o JSON bruto da Riot. Primeiro normalize os dados, calcule métricas e gere insights determinísticos; somente então use um LLM para explicar os resultados.

## Git Flow

Modelo de branching do repositório:

- `main` — produção. Recebe apenas merges de `homologacao` (releases prontas para deploy).
- `homologacao` — homologação. Integra `develop` quando o conjunto de features está pronto para validação.
- `develop` — integração contínua. **Branch default** do repositório.
- `feat/NNN-nome` — feature branches, sempre criadas a partir de `develop`.

Fluxo de uma feature:

```bash
git checkout develop
git checkout -b feat/001-minha-feature
# ... desenvolvimento ...
git push -u origin feat/001-minha-feature
```

Ao concluir, a feature é integrada de volta a `develop` via pull request. `main` e `homologacao` não recebem commits diretos.

## Caminho de deploy do MVP (Railway)

O MVP publica como **imagem única** — API .NET e SPA Angular no mesmo contêiner — em um
serviço na Railway, construída pelo `Dockerfile` da raiz. O procedimento operacional
completo está em [`specs/008-railway-deploy/runbook.md`](specs/008-railway-deploy/runbook.md).

- **Branch de produção:** `main`. A configuração do serviço é versionada em `.railway/railway.ts` (IaC); `railway.json` e `railway.toml` não existem no repositório e não devem ser criados (Config as Code é depreciado — Spec 008, R11.2).
- **Deploy manual:** o workflow `deploy` só roda por `workflow_dispatch` a partir de `main` ou por publicação de GitHub Release não-prerelease. `push` em `develop`/`homologacao` não publica nada, e o auto-deploy nativo da Railway fica desabilitado no painel.
- **Autorização explícita:** nenhum deploy, `railway config apply`, migração em produção, tag ou release acontece sem autorização explícita para aquela execução. O runbook descreve o procedimento; ele não é uma autorização.
- **Migrações:** passo separado e manual, pelo workflow `migrate` (forward-only, nunca automático no deploy) — detalhes em [`backend/README.md`](backend/README.md) e no runbook.
- **Sem valores sensíveis:** apenas **nomes** de variáveis e secrets aparecem na documentação. A aplicação lê `ConnectionStrings__LoLCoach` do ambiente; nenhuma connection string, token ou `.env` é versionado.
