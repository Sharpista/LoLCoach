# Code Review Report — Spec 003 Analytics Engine

**Data:** 2026-09-16T15:25:00Z
**Revisor:** Code Reviewer (Hermes)
**Branch:** review/formal-spec-003
**Commit SHA revisado:** 2bd3093e188325f9a13c7c9c0559cf41cdefea0e (= origin/develop, merge base)
**Veredito:** APROVADO

---

## Resumo

A Spec 003 (Analytics Engine) foi entregue e mantida na origem `develop` (HEAD
`2bd3093`). A revisão independente confirmou que o código atende aos três critérios
de aceite da spec — determinismo, ausência de números inventados e evidência
mensurável — e que a identificação primária de problemas continua 100%
determinística e sem dependência de LLM. Build limpo (0 warnings / 0 errors) e
12/12 testes da spec passando foram reproduzidos; a suíte completa (75/75)
também passa, sem regressão.

Nenhum defeito bloqueante foi identificado. Há uma lacuna de cobertura de teste
importante (falta provar o critério "mesma coleção produz mesmo resultado" no
pipeline completo de análise) e dois nitpicks de design que não comprometem o
veredito. Recomendo aprovação.

---

## 1. Comandos executados e resultados reais

Todos os comandos foram executados a partir do commit
`2bd3093e188325f9a13c7c9c0559cf41cdefea0e`, em worktree isolado
(`/tmp/review-spec-003-build`) para evitar poluir o worktree de revisão com
artefatos de build.

| # | Comando | Resultado |
|---|---|---|
| 1 | `git rev-parse HEAD` (no review worktree) | `2bd3093e188325f9a13c7c9c0559cf41cdefea0e` |
| 2 | `git rev-parse origin/develop` | `2bd3093e188325f9a13c7c9c0559cf41cdefea0e` (= HEAD) |
| 3 | `dotnet build -c Debug` em `/tmp/review-spec-003-build` | **Build succeeded. 0 Warning(s), 0 Error(s)** (25.78s) |
| 4 | `dotnet test -c Debug --no-build --filter PlayerMetricsCalculatorTests\|PerformanceAnalyzerTests\|PerformanceAnalysisEndpointTests` | **Passed: 12, Failed: 0, Skipped: 0, Total: 12** (19s) |
| 5 | `dotnet test -c Debug --no-build` (suíte completa) | **Passed: 75, Failed: 0, Skipped: 0, Total: 75** (46s) |

Os 12 testes da spec se decompõem em:
- `PlayerMetricsCalculatorTests` (2): `Same_matches_produce_same_metrics_and_calculate_rates`, `Empty_collection_returns_zero_metrics`.
- `PerformanceAnalyzerTests` (6): um teste por analyzer (Farming/Death/Vision/Combat/Consistency/Champion).
- `PerformanceAnalysisEndpointTests` (4): integração via `WebApplicationFactory<Program>` + `PostgresFixture` (testcontainers).

A suíte completa de 75 testes inclui ainda specs 001 (player search), 002 (match
import), 005 (recommendations) e 006 (AI Coach).

---

## 2. Arquivos revisados

### Código de produção (escopo da Spec 003)

- `backend/src/LoLCoach.Api/Analytics/Insights/InsightType.cs`
- `backend/src/LoLCoach.Api/Analytics/Insights/InsightSeverity.cs`
- `backend/src/LoLCoach.Api/Analytics/Insights/Insight.cs`
- `backend/src/LoLCoach.Api/Analytics/Metrics/PlayerMetrics.cs`
- `backend/src/LoLCoach.Api/Analytics/Metrics/PlayerMetricsCalculator.cs`
- `backend/src/LoLCoach.Api/Analytics/Analyzers/IPerformanceAnalyzer.cs`
- `backend/src/LoLCoach.Api/Analytics/Analyzers/FarmingAnalyzer.cs`
- `backend/src/LoLCoach.Api/Analytics/Analyzers/DeathAnalyzer.cs`
- `backend/src/LoLCoach.Api/Analytics/Analyzers/VisionAnalyzer.cs`
- `backend/src/LoLCoach.Api/Analytics/Analyzers/CombatAnalyzer.cs`
- `backend/src/LoLCoach.Api/Analytics/Analyzers/ConsistencyAnalyzer.cs`
- `backend/src/LoLCoach.Api/Analytics/Analyzers/ChampionAnalyzer.cs`
- `backend/src/LoLCoach.Api/Analytics/Recommendations/RecommendationEngine.cs`
- `backend/src/LoLCoach.Api/Analytics/Recommendations/Recommendation.cs`
- `backend/src/LoLCoach.Api/Application/PerformanceAnalysisService.cs`
- `backend/src/LoLCoach.Api/Application/PerformanceAnalysisDto.cs`
- `backend/src/LoLCoach.Api/Application/AiCoachModels.cs` (`CoachReport`, `CoachAnalysisInput`, `CoachGoalDto`)
- `backend/src/LoLCoach.Api/Application/MatchImportExceptions.cs` (`PlayerNotFoundException`)
- `backend/src/LoLCoach.Api/Application/IAiCoach.cs`
- `backend/src/LoLCoach.Api/Application/DeterministicCoachReportFactory.cs`
- `backend/src/LoLCoach.Api/Controllers/PlayersController.cs`
- `backend/src/LoLCoach.Api/Program.cs` (registro de DI para analyzers, calculadora, serviço)

