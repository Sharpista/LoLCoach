// Spec 008 / T-OPS-9 - configuração versionada da plataforma (Railway IaC).
//
// Fonte de verdade do contrato: `specs/008-railway-deploy/spec.md` (R11) e
// `design.md` (Decisão J). O arquivo é avaliado pela Railway CLI
// (`railway config plan` / `railway config apply`), nunca pela aplicação.
//
// Regras respeitadas aqui:
//   - `railway.json` / `railway.toml` são PROIBIDOS no repositório (R11.2): o
//     Config as Code é depreciado e para de ser lido em 2026-12-01.
//   - valores secretos NÃO entram neste arquivo: permanecem na plataforma e são
//     referenciados apenas por nome via `preserve()` (R11.3).
//   - `railway config plan` é o gate de drift; `apply` é ação humana autorizada
//     e nunca usa `--show-values` (R11.4).

import { defineRailway, github, preserve, project, service } from "railway/iac";

export default defineRailway(() => {
  // Serviço único (design.md, Decisão A): a mesma imagem serve a API e o SPA.
  //
  // `source: github(...)` + `build: { builder: "DOCKERFILE" }` fazem a Railway
  // construir a partir do Dockerfile da RAIZ do repositório (T-OPS-9). Não há
  // `start`/`buildCommand`: quem define o processo é o ENTRYPOINT da imagem, que
  // é quem honra `PORT` (R2.1). O repositório não tem `railway.json`/`railway.toml`,
  // então não há dois sistemas gerenciando o mesmo serviço.
  //
  // AUTO-DEPLOY: a integração GitHub da Railway deve permanecer com auto-deploy
  // DESLIGADO (R7.4) - o gatilho é o workflow `.github/workflows/deploy.yml`.
  // O toggle de auto-deploy não é expressável neste DSL (beta): ele é conferido
  // no painel e registrado no runbook, junto do alerta de que `railway config
  // plan` deve ser revisado antes de qualquer `apply`.
  const api = service("api", {
    source: github("Sharpista/LoLCoach", { branch: "main" }),
    build: {
      builder: "DOCKERFILE",
      dockerfilePath: "Dockerfile",
    },

    // healthcheckPath da plataforma: `/health` (liveness, sem dependência de
    // banco/Riot/Gemini - spec R3.1). A plataforma consulta apenas na subida do
    // deploy; timeout padrão de 300s (R3.1).
    healthcheck: "/health",
    healthcheckTimeout: 300,

    // Design Decisão A / spec: uma réplica no MVP. A região (US East Metal,
    // `us-east4-eqdc4a`) é selecionada no painel e está registrada no runbook:
    // o placement pelo DSL é beta e não pode ser validado sem a CLI.
    replicas: 1,

    // Somente nomes aqui. Não secretos com valor explícito; o secret é
    // preservado no lado da plataforma (R11.3).
    env: {
      ASPNETCORE_ENVIRONMENT: "Production",
      ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true",
      ConnectionStrings__LoLCoach: preserve(),
    },
  });

  return project("sparkling-dream", {
    resources: [api],
  });
});
