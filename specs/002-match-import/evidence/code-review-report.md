# Code Review Report — Spec 002: Match Import

**Data:** 2026-09-16T13:30:00Z
**Branch:** review/formal-spec-002
**Commit SHA (head):** 2bd3093
**Reviewer:** code-reviewer
**Worktree:** /home/alexandre/LoLSaas/.worktrees/t_5b3a30b0
**PR de implementação:** #12 (`feat(match-import)` — commit `9ea9b70`, merge `b875658` em `develop`)

---

## Resumo

Revisão técnica formal da Spec 002 — Importação de Partidas. O escopo entregue (28
arquivos, ~1340 linhas) implementa o fluxo `POST /api/players/{id}/matches/sync`,
a normalização via PUUID, a persistência com índice único em `RiotMatchId` e o
tratamento de falhas individuais/Riot rate-limit. Build reproduzido com 0
warnings/0 errors e as 10 suítes da spec (2+2+2+4) passando, contra a evidência
QA em `1612d6b`. A implementação é consistente com o design (`specs/002-match-import/design.md`)
e a rastreabilidade com `tasks.md` está completa.

**Veredito: APROVADO**

---

## Ambiente e Comandos Executados

| Item | Valor |
|------|-------|
| .NET | 10.0.12 |
| Branch | review/formal-spec-002 |
| HEAD | 2bd3093 |
| Worktree | /home/alexandre/LoLSaas/.worktrees/t_5b3a30b0 |
| Commits ahead de origin/develop | 0 |

**Build reproduzido:**

```
$ cd backend && dotnet build --no-incremental
…
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.25
```

**Testes da Spec 002 reproduzidos:**

```
$ dotnet test --no-build --filter "FullyQualifiedName~MatchNormalizer|FullyQualifiedName~SyncPlayerMatches|FullyQualifiedName~MatchRepository|FullyQualifiedName~MatchSync"
…
Passed LoLCoach.Tests.MatchRepositoryTests.New_match_roundtrips_player_match_fields_in_PostgreSQL [5 s]
Passed LoLCoach.Tests.MatchSyncEndpointTests.Sync_valid_player_returns_import_counters_and_persists_match [7 s]
Passed LoLCoach.Tests.MatchSyncEndpointTests.Sync_unknown_player_returns_404_problem_details [345 ms]
…
Test Run Successful.
Total tests: 10
     Passed: 10
```

(Build + testes em isolamento — sem chamadas reais à Riot API; `PostgresFixture`
com Testcontainers `postgres:16-alpine` para os testes de banco.)

---

## Arquivos Revisados

### Domain
- `backend/src/LoLCoach.Api/Domain/Match.cs`
- `backend/src/LoLCoach.Api/Domain/PlayerMatch.cs`
- `backend/src/LoLCoach.Api/Domain/Player.cs` (apenas para validar `PlayerMatches` e a navegação reversa)

### Application
- `backend/src/LoLCoach.Api/Application/IRiotMatchClient.cs` (interface + DTOs Riot)
- `backend/src/LoLCoach.Api/Application/RiotRegions.cs` (mapeamento platform → routing)
- `backend/src/LoLCoach.Api/Application/MatchImportExceptions.cs`
- `backend/src/LoLCoach.Api/Application/IMatchRepository.cs`
- `backend/src/LoLCoach.Api/Application/MatchNormalizer.cs`
- `backend/src/LoLCoach.Api/Application/IMatchNormalizer.cs`
- `backend/src/LoLCoach.Api/Application/SyncPlayerMatchesCommand.cs`
- `backend/src/LoLCoach.Api/Application/SyncPlayerMatchesHandler.cs`
- `backend/src/LoLCoach.Api/Application/PlayerSearchExceptions.cs` (para validar `RiotRateLimitedException`/`RiotServiceUnavailableException`)