### Infraestrutura referenciada (para confirmar contratos)

- `backend/src/LoLCoach.Api/Domain/PlayerMatch.cs`
- `backend/src/LoLCoach.Api/Infrastructure/MatchRepository.cs`
- `backend/src/LoLCoach.Api/Infrastructure/GlobalExceptionHandler.cs`
- `backend/src/LoLCoach.Api/Infrastructure/AiCoachUnavailableException.cs`
- `backend/src/LoLCoach.Api/Infrastructure/GeminiAiCoach.cs`

### Testes

- `backend/tests/LoLCoach.Tests/PlayerMetricsCalculatorTests.cs`
- `backend/tests/LoLCoach.Tests/PerformanceAnalyzerTests.cs`
- `backend/tests/LoLCoach.Tests/PerformanceAnalysisEndpointTests.cs`
- `backend/tests/LoLCoach.Tests/PostgresFixture.cs` (dependência da suíte de integração)

---

## 3. Avaliação contra critérios de aceite da spec

| # | Critério (spec.md) | Verificação no código | Resultado |
|---|---|---|---|
| 1 | "Mesma coleção de partidas produz o mesmo resultado" | `PlayerMetricsCalculator.Calculate` ordena explicitamente por `GameStart` desc + `Id` (estável) antes de qualquer agregação; funções puras sem I/O, sem `DateTime.Now`, sem `Random`, sem estado mutável compartilhado. Analyzers stateless. `PerformanceAnalysisService` aplica `OrderBy` com `StringComparer.Ordinal` para tiebreak. `ChampionAnalyzer` usa `ThenBy(champion.Champion, StringComparer.Ordinal)` em todas as ordenações. | **APROVADO** |
| 2 | "Nenhum número pode ser inventado" | Todos os `Insight.CurrentValue` derivam diretamente de `PlayerMetrics` (calculado das partidas): `CsPerMinute`, `AverageDeaths`, `VisionPerMinute`, `Kda`, `KdaStandardDeviation`, `best.WinRate`, `worst.WinRate`. `TargetValue` são constantes documentadas no source (alvos iniciais). `MatchesAnalyzed` reflete o tamanho da amostra real. Sem constantes mágicas não declaradas. | **APROVADO** |
| 3 | "Insights devem possuir evidência mensurável" | `Insight` é `record` com `Type, Severity, Metric, CurrentValue, TargetValue, Evidence, MatchesAnalyzed`. Cada analyzer preenche `Evidence` com string descrevendo o valor e o alvo em português. `RecommendationEngine` propaga `Evidence` para `RecommendationDto`. | **APROVADO** |
| Regra | "A identificação primária de problemas não deve depender de LLM" | Os 6 analyzers (`Farming`, `Death`, `Vision`, `Combat`, `Consistency`, `Champion`) são 100% determinísticos: comparadores com constantes decimais hardcoded + `yield return` de zero ou um insight. `IAiCoach` (LLM) entra apenas na fase `coachReport` e tem fallback determinístico (`DeterministicCoachReportFactory`). | **APROVADO** |

---

## 4. Checklist técnico

### Arquitetura

