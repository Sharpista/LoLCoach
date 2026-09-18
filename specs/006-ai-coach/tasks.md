# Tasks 006 - AI Coach

- [x] Criar `IAiCoach`.
  - Arquivo: `backend/src/LoLCoach.Api/Application/IAiCoach.cs`
- [x] Criar `CoachAnalysisInput`.
  - Arquivo: `backend/src/LoLCoach.Api/Application/AiCoachModels.cs`
- [x] Criar `CoachReport`.
  - Arquivo: `backend/src/LoLCoach.Api/Application/AiCoachModels.cs`
- [x] Criar adaptador do provedor escolhido.
  - Arquivo: `backend/src/LoLCoach.Api/Infrastructure/GeminiAiCoach.cs`
  - Configuração: `AiCoach` options, `AiCoach__GeminiApiKey` ou `GEMINI_API_KEY`; nenhum segredo em código fonte.
- [x] Criar prompt restrito a dados fornecidos.
  - Arquivo: `backend/src/LoLCoach.Api/Infrastructure/GeminiAiCoach.cs`
- [x] Validar/estruturar resposta do LLM.
  - Arquivo: `backend/src/LoLCoach.Api/Infrastructure/GeminiAiCoach.cs`
- [x] Implementar timeout e tratamento de falha.
  - Arquivos: `backend/src/LoLCoach.Api/Infrastructure/AiCoachOptions.cs`, `backend/src/LoLCoach.Api/Application/PerformanceAnalysisService.cs`
- [x] Implementar fallback determinístico.
  - Arquivo: `backend/src/LoLCoach.Api/Application/DeterministicCoachReportFactory.cs`
- [x] Criar endpoint/integração no dashboard.
  - Integração no endpoint existente `GET /api/players/{id}/analysis`, com `coachReport` e `recommendations` no DTO.
- [x] Criar testes com fake/mock do provedor.
  - Arquivos: `backend/tests/LoLCoach.Tests/PerformanceAnalysisEndpointTests.cs`, `backend/tests/LoLCoach.Tests/DeterministicCoachReportFactoryTests.cs`

## Verificação

- Build/testes executados nesta implementação combinada 005+006:
  - `dotnet build backend/LoLCoach.slnx` — sucesso, 0 warnings, 0 errors.
  - `dotnet test backend/tests/LoLCoach.Tests/LoLCoach.Tests.csproj --no-build --logger "console;verbosity=normal"` — sucesso, 75 testes passados.
  - `dotnet format backend/LoLCoach.slnx --verify-no-changes` — sucesso.
- QA independente: aprovado (cenários de fallback determinístico, timeout, resposta inválida e ausência de chave). Parecer registrado a partir do relatório de execução da task; a evidência reproduzível está em `evidence/verification-2026-09-18.md`; a validação visual/manual continua sendo responsabilidade da task quando aplicável.
- Code review independente: aprovado, sem achados bloqueantes ou importantes. Mesma origem do parecer de QA.
- CI `backend-ci`: SUCCESS no head de implementação `6852027` (eventos `push` e `pull_request`).
- Verificação independente do agente `github-profile`: `dotnet build` 0 warnings / 0 errors e `dotnet test` 75/75 passando, reproduzidos fora do fluxo de implementação.
- Promoção: `status: READY -> DONE` em `spec.md`, após QA e code review aprovados, conforme `AGENTS.md` e `orchestrator/ORCHESTRATOR.md`.
