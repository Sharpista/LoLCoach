# Tasks 001 - Busca de Jogador Riot

Estado: READY (Open Questions resolvidas em spec.md → Decisões). Base de persistência já entregue e verificada.

## Backend

- [x] Criar entidade `Player`.
- [x] Criar `SearchPlayerCommand`.
- [ ] Criar `SearchPlayerValidator`.
- [ ] Criar `SearchPlayerHandler`.
- [x] Criar `PlayerDto`.
- [ ] Criar `IRiotAccountClient` e implementação.
- [ ] Configurar `HttpClientFactory`.
- [ ] Tratar 404, 429 e 5xx da Riot.
- [x] Criar `IPlayerRepository` e implementação.
- [x] Configurar EF Core e índice único em PUUID.
- [x] Criar migration.
- [ ] Criar endpoint `POST /api/players/search`.
- [ ] Adicionar logs relevantes.

## Testes

- [ ] Testar entrada inválida.
- [ ] Testar jogador novo.
- [ ] Testar jogador existente.
- [ ] Testar Riot 404.
- [ ] Testar Riot 429.
- [ ] Testar endpoint de integração.

## Frontend

- [ ] Criar feature `player-search`.
- [ ] Criar formulário e validações.
- [ ] Criar serviço HTTP.
- [ ] Implementar loading e erros.
- [ ] Redirecionar ao dashboard.