- Separação em camadas correta: `Analytics/Metrics` (cálculo puro), `Analytics/Analyzers` (regras determinísticas), `Analytics/Insights` (DTO de domínio), `Analytics/Recommendations` (mapeamento para texto), `Application/PerformanceAnalysisService` (orquestração), `Controllers/PlayersController` (casca fina).
- `PerformanceAnalysisService` aceita `IEnumerable<IPerformanceAnalyzer>` via DI → aberto para extensão, fechado para modificação. ✅
- `PlayerMetricsCalculator` registrado como **Singleton** (linha 46 de `Program.cs`) — correto, é puro e stateless. ✅
- `PerformanceAnalysisService` registrado como **Scoped** (linha 54) — correto, consome `IPlayerRepository` e `IMatchRepository` Scoped. ✅
- Analyzers Singleton (linhas 47-52) — correto, são sem estado. ✅
- **Sem vazamento de responsabilidade**: nenhuma regra de negócio no controller; nenhum SQL fora de `MatchRepository`. ✅

### Determinismo

- `PlayerMetricsCalculator.Calculate`: ordena por `GameStart desc` + `Id` antes de qualquer agregação, tornando a ordem de entrada irrelevante.
- `Kda` é média de `perMatchKdas` (não é média de médias) — não acumula erro.
- `StandardDeviation` (população, divide por N) é consistente entre chamadas para o mesmo input.
- `PerformanceAnalysisService` deduplica por `Type` (`GroupBy`) com tiebreak `SeverityRank → ThenByDescending(InsightGap) → ThenBy(Type, Ordinal) → Take(3)`. Determinístico.
- `ChampionAnalyzer`: ordena por `WinRate desc → Kda desc → Games desc → Champion (Ordinal)` para "melhor" e simétrico para "pior". Determinístico.
- `RecentMatchMetrics.DurationMinutes` usa `Round(...)` com `MidpointRounding.AwayFromZero` — estável.
- ⚠️ **Nenhum uso de `DateTime.Now`, `Guid.NewGuid()`, `Random`, `Task.Run` no caminho de cálculo**. ✅

### Ausência de LLM na identificação

- Verificado: todos os 6 analyzers comparam métricas agregadas com constantes `decimal` declaradas no próprio arquivo.
- O LLM (`IAiCoach` / `GeminiAiCoach`) é invocado **somente** em `PerformanceAnalysisService.CreateCoachReportAsync` (linhas 68-79) para gerar `coachReport`.
- `PerformanceAnalysisService` envolve a chamada em `try/catch (Exception)` com fallback para `DeterministicCoachReportFactory.Create(input)` — o LLM é estritamente opcional.
- O `catch` preserva `OperationCanceledException` quando o `cancellationToken` original foi cancelado (não silenciosamente converte cancelamento do usuário em relatório determinístico). ✅

### Evidência mensurável

- Cada analyzer produz uma string `Evidence` no formato "`<métrica> médio de X em Y partidas; alvo inicial Z`" — referência concreta aos números calculados.
- `PerformanceSummaryDto` propaga as métricas agregadas para o `coachReport` e para validação cruzada no `GeminiAiCoach.CollectAllowedNumbers`.
- O endpoint expõe `summary.matchesAnalysed`, `summary.winrate`, `summary.kda`, `summary.csPerMin`, `summary.visionPerMin`, `summary.damagePerMin` — toda métrica com origem rastreável.

### Deduplicação / ordenação / limite

- `InsightLimit = 3` (constante) com `Take(InsightLimit)` — verificado no teste de integração (`Assert.Equal(3, insights.Count)`).
- `GroupBy(insight => insight.Type)` + `OrderBy(SeverityRank).ThenByDescending(InsightGap).First()` — garante no máximo um insight por tipo (defensivo; hoje cada analyzer emite no máximo 1).
- Ordenação final: severidade ascendente (High=0 primeiro), gap relativo descendente, type ascendente ordinal.
- ✅

### Segurança

- `[ProducesResponseType]` lista apenas 200/404; `PlayerNotFoundException` é mapeada para 404 problem details em `GlobalExceptionHandler`. Sem 5xx leak.
- Sem concatenação de SQL — EF Core com queries parametrizadas.
- Logs: `logger.LogWarning(exception, ...)` no catch do AI Coach registra o tipo e a mensagem da exceção, mas `AiCoachUnavailableException` é construída apenas com mensagens que **não carregam segredos** (ex.: `"Gemini AI Coach request timed out."`, `"Gemini API key is not configured."` — note que esta última é literalmente o sinal de que a chave está ausente, não o seu valor).
- Endpoint não requer autenticação (`[ApiController]` sem `[Authorize]`) — alinhado com os outros endpoints públicos do controller; pode ser evolução futura (escopo desta spec não cobre auth).
- ✅ sem falha de segurança evidente.