### Infrastructure
- `backend/src/LoLCoach.Api/Infrastructure/RiotMatchClient.cs`
- `backend/src/LoLCoach.Api/Infrastructure/MatchRepository.cs`
- `backend/src/LoLCoach.Api/Infrastructure/PlayerDbContext.cs` (apenas o bloco `Match`/`PlayerMatch`)
- `backend/src/LoLCoach.Api/Infrastructure/Migrations/20260914165003_AddMatches.cs`
- `backend/src/LoLCoach.Api/Infrastructure/GlobalExceptionHandler.cs` (mapeamento de exceções relevantes)
- `backend/src/LoLCoach.Api/Program.cs` (DI: `IRiotMatchClient`, `IMatchNormalizer`, `SyncPlayerMatchesHandler`)

### Controllers
- `backend/src/LoLCoach.Api/Controllers/PlayersController.cs` (apenas `POST /api/players/{id}/matches/sync`)

### Testes
- `backend/tests/LoLCoach.Tests/MatchNormalizerTests.cs`
- `backend/tests/LoLCoach.Tests/MatchRepositoryTests.cs`
- `backend/tests/LoLCoach.Tests/MatchSyncEndpointTests.cs`
- `backend/tests/LoLCoach.Tests/SyncPlayerMatchesHandlerTests.cs`
- `backend/tests/LoLCoach.Tests/PostgresFixture.cs`

---

## Checklist Técnico

### 1. Correção

| Item | Resultado |
|------|-----------|
| Lógica cobre os 4 critérios de aceitação | ✅ |
| Identificação por PUUID com `SingleOrDefault` | ✅ — retorna `null` se ausente (CA3) |
| Truncamento para 20 partidas (client + handler) | ✅ — `Math.Clamp(count, 1, 20)` em `RiotMatchClient.cs:19`; `.Take(RecentMatchLimit)` em `SyncPlayerMatchesHandler.cs:25` |
| Dupla proteção contra duplicação (aplicação + DB) | ✅ — `ExistsByRiotMatchIdAsync` antes + `ix_matches_riot_match_id` único |
| Continuidade após falha individual | ✅ — `try/catch` em `SyncPlayerMatchesHandler.cs:33-57` |
| Cancelamento respeitado | ✅ — `OperationCanceledException` quando `cancellationToken.IsCancellationRequested` re-thrown |
| Off-by-one, comparação de referência vs valor | ✅ — sem achados |

### 2. Segurança

| Item | Resultado |
|------|-----------|
| Input não confiável | ✅ — `Uri.EscapeDataString` em `puuid` e `matchId` (`RiotMatchClient.cs:21,43`) |
| AuthN/AuthZ | ✅ — endpoint exige `id:guid` válido; inexistente → 404 via `PlayerNotFoundException` → `GlobalExceptionHandler` |
| Segredos em código | ✅ — `Riot:ApiKey` via `IConfiguration` (`RiotMatchClient.cs:60`), nunca commitada |
| Dados sensíveis em logs | ✅ — `LogWarning` inclui apenas `matchId`, `puuid`, `playerId` (todos identificadores opacos), sem tokens, PII pessoal ou payload |
| Mensagens de erro para o cliente | ✅ — `ProblemDetails` com mensagens genéricas (`Player not found`, `Riot service unavailable`); stack trace fica em servidor |
| Concatenação de query insegura | ✅ — uso de `System.Text.Json` + DTOs tipados; sem SQL cru em runtime |

### 3. Testes

| Item | Resultado |
|------|-----------|
| Cobertura dos 4 critérios de aceitação | ✅ — 4 suítes da spec com 10 testes |
| Casos negativos | ✅ — PUUID ausente, player desconhecido, Riot 429/5xx, duplicação |
| `Sync_continues_after_individual_match_failure` | ✅ — verifica ordem de processamento e contadores `1/0/1` |
| Teste de DB real | ✅ — `MatchRepositoryTests` com Testcontainers `postgres:16-alpine` |
| Teste de endpoint com WebApplicationFactory + DB real | ✅ — `MatchSyncEndpointTests` |
| `FakeRiotMatchClient` em ambos os testes | ✅ — sem dependência de chave Riot |
| Acoplamento a implementação | ⚪ — fakes cobrem comportamento, não implementação interna |

