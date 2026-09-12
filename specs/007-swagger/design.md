# Design 007 - OpenAPI e Swagger UI

## Decisão

Usar `Swashbuckle.AspNetCore` 9.0.6. O projeto não possui geração/UI OpenAPI configurada; `Microsoft.AspNetCore.OpenApi` sozinho gera descrição, mas não fornece a UI Swagger requerida. Swashbuckle entrega ambos em uma dependência explícita e funciona com ASP.NET Core/.NET 10 sem introduzir alteração no pipeline de negócio.

## Pipeline

```text
Controllers/ApiExplorer -> Swashbuckle -> /swagger/v1/swagger.json
                                             -> /swagger/index.html
```

`AddSwaggerGen` registra o documento v1. `UseSwagger` e `UseSwaggerUI` são ativados por padrão somente em `Development`; fora desse ambiente, exigem `Swagger:Enabled=true`. A UI não tem autenticação neste escopo: habilitá-la fora de Development exige decisão operacional explícita e deve ser restrito por rede/proxy em ambientes compartilhados.

## Metadados do endpoint

`PlayersController` declara `Consumes`, `Produces` e `ProducesResponseType` para manter a documentação alinhada ao contrato atual. Comentários XML descrevem campos e validações sem duplicar regras no controller. A validação continua em `SearchPlayerValidator`; atributos Swagger são apenas metadados.

A resposta 400 usa `HttpValidationProblemDetails`; 404, 429 e 503 usam `ProblemDetails`, todos com `application/problem+json`. O status 429 pode incluir `Retry-After` no runtime, como já definido no contrato.

## Segurança

Nenhuma chave, connection string ou payload externo é incluído no documento. O exemplo de request é derivado do schema e contém apenas valores sintéticos. Swagger fora de Development fica desligado por padrão; a configuração explícita deve ser tratada como risco de exposição de superfície e contratos.

## Testes

`HostTests` sobe `WebApplicationFactory` com uma connection string sintética que não é usada, busca o JSON e verifica a operação POST, request body e respostas. Nenhuma dependência PostgreSQL/Riot é necessária.

## Open Questions

Nenhuma.
