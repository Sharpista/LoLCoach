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
- QA independente: pendente após implementação e verificação das specs 005+006.
