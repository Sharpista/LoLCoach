# Design 001 - Busca de Jogador Riot

## Fluxo

```text
Angular -> POST /api/players/search -> SearchPlayerHandler -> IRiotAccountClient -> Riot API (ACCOUNT-V1) -> IPlayerRepository -> PostgreSQL
```

## Domain

Entidade `Player`:

- Id: Guid
- Puuid
- GameName
- TagLine
- Region
- CreatedAt
- LastUpdatedAt

PUUID deve ser único.

## Application

Criar:

- `SearchPlayerCommand`
- `SearchPlayerHandler`
- `SearchPlayerValidator`
- `PlayerDto`

O handler deve validar a entrada, consultar a Riot, localizar o jogador por PUUID, criar ou atualizar e retornar DTO.

## Contrato de região

Entrada/persistência/devolução: **plataforma LoL** em caixa baixa (`br1`, `euw1`, `na1`, `kr`, ...).

O backend deriva o routing regional internamente para a ACCOUNT-V1:

| Routing | Plataformas |
|---|---|
| `americas` | `na1`, `br1`, `la1`, `la2` |
| `asia` | `kr`, `jp1` |
| `europe` | `euw1`, `eun1`, `tr1`, `ru` |
| `sea` | `oc1`, `ph2`, `sg2`, `th2`, `tw2`, `vn2` |

Plataformas aceitas (conjunto LoL): `br1`, `eun1`, `euw1`, `jp1`, `kr`, `la1`, `la2`, `na1`, `oc1`, `ph2`, `ru`, `sg2`, `th2`, `tr1`, `tw2`, `vn2`.

Região validada case-insensitive e normalizada para caixa baixa. Não inferir região a partir de `tagLine`. Confirmar suporte de `sea` à ACCOUNT-V1 na doc oficial durante a implementação; se indisponível, registrar e tratar plataformas `sea` conforme o contrato da Riot.

## Contrato de validação

- `gameName`: 3–16 caracteres, permite Unicode (letras/dígitos) e espaços; case-insensitive para fins de validação/lookup.
- `tagLine`: busca 2–5 caracteres (aceita legado de 2, ex.: `BR`, `NA`, `KR`); alfanumérico; case-insensitive. Criação/alteração de conta mantém 3–5.
- Normalização: trim de espaços nas bordas; comparação/lookup case-insensitive. A deduplicação é feita por `puuid` (único), portanto a variação de caixa não gera duplicidade.
- `region`: obrigatório e pertencente ao conjunto de plataformas aceitas.

## Infrastructure

Criar:

- `IRiotAccountClient`
- `RiotAccountClient`
- `IPlayerRepository`
- `PlayerRepository`

Usar `HttpClientFactory`. API Key deve vir de configuração segura/variável de ambiente (`Riot__ApiKey`), nunca versionada. Usar cabeçalho `X-Riot-Token` (fallback `api_key` query conforme doc oficial). Base URL derivada do routing: `https://{routing}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{gameName}/{tagLine}`.

Tratamento de erros da Riot:
- 404 (jogador inexistente) → 404 Problem Details.
- 429 → 429 Problem Details, preservando `Retry-After` quando presente.
- 5xx / timeout / transporte → 503 Problem Details.
- Nunca expor chave, payload externo ou exceção interna.

## Persistência

Tabela `players` com índice único em `puuid`.

## Frontend

Feature `player-search` com formulário para gameName, tagLine e region. Em sucesso, redirecionar para `/player/{id}`.

## Constraints

- Controller sem regra de negócio.
- Não expor DTO bruto da Riot.
- Não acessar EF Core diretamente no Controller.

## Contrato HTTP técnico

`POST /api/players/search`, `Content-Type: application/json`.
Entrada: `{ "gameName": "Example", "tagLine": "TAG", "region": "br1" }`.
Sucesso: **200 OK**, tanto para criação quanto atualização, JSON camelCase:

```json
{
  "id": "a2a0b1c2-d3e4-4567-89ab-0123456789ab",
  "puuid": "<PUUID retornado pela Riot>",
  "gameName": "Example",
  "tagLine": "TAG",
  "region": "br1",
  "createdAt": "2026-01-01T00:00:00+00:00",
  "lastUpdatedAt": "2026-01-01T00:00:00+00:00"
}
```

Datas UTC ISO 8601. `id` e `createdAt` preservados na atualização por PUUID; demais dados básicos e `lastUpdatedAt` atualizados. Não retornar DTO externo bruto.
Erros previstos: Problem Details (`application/problem+json`) com `type`, `title`, `status`, `detail` e `traceId`; validação 400 inclui `errors` com chaves camelCase e arrays de mensagens. 404 para ausência na Riot, 429 para limite externo (preservar `Retry-After` válido), 503 para indisponibilidade externa, transporte ou timeout.

## Base backend independente das perguntas

- .NET 10 / ASP.NET Core, SDK local verificado 10.0.401. Um projeto API com pastas Domain, Application e Infrastructure, e projeto xUnit irmão em `backend/tests`.
- EF Core + Npgsql: PostgreSQL, índice único `ix_players_puuid`, migration inicial nova. Colunas text evitam comprimentos de negócio não especificados.
- Repository: upsert atômico PostgreSQL por PUUID, preservando Id/CreatedAt em conflito, retornando registro persistido. Interface oferece lookup por PUUID e save. SQL parametrizado.
- Datas UTC explícitas; `TimeProvider` no handler. Repository não interpreta/normaliza região.
- FluentValidation para `SearchPlayerValidator`; HttpClientFactory para o client Riot.
- Sem migration automática no startup; ferramenta EF local. Configuração via `ConnectionStrings__LoLCoach` e `Riot__ApiKey`, nunca secrets versionados.
- Testes PostgreSQL em container efêmero prefixado `lolcoach`; não usar cluster existente nem substituir verificação PostgreSQL por SQLite. Testes do fluxo Riot usam fake de `IRiotAccountClient` (sem HTTP real).

## Fontes oficiais consultadas

- https://developer.riotgames.com/docs/lol — ACCOUNT-V1 `/riot/account/v1/accounts/by-riot-id/{gameName}/{tagLine}`, distinção plataforma/routing regional; listagem regional geral americas, asia, europe, sea.
- https://developer.riotgames.com/apis#account-v1/GET_getByRiotId
- https://support-leagueoflegends.riotgames.com/hc/en-us/articles/360041788533-Riot-ID-FAQ — regras de criação/alteração e taglines regionais legadas.
- https://support-developer.riotgames.com/hc/en-us/articles/22698983117587-Summoner-Name-to-Riot-ID — gameName 3–16, tagLine 3–5, Unicode e separador `#`.

QA, frontend e code review concluídos; ver o rastreamento e a verificação em `tasks.md`. As decisões de região e normalização estão incorporadas nesta versão.
