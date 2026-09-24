---
id: 007
name: swagger
status: DONE
depends_on: [001]
---

# Spec 007 - OpenAPI e Swagger UI

## Objetivo

Disponibilizar um documento OpenAPI e uma interface Swagger UI para a API existente do LoLCoach, sem alterar contratos funcionais nem expor segredos.

## Requisitos

1. A API deve gerar OpenAPI v3 em `GET /swagger/v1/swagger.json`.
2. Em ambiente `Development`, a Swagger UI deve estar disponível em `/swagger`.
3. Em ambientes diferentes de `Development`, Swagger somente pode ser exposto quando `Swagger:Enabled=true` (ou `Swagger__Enabled=true` por variável de ambiente).
4. O documento deve descrever `POST /api/players/search` com request JSON camelCase contendo `gameName`, `tagLine` e `region`, incluindo as validações vigentes: os três campos são required; `gameName` tem limites 3–16; `tagLine` tem limites 2–5 e pattern alfanumérico ASCII com espaços de borda permitidos pelo trim do validator; `region` enumera as plataformas suportadas. O pattern Unicode de `gameName` não é emitido por incompatibilidade entre regex .NET e OpenAPI.
5. A operação deve declarar respostas 200, 400, 404, 429 e 503. Respostas de erro devem usar `application/problem+json` e ProblemDetails; 400 deve representar os erros de validação.
6. A documentação não deve conter connection strings, Riot API key, Gemini key ou exemplos de secrets.
7. Testes devem verificar a geração do documento e a presença da operação principal sem banco, Riot API ou secrets.

## Contrato documentado

- `POST /api/players/search`, `Content-Type: application/json`.
- Request: `gameName` obrigatório, trim de 3-16 caracteres, letras/dígitos/espaços Unicode; `tagLine` obrigatório, trim de 2-5 caracteres alfanuméricos; `region` obrigatório e plataforma LoL suportada (`br1`, `euw1`, `na1`, `kr`, etc.).
- 200: `PlayerDto` em camelCase.
- 400: `HttpValidationProblemDetails` com `errors`.
- 404: jogador não encontrado (`ProblemDetails`).
- 429: limite Riot (`ProblemDetails` e `Retry-After` quando disponível).
- 503: Riot indisponível (`ProblemDetails`).

## Fora do escopo

- Autenticação da UI.
- Alteração de endpoints, status HTTP, regras de negócio, banco, migrations ou integrações.

## Open Questions

Nenhuma. A solução usa Swashbuckle.AspNetCore 9.0.6, compatível com o target `net10.0`, para gerar JSON e UI.