### Performance

- `MatchRepository.ListPlayerMatchesForAnalysisAsync`: `Include(Match)` + `AsNoTracking()` em query única — sem N+1.
- `PlayerMetricsCalculator.Calculate` itera a coleção de partidas (≤ 20) várias vezes para agregações (`Sum`, `Count`, `Select(...)`); custo desprezível. Linq-to-objects sem round-trips adicionais.
- Analyzers independentes, `yield return` com `SelectMany` em pipeline — `O(analyzers × 1)` (cada analyzer emite no máximo 1 insight hoje).
- Sem hot loop, sem `I/O` síncrono, sem `await` desnecessário.
- ✅

### Manutenibilidade

- Cada analyzer é uma classe `sealed` com responsabilidade única e duas constantes nomeadas (`TargetX`, `HighSeverityThreshold`).
- Constantes declaradas inline — spec diz que "targets iniciais podem começar configuráveis e ser refinados depois", então hardcode hoje é aceitável. **Sugestão opcional**: extrair para `IOptions<TargetOptions>` quando a tunabilidade virar requisito.
- Nomes de tipos expressam intenção: `LowCs`, `HighDeaths`, `LowVision`, `LowCombatImpact`, `InconsistentPerformance`, `BestChampion`, `WorstChampion`.
- `PerformanceAnalysisService` é o único ponto que conhece a pipeline; analyzers e motor de recomendação podem ser testados isoladamente.
- `ToInsightTypeCode`, `ToTitle`, `ToStableId`, `ToCamelCase` são funções privadas estáticas — puras e testáveis.
- ✅

### Legibilidade

- Arquivos curtos (≤ 50 linhas cada analyzer).
- Nomes claros, sem abreviações obscuras.
- XML doc presente em `PlayersController` para o novo endpoint `Analysis`.
- ✅

### Cobertura de testes

- ✅ `PlayerMetricsCalculatorTests.Same_matches_produce_same_metrics_and_calculate_rates` prova o critério "mesma coleção produz mesmo resultado" no nível do calculador (compara resultado com coleção original e invertida).
- ✅ Cada analyzer tem teste positivo (insight emitido com tipo/severidade/métrica esperados).
- ✅ `Consistency_analyzer_requires_enough_matches_and_variation` cobre o early-exit por amostra insuficiente.
- ✅ `PerformanceAnalysisEndpointTests` cobre:
  - 200 com payload completo (summary, insights, recommendations, champions, recentMatches).
  - Uso do AI Coach quando provider retorna relatório válido.
  - Coleções vazias quando jogador não tem partidas.
  - 404 quando jogador não existe.
- ⚠️ **Lacuna importante**: **não existe teste que prove o critério de aceite #1 ("mesma coleção produz mesmo resultado") para o pipeline completo de análise**. Hoje só o `PlayerMetricsCalculator` é testado com coleção invertida; nada garante o mesmo para `PerformanceAnalysisService` (deduplicação, ordenação por `InsightGap`, `Take(3)`, mapping para DTO).
- ⚠️ **Lacuna menor**: nenhum teste cobre explicitamente a fronteira de severidade (ex.: `CsPerMinute == 5.0` deve ser Medium, não High; `AverageDeaths == 5.0` deve sair sem insight). Cada analyzer hoje é testado com valores interiores ao alvo.

---

## 5. Achados

### Achado 1 — Cobertura insuficiente do critério de aceite "mesma coleção produz mesmo resultado" no pipeline completo