### 4. Design e Manutenibilidade

| Item | Resultado |
|------|-----------|
| Responsabilidade por camada | ✅ — Controller fino; handler concentra orquestração; normalizer é puro; repositório é EF Core |
| Domínio sem dependência de infraestrutura | ✅ — `Match`/`PlayerMatch` com setters `private`, factory no construtor |
| Aggregate root bem definido | ✅ — `Match` é root; `PlayerMatch.AttachToMatch` é `internal` |
| Injeção de `ILogger<T>` | ✅ — handler recebe `ILogger<SyncPlayerMatchesHandler>` |
| Configuração DI | ✅ — `IRiotMatchClient` registrado com `AddHttpClient<>` (timeout 10 s), handler `Scoped` |
| Migration nova sem alterar anteriores | ✅ — `20260914165003_AddMatches` adiciona; `Down()` reversível |
| Configurações via `IConfiguration` | ✅ — `Riot:ApiKey` e `ConnectionStrings:LoLCoach` |

### 5. Performance

| Item | Resultado |
|------|-----------|
| N+1 | ✅ — não há loops com queries; cada `ExistsByRiotMatchIdAsync` é uma única chamada `AnyAsync` com `AsNoTracking` |
| `AsNoTracking` em leitura | ✅ — `MatchRepository.cs:10,18` |
| Tracking desnecessário | ⚪ — `AddAsync` é o ponto correto para tracking; nenhum vazamento óbvio |
| HttpClient por injeção | ✅ — `IHttpClientFactory` via `AddHttpClient<IRiotMatchClient>()` (`Program.cs:68`) |
| Concurrency no handler | ⚠️ — ver achado 🟡 A1 abaixo |
| `DbChangeTracker.Clear()` após `DbUpdateException` | ✅ — `MatchRepository.cs:32` (bom: evita manter entidades falhas no contexto para o próximo `SaveChangesAsync` do loop) |

### 6. Operabilidade

| Item | Resultado |
|------|-----------|
| Logs estruturados em pontos críticos | ✅ — `LogWarning` para PUUID ausente, falha individual, `LogWarning(exception, ...)` com contexto |
| `Retry-After` propagado | ✅ — `RiotRateLimitedException.RetryAfterSeconds` → header `Retry-After` em `GlobalExceptionHandler.cs:50-53` |
| Status codes corretos | ✅ — 200 (sync), 404 (player inexistente), 400 (validação herdada de search), 429, 503 |
| Swagger/OpenAPI | ✅ — `ProducesResponseType` decorando o endpoint; XML comments presentes |
| Cancellation token propagado | ✅ — toda chamada externa recebe `cancellationToken` |
| Timeout HTTP configurado | ✅ — 10 s em `Program.cs:70` |

### 7. Integração (escopo desta spec é backend-only)

N/A — a Spec 002 é puramente backend (sincronização via endpoint exposto). Nenhuma
alteração de frontend foi feita neste PR. O consumo pelo frontend acontece nas
Specs 003/004, fora do escopo.

---

## Achados

### 🟡 A1 — Concorrência: existência verificada e inserção podem colidir entre requests

