---
id: 001
name: player-search
status: IN_PROGRESS
depends_on: []
---

# Spec 001 - Busca de Jogador Riot

## Objetivo

Permitir que o usuário informe um Riot ID e o sistema localize a conta correspondente usando a API oficial da Riot.

## Entrada

- `gameName`
- `tagLine`
- `region` (plataforma LoL, ex.: `br1`, `euw1`, `na1`, `kr`)

## Requisitos

1. Validar o Riot ID.
2. Consultar a Riot API (ACCOUNT-V1).
3. Obter o PUUID.
4. Persistir dados básicos da conta.
5. Atualizar a conta se ela já existir.
6. Não criar duplicidades.

## Dados armazenados

- Id interno
- PUUID
- GameName
- TagLine
- Região
- CreatedAt
- LastUpdatedAt

## Erros

- 400 - entrada inválida
- 404 - jogador não encontrado
- 429 - rate limit externo
- 503 - serviço Riot indisponível

## Critérios de aceitação

- Riot ID válido retorna jogador e PUUID.
- Riot ID inexistente retorna 404.
- Jogador existente é atualizado sem duplicidade.

## Fora do escopo

- OAuth Riot
- login da aplicação
- análise de partidas

## Decisões (Open Questions resolvidas)

1. **Contrato de `region`**: o usuário informa a **plataforma LoL** (`br1`, `euw1`, `na1`, `kr`, ...). O backend deriva o **routing regional** (`americas`/`asia`/`europe`/`sea`) internamente para chamar a ACCOUNT-V1 e **persiste/devolve a plataforma**. Região não é inferida a partir de `tagLine`. Valores aceitos: conjunto atual de plataformas LoL (ver tabela de mapeamento em `design.md`).
2. **Validação de IDs legados**: a busca aceita `tagLine` legado de 2–5 caracteres (ex.: `BR`, `NA`, `KR`) além da regra de criação/alteração (3–5). Normalização case-insensitive; `gameName` aceita Unicode e espaços. A criação/alteração de conta mantém `gameName` 3–16 e `tagLine` 3–5.

Nenhuma dependência de outra spec (`depends_on: []`).