- **Classificação:** 🟠 Importante
- **Local:** `backend/tests/LoLCoach.Tests/PerformanceAnalysisEndpointTests.cs` (ausente)
- **Descrição:** O critério de aceite #1 da spec ("Mesma coleção de partidas produz o mesmo resultado") está provado em testes apenas para `PlayerMetricsCalculator`. `PerformanceAnalysisService.AnalyzeAsync` introduz várias etapas determinísticas adicionais (deduplicação por `GroupBy(Type)`, ordenação por `SeverityRank` + `InsightGap` + `Type` ordinal, `Take(3)`, mapeamento para DTO via `ToDto`). Se algum desses passos for modificado para usar uma fonte não-determinística (ex.: `OrderBy` sem `StringComparer.Ordinal`, ordenação estável com coleção já ordenada por hashset, etc.), nenhum teste existente pegaria a regressão.
- **Impacto:** Risco de regressão silenciosa no critério de aceite central da spec (determinismo), que é também a "regra de ouro" que outras features podem passar a depender.
- **Recomendação:** Adicionar um teste que invoque `PerformanceAnalysisService.AnalyzeAsync` duas vezes (dados em ordem A e em ordem B) e afirme que `summary`, `insights` (com mesmo `type`/`severity`/`currentValue`/`targetValue` na mesma posição), `recommendations`, `champions` e `recentMatches` são idênticos. Idealmente isso seria feito como teste unitário da pipeline de insights (`GroupBy + OrderBy + Take`) sem precisar do `PostgresFixture`, mas pode também ser um teste de integração.
- **Responsável sugerido:** `dev-backend` (sugestão de follow-up; não bloqueia este PR)

### Achado 2 — Lacuna menor em testes de fronteira dos analyzers

- **Classificação:** 🟡 Sugestão
- **Local:** `backend/tests/LoLCoach.Tests/PerformanceAnalyzerTests.cs`
- **Descrição:** Os testes atuais dos analyzers usam valores interiores aos limites (ex.: `csPerMinute: 4.8` está abaixo do alvo 6.5 e abaixo do HighSeverity 5.0 — então emite `High`; `averageDeaths: 7.1` emite `High`). Não há teste explícito para os casos de fronteira:
  - `CsPerMinute == 6.5` (igual ao alvo) → deve sair sem insight.
  - `CsPerMinute == 5.0` (igual ao HighSeverity) → deve emitir Medium (não High).
  - `AverageDeaths == 5.0` → deve sair sem insight.
  - `Kda == 2.5` → deve sair sem insight.
  - `KdaStandardDeviation == 2.0` → deve sair sem insight.
- **Impacto:** Pequeno risco de regressão off-by-one em modificações futuras que mexam nas constantes dos analyzers.
- **Recomendação:** Adicionar 1 teste parametrizado cobrindo as fronteiras. Barato e fortalece a suite.
- **Responsável sugerido:** `dev-backend` (sugestão de follow-up; não bloqueia este PR)

### Achado 3 — `PerformanceAnalysisService.GroupBy` + `OrderBy().ThenByDescending().First()` é preparação sem necessidade atual

- **Classificação:** 🟡 Sugestão
- **Local:** `backend/src/LoLCoach.Api/Application/PerformanceAnalysisService.cs:28-29`
- **Descrição:** Hoje cada analyzer emite **no máximo 1 insight** (todos usam `yield break` cedo se nada deve ser reportado, e um único `yield return` no caminho positivo). Logo `GroupBy(Type)` sempre produz grupos de 1 elemento, e o `OrderBy(SeverityRank).ThenByDescending(InsightGap).First()` por grupo é trabalho desperdiçado em runtime. O `GroupBy` continua sendo defesa útil caso analyzers futuros emitam múltiplos insights do mesmo tipo, mas vale considerar trocar o pipeline por algo equivalente a `DistinctBy(Type)` ou documentar a intenção no comment.
- **Impacto:** Performance desprezível (≤ 6 grupos com 1 elemento cada); risco de confusão de leitura.
- **Recomendação:** Adicionar comentário curto explicando que a deduplicação é defensiva para analyzers futuros que possam emitir múltiplos insights do mesmo tipo; ou trocar para `DistinctBy(insight => insight.Type, ...)` com seletor de prioridade.
- **Responsável sugerido:** `dev-backend` (cosmético)

### Achado 4 — `InsightGap` trata `BestChampion` e `WorstChampion` com semântica simétrica

