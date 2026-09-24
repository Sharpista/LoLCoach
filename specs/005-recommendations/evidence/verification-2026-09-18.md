# Verificação reproduzível — 2026-09-18

Escopo: validação da implementação de recomendações e AI Coach (Specs 005 e 006) após a adoção do protocolo de evidências.

## Comandos

- `dotnet build backend/LoLCoach.slnx --no-restore` — **passou**, 0 warnings, 0 errors.
- `dotnet test backend/tests/LoLCoach.Tests/LoLCoach.Tests.csproj --no-build --logger "console;verbosity=minimal"` — **passou**, 75/75 testes.
- `npm test -- --watch=false` (em `frontend/`) — **passou**, 24/24 testes em 5 arquivos.

## Limites

Esta verificação cobre build e testes automatizados. Não substitui uma validação visual no navegador nem um canário de produção; esses passos devem ser registrados na task de qualquer mudança de UI ou deploy.
