# Tasks 005 - Recommendations

- [x] Criar modelo `Recommendation`.
  - Arquivo: `backend/src/LoLCoach.Api/Analytics/Recommendations/Recommendation.cs`
- [x] Criar `RecommendationEngine`.
  - Arquivo: `backend/src/LoLCoach.Api/Analytics/Recommendations/RecommendationEngine.cs`
- [x] Mapear LOW_CS.
- [x] Mapear HIGH_DEATHS.
- [x] Mapear LOW_VISION.
- [x] Mapear problemas de combate.
- [x] Mapear inconsistência quando aplicável.
- [x] Criar testes determinísticos.
  - Arquivo: `backend/tests/LoLCoach.Tests/RecommendationEngineTests.cs`
- [x] Incluir recomendações no endpoint de análise.
  - Arquivos: `backend/src/LoLCoach.Api/Application/PerformanceAnalysisDto.cs`, `backend/src/LoLCoach.Api/Application/PerformanceAnalysisService.cs`

## Verificação

- Build/testes executados nesta implementação combinada 005+006:
  - `dotnet build backend/LoLCoach.slnx` — sucesso, 0 warnings, 0 errors.
  - `dotnet test backend/tests/LoLCoach.Tests/LoLCoach.Tests.csproj --no-build --logger "console;verbosity=normal"` — sucesso, 75 testes passados.
  - `dotnet format backend/LoLCoach.slnx --verify-no-changes` — sucesso.
- QA independente: aprovado em 15 cenários. Parecer registrado a partir do relatório de execução da task; a evidência reproduzível está em `evidence/verification-2026-09-18.md`; a validação visual/manual continua sendo responsabilidade da task quando aplicável.
- Code review independente: aprovado, sem achados bloqueantes ou importantes. Mesma origem do parecer de QA.
- CI `backend-ci`: SUCCESS no head de implementação `6852027` (eventos `push` e `pull_request`).
- Verificação independente do agente `github-profile`: `dotnet build` 0 warnings / 0 errors e `dotnet test` 75/75 passando, reproduzidos fora do fluxo de implementação.
- Promoção: `status: READY -> DONE` em `spec.md`, após QA e code review aprovados, conforme `AGENTS.md` e `orchestrator/ORCHESTRATOR.md`.