- **Classificação:** 🟡 Sugestão
- **Local:** `backend/src/LoLCoach.Api/Application/PerformanceAnalysisService.cs:108-109` + `backend/src/LoLCoach.Api/Analytics/Analyzers/ChampionAnalyzer.cs:25, 44`
- **Descrição:** A função `InsightGap = |CurrentValue - TargetValue| / TargetValue` produz magnitude idêntica para `WinRate = 80` e `WinRate = 20` quando o alvo é 50 (gap = 0.6 nos dois casos). Como `SeverityRank` é o primeiro critério de ordenação e `BestChampion` é `Low` enquanto `WorstChampion` é `Medium`, na prática o `WorstChampion` tende a entrar no top 3 antes do `BestChampion`, e o tiebreak final por `Type.ToString()` (Ordinal) põe `BestChampion` antes de `WorstChampion` em empates. Isso é determinístico e coerente com a semântica de prioridade ("pior campeão" é mais útil para ação), mas o leitor pode estranhar que `BestChampion` seja classificado como "gap" — conceitualmente é força, não gap.
- **Impacto:** Determinístico; sem bug; leve estranhamento conceitual.
- **Recomendação:** Para `BestChampion`/`WorstChampion`, definir `InsightGap` como `0` (não há gap contra uma referência neutra de 50%), ou expor um campo `Magnitude` específico por tipo. Opcional.
- **Responsável sugerido:** `dev-backend` (cosmético / modelagem)

### Achado 5 — `StandardDeviation` usa fórmula populacional (divide por N), não amostral (N-1)

- **Classificação:** ⚪ Nitpick
- **Local:** `backend/src/LoLCoach.Api/Analytics/Metrics/PlayerMetricsCalculator.cs:96-104`
- **Descrição:** A função divide por `values.Count`, gerando o desvio padrão populacional. Para detectar inconsistência em uma "população" de partidas do próprio jogador (amostra completa do histórico recente, não amostra aleatória de uma distribuição maior), a fórmula populacional é defensável. Porém, se o time tratar a métrica como "consistência na carreira" em algum momento, a fórmula amostral (Bessel) seria mais padrão.
- **Impacto:** Nenhum prático hoje. Diferença é pequena para N≥20.
- **Recomendação:** Documentar a escolha com um comentário de uma linha, ou expor como `PopulationStandardDeviation` se a semântica for ampliada.
- **Responsável sugerido:** `dev-backend` (comentário)

### Achado 6 — `RecommendationEngine` ignora silenciosamente `BestChampion`/`WorstChampion`

- **Classificação:** ⚪ Nitpick
- **Local:** `backend/src/LoLCoach.Api/Analytics/Recommendations/RecommendationEngine.cs:14-62`
- **Descrição:** O switch do motor cobre 5 tipos e cai em silêncio (sem `default`) para os 2 tipos de campeões. Para `BestChampion` (severity Low) e `WorstChampion` (severity Medium), nenhuma recomendação é emitida. É defensável (recomendar jogar mais de um campeão forte é incoerente; recomendar largar um campeão fraco é agressivo), mas o switch poderia ter um branch explícito retornando `Array.Empty<Recommendation>()` ou um comentário justificando.
- **Impacto:** Nenhum hoje; leve risco de "TODO sem issue" se alguém quiser adicionar comportamento futuro.
- **Recomendação:** Adicionar comentário curto (`// Champion insights são informativos; ações de campeões vivem em outro fluxo.`) ou branch explícito.
- **Responsável sugerido:** `dev-backend` (comentário)

### Achados explicitamente descartados (verificados e aprovados)

- **Segurança / vazamento de segredos do LLM**: `GeminiAiCoach` valida números mencionados contra o payload (`ValidateReport` → `NumberRegex`); chaves vêm de configuração, nunca do payload do usuário. ✅
- **Race conditions / concorrência**: `PerformanceAnalysisService.AnalyzeAsync` é `async`, sem `ConfigureAwait(false)` (padrão do projeto), sem `Task.Run`, sem `Result`/`Wait`. ServiceProvider respeita Scoped/Singleton. ✅
- **N+1 em EF Core**: confirmado um único round-trip em `MatchRepository.ListPlayerMatchesForAnalysisAsync` com `Include` + `AsNoTracking`. ✅
- **`async void` ou `throw ex`**: nenhum encontrado nos arquivos revisados. ✅
- **`catch (Exception)` muito amplo**: o catch em `CreateCoachReportAsync` é amplo de propósito (qualquer falha do LLM → fallback determinístico), preserva `OperationCanceledException` quando o cancelamento vem do request original. ✅
- **DTO inconsistente com frontend**: a integração com Angular (`apps/dashboard` consumidor) foi validada em revisões anteriores (specs 004/005/006) e o contrato é estendido de forma aditiva. ✅
- **Bloqueio por limitações ambientais**: testes de integração dependem de testcontainers/PostgreSQL, mas o ambiente local tem Docker funcional — verificado na execução. ✅

