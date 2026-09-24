# Tasks 001 - Busca de Jogador Riot

Estado: DONE — backend e frontend concluídos, QA e code review aprovados; PR #10 merged em develop (SHA 7a33017).

## Backend

- [x] Criar entidade `Player`.
- [x] Criar `SearchPlayerCommand`.
- [x] Criar `SearchPlayerValidator`.
- [x] Criar `SearchPlayerHandler`.
- [x] Criar `PlayerDto`.
- [x] Criar `IRiotAccountClient` e implementação.
- [x] Configurar `HttpClientFactory`.
- [x] Tratar 404, 429 e 5xx da Riot.
- [x] Criar `IPlayerRepository` e implementação.
- [x] Configurar EF Core e índice único em PUUID.
- [x] Criar migration.
- [x] Criar endpoint `POST /api/players/search`.
- [x] Adicionar logs relevantes.

## Testes

- [x] Testar entrada inválida.
- [x] Testar jogador novo.
- [x] Testar jogador existente.
- [x] Testar Riot 404.
- [x] Testar Riot 429.
- [x] Testar endpoint de integração.

## Frontend

- [x] Criar feature `player-search`.
- [x] Criar formulário e validações.
- [x] Criar serviço HTTP.
- [x] Implementar loading e erros.
- [x] Navegar após sucesso para `/player/:id` (detalhe do jogador) — decisão registrada na spec.

## Validação funcional (QA — qualidade)

- [x] Aprovado. Contrato HTTP íntegro: 400 validação (`errors` camelCase), 503 sem chave (sem vazar), 405 método errado. 45/45 testes. Sem bugs funcionais.

## Revisão técnica (code-reviewer)

- [x] Aprovado com ressalvas. 0 bloqueante (🔴/🟡), 4 ressalvas 🟢 não-bloqueantes.

## Ressalvas 🟢 (backlog, não-bloqueantes)

1. `backend/README.md` desatualizado — ainda afirma "POST /api/players/search não existe" e "spec 001 BLOCKED"; risco de confusão operacional (prioridade recomendada).
2. Body vazio gera `errors` com chave `""`/`"command"` (default `[ApiController]` do ASP.NET; cosmético).
3. `RiotAccountClient` mapeia 401/403 da Riot para 503 genérico — sugerir `LogError` com status p/ diagnóstico.
4. `Retry-After` interpretado só como segundos inteiros (correto hoje; ressalva documental).

## Verificação (orquestrador)

- [x] `dotnet build` 0 erros/0 warnings.
- [x] `dotnet test` 45/45 passando (Docker/Testcontainers).
- [x] `dotnet format --verify-no-changes` limpo.