- **Local:** `backend/src/LoLCoach.Api/Application/SyncPlayerMatchesHandler.cs:27-47` e `backend/src/LoLCoach.Api/Infrastructure/MatchRepository.cs:9-35`
- **Descrição:** A verificação `ExistsByRiotMatchIdAsync` (linha 27) e a inserção em `AddAsync`/`SaveChangesAsync` (linhas 44-45) não estão na mesma transação e não usam lock. Se dois requests de sync concorrentes para o mesmo jogador (ou para jogadores diferentes que importam a mesma partida Riot) chegarem quase simultaneamente, ambos podem passar pelo `Exists` e tentar inserir. Nesse cenário a defesa é o índice único `ix_matches_riot_match_id` (corretamente coberto por `Database_itself_rejects_duplicate_riot_match_id`), que vai disparar `DbUpdateException` e ser contado como `failed` em vez de duplicar.
- **Impacto:** Nenhum em termos de **integridade de dados** (o índice único é a salvaguarda real e está provado pelo teste `Database_itself_rejects_duplicate_riot_match_id`). O efeito observável é que uma corrida real reportaria `failed++` espúrio ao chamador. Para o MVP, com um único jogador por usuário e baixa probabilidade de clique duplo, isso é aceitável. Vale como follow-up porque a modelagem de `SyncPlayerMatchesResult` torna `failed` indistinguível de falha real de Riot.
- **Recomendação:** Ou (a) envolver o loop em `db.Database.BeginTransactionAsync()` e serializar via `SELECT ... FOR UPDATE` em `matches`; ou (b) no `catch (DbUpdateException)`, inspecionar `InnerException` para `PostgresErrorCodes.UniqueViolation` e recontar como `skipped++` em vez de `failed++` — esta última é a mudança mínima e barata, e o teste `Database_itself_rejects_duplicate_riot_match_id` já está pronto para ser estendido. Não bloqueia a aprovação.
- **Responsável sugerido:** `dev-backend` (decisão de produto: `skipped` vs `failed` em corrida)
- **Severidade:** Sugestão — melhoria recomendada

### 🟡 A2 — Truncamento silencioso em `Math.Clamp` sem log

- **Local:** `backend/src/LoLCoach.Api/Infrastructure/RiotMatchClient.cs:19`
- **Descrição:** Se algum chamador passar `count > 20`, o `Math.Clamp` reduz silenciosamente para 20 sem nenhum log. A constante `MaxMatchCount = 20` é a razão de ser deste clamp; o handler hoje sempre passa `RecentMatchLimit = 20`, então o clamp é tecnicamente defensivo. O ponto vale registrar: se a constante evoluir no handler, a do client não acompanha (são duplicadas).
- **Impacto:** Mínimo — comportamento atual está correto. Risco de divergência futura se `RecentMatchLimit` mudar.
- **Recomendação:** Considerar expor `RecentMatchLimit` como `public const int` em um único lugar (`IRiotMatchClient` ou uma classe estática `MatchImportConstants`) e reutilizar no handler. Não bloqueia.
- **Responsável sugerido:** `dev-backend` (refatoração menor)
- **Severidade:** Sugestão

### 🟡 A3 — `Logger.LogWarning` com `puuid` pode ser ruidoso em alta cardinalidade

- **Local:** `backend/src/LoLCoach.Api/Application/SyncPlayerMatchesHandler.cs:40`
- **Descrição:** Quando o normalizador retorna `null` (PUUID ausente na partida — raro mas possível em partidas custom/ARAM com re-rolls), o handler registra `LogWarning` com `matchId` e `puuid`. PUUID é identificador opaco, então não há PII, mas a mensagem pode poluir logs em massa se acontecer para muitos jogadores simultaneamente. Para o MVP é apenas um sinal observacional, não um bug.
- **Impacto:** Operacional baixo. Não é PII; é apenas ruído potencial.
- **Recomendação:** Considerar mover para `LogInformation` ou agregar por jogador com `LogWarning("Match without target player for {PlayerCount} players today", count)`. Não bloqueia.
- **Responsável sugerido:** `dev-backend`
- **Severidade:** Sugestão opcional

### 🟢 Positivos

1. **Dupla camada de defesa contra duplicação** (linha de aplicação + índice único no banco) é a decisão correta para importações idempotentes. O teste de DB real (`Database_itself_rejects_duplicate_riot_match_id`) é prova forte de que a defesa não foi esquecida.
2. **Aggregate root bem encapsulado**: `Match.AddPlayerMatch` é a única forma de anexar `PlayerMatch`, com `AttachToMatch` `internal` para impedir uso externo. Imutabilidade dos setters reforça.
3. **Tratamento de exceções granular** no handler — apenas as 4 exceções esperadas (`RiotRateLimitedException`, `RiotServiceUnavailableException`, `InvalidOperationException`, `DbUpdateException`) são capturadas; `OperationCanceledException` é re-thrown corretamente. Cobertura de testes boa para cada caminho.
4. **`Retry-After` propagado** desde `RiotMatchClient.ThrowForFailure` até o header HTTP via `GlobalExceptionHandler` — sem isso o cliente seria forçado a fazer polling cego.
5. **Testcontainers no `PostgresFixture`** dá confiança real sobre o índice único e o cascade delete — exatamente onde EF Core costuma esconder bugs entre provedores.
6. **`HttpCompletionOption.ResponseHeadersRead`** em `RiotMatchClient.SendAsync` evita carregar corpo de erro desnecessariamente em respostas 4xx/5xx.
7. **DTOs da Riot isolados em `IRiotMatchClient.cs`** — domínio não conhece `RiotMatchDetails`/`RiotMatchInfo`/`RiotMatchParticipant`. Boa fronteira.
8. **Rastreabilidade de `tasks.md`** com referências arquivo-por-linha; o PR #12 incluiu o commit `9ea9b70` cuja mensagem é detalhada e aponta inclusive para o worktree original.

---

## Fora de escopo (follow-ups sugeridos)

Estes itens existem no código mas estão fora do escopo desta spec; não devem
bloquear esta aprovação, mas vale registrar para issues:

1. **`PlayerMetricsCalculator` é Singleton e consome `IMatchRepository`?** Não — verificado, é singleton mas não depende de `IMatchRepository`. OK.
2. **`PerformanceAnalysisService` chama `matchRepository.ListPlayerMatchesForAnalysisAsync`** — pertence à Spec 003 e está fora do escopo desta review.
3. **`Analytics/**` inteiro** foi listado no escopo do card mas pertence à Spec 003 (merge `b875658`). O diff do PR #12 mistura Spec 002 + Spec 003 num só PR (vide mensagem do commit `9ea9b70`: "Baseline da Spec 002 incorporado a esta branch porque o motor de analytics da Spec 003 (GET /api/players/{id}/analysis) consome PlayerMatch persistido"). Esta mistura é justificável e foi aceita pelo merge em `develop`, mas merece nota de rastreabilidade para quem ler o histórico.
4. **`MatchRepository.ListPlayerMatchesForAnalysisAsync`** retorna `Include(playerMatch => playerMatch.Match)` — uma análise de N+1 está descartada por construção, mas considere avaliar `AsSplitQuery` se a lista de partidas crescer significativamente. Fora do escopo desta review.

---

## Decisão

**APROVADO**

A Spec 002 — Importação de Partidas está pronta para fechamento. Build limpo,
testes passando (10/10 da spec; 75/75 totais conforme QA em `1612d6b`), critérios
de aceite atendidos, sem bloqueantes. Os 3 achados são sugestões que podem ser
endereçadas em PRs de melhorias sem urgência.

---

## Checklist Final

- [x] Build compila (0 warnings / 0 errors reproduzido)
- [x] Testes executados e passando (10/10 da spec reproduzido)
- [x] Critérios de aceite atendidos (CA1, CA2, CA3, CA4)
- [x] Sem bug crítico ou alto
- [x] Sem falha de segurança evidente
- [x] Padrão do projeto respeitado (mesma estrutura da Spec 001)
- [x] Instruções de teste informadas (comandos reais acima)
- [x] Integração backend↔frontend fora do escopo desta spec
- [x] QA considerado (`t_59fd42b9` em `1612d6b` — APROVADO com 75/75)
- [x] Limitações ambientais separadas de defeitos (Postgres via Testcontainers,
      Riot API via `FakeRiotMatchClient` — ambas escolhas deliberadas e corretas)
- [x] Arquivo versionado: `specs/002-match-import/evidence/code-review-report.md`
- [x] Commit SHA informado: `2bd3093`

---

## Resposta para o `orquestrador`

**Status da revisão:** Aprovado.

**Recomendação:** Pode seguir para finalização. A Spec 002 está pronta para o
fechamento documental. Não há código a corrigir; os 3 achados são sugestões de
melhoria que viram tasks filhos no board se o `orquestrador` decidir endereçá-los
(A1 sobre corrida/duplicação é o mais valioso entre eles).