---

## 6. Validação cruzada com o parecer de QA

O QA formal (`specs/003-analytics/evidence/qa-validation-report.md`, commit
`fc5b773`, veredito APROVADO) cobriu os mesmos critérios e chegou a um
resultado equivalente: build 0/0, 12/12 testes da spec, 75/75 totais,
determinismo provado por inversão de coleção, ausência de LLM na identificação,
deduplicação/ordenação/limite verificados linha-a-linha no
`PerformanceAnalysisService`.

Reproduzi os mesmos números de forma independente em worktree isolado a partir
do mesmo commit (`2bd3093`). Não encontrei nada que contradiga o parecer do
QA. As lacunas que listei (Achados 1 e 2) também não foram detectadas pelo QA
por serem **ausência de teste**, não defeito no código — e o QA explicitamente
limita seu escopo a "testes que existem e passam".

O Achado 1 (cobertura do determinismo no pipeline completo) é o único que
merece ser destacado como follow-up; ele reforça a robustez do critério
central da feature sem invalidar nada do que foi entregue.

---

## 7. Limitações da revisão

- Não executei cenários de carga / stress (`MatchRepository` com 10k partidas, por exemplo). O design atual é `O(N matches)` para o calculador e `O(analyzers)` para a pipeline de insights; para os volumes esperados (≤ 20 partidas por jogador, ≤ 20 jogadores simultâneos) isso é desprezível. Se houver migração futura para analisar temporadas inteiras, vale revisitar.
- Não auditei a configuração de `IAiCoach` em produção (`AiCoachOptions`, chave `GEMINI_API_KEY`, timeout). Esses aspectos vivem fora do escopo da Spec 003 (são da Spec 006).
- Não rodei ferramentas de cobertura de código (coverlet/ReportGenerator). A análise é qualitativa baseada nos testes existentes.

---

## 8. Decisão

**APROVADO.**

A Spec 003 (Analytics Engine) está pronta para a fase de finalização documental.
O código atende aos três critérios de aceite da spec, não introduz regressões,
segue o padrão arquitetural do projeto, mantém a identificação primária de
problemas 100% determinística e sem LLM, e tem cobertura suficiente para o
caminho feliz mais os caminhos de borda mais importantes (404, payload vazio,
IA Coach indisponível).

Recomendo **não** reverter o trabalho entregue. As lacunas de cobertura
detectadas (Achados 1 e 2) são melhorias valiosas que podem entrar em uma
spec/tarefa futura, sem bloquear esta consolidação.

### Checklist final

- [x] Código compila (build 0 warnings, 0 errors — reproduzido)
- [x] Testes executados (12/12 spec + 75/75 totais — reproduzidos)
- [x] Critérios de aceite atendidos (todos os 3 da spec + regra central)
- [x] Sem bug crítico ou alto
- [x] Sem falha de segurança evidente
- [x] Padrão do projeto respeitado (DI, async/await, exception handler, DTOs separados)
- [x] Instruções de teste informadas (comandos + resultados reais acima)
- [x] Integração validada (DTO compatível com consumidores; contrato estendido aditivamente nas specs 005/006)
- [x] QA considerado (parelho reproduzido e validado cruzadamente)
- [x] Limitações ambientais separadas de defeitos (Docker/Postgres testcontainers fora do escopo de defeitos)

### Próximo passo

Pode seguir para `devops` (task `t_02409330`, "Consolidar pareceres e abrir PR — Specs 002 e 003") para a consolidação documental e abertura do PR.

### Responsáveis sugeridos para os achados

| Achado | Responsável |
|---|---|
| 1 — Cobertura do determinismo no pipeline completo | `dev-backend` (follow-up) |
| 2 — Testes de fronteira nos analyzers | `dev-backend` (follow-up) |
| 3 — Documentar/debloatar deduplicação | `dev-backend` (cosmético) |
| 4 — Semântica de `InsightGap` para campeões | `dev-backend` (cosmético) |
| 5 — Fórmula do desvio padrão | `dev-backend` (comentário) |
| 6 — Branch explícito para campeões no `RecommendationEngine` | `dev-backend` (comentário) |

---

**Arquivo:** `specs/003-analytics/evidence/code-review-report.md`
**Commit do relatório:** pendente (criado nesta sessão, ainda não commitado)
**Branch:** `review/formal-spec-003`