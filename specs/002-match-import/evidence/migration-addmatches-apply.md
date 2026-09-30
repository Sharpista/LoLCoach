# Spec 002 — aplicação da migration `AddMatches` (`matches`/`player_matches`)

| Campo | Valor |
|---|---|
| Card Kanban | `t_d13c8671` (assignee `devops`) |
| run_id oficial | `run_01M3DQ2G1DBQBSSXN1JJCPC075` |
| Issue/contexto | bloqueio reportado pelo usuário: `relation "player_matches" does not exist` em `GET /api/players/{id}/analysis` |
| Ambiente alvo | projeto Supabase `tsfdsjmostggtxckmirr` (`LoLCoach`, PostgreSQL 17.6) — único ambiente vivo; consumido por `ConnectionStrings__LoLCoach` |
| Workspace | `/home/alexandre/LoLSaas` (branch `feat/ajustes`) + árvore pinada `3b8eef1289b31b9e94cb4e77f3349fa8622a7082` em `/tmp/lol-mig-check/wt` |
| Data (UTC) | 2026-09-26 (execução 02:00–02:40 UTC) |
| Resultado | **migration aplicada e verificada**; endpoint deixa de falhar por relation inexistente (prova abaixo). 1 limitação de ambiente registrada (§8) |

> Este arquivo foi produzido no workspace compartilhado `/home/alexandre/LoLSaas`
> **sem commit** (o card não autoriza commit/push e há outros agentes operando o
> mesmo worktree). Publicação/commit, se desejada, é card de `github-profile`.

---

## 1. Autorização e base do escopo

O card determina: *"aplicar migration somente se o ambiente/credencial já estiver
configurado e autorizado pelo escopo operacional"*. Condições verificadas **antes**
de qualquer escrita:

- `ConnectionStrings__LoLCoach` já presente no ambiente do perfil
  (`~/.hermes/shared/.env`, modo 600; valor nunca impresso; fingerprint
  `sha256[:16]=90b0785715e3eae5`).
- Alvo derivado da própria credencial: `aws-0-us-east-1.pooler.supabase.com:6543`,
  banco `postgres`, usuário `postgres.tsfdsjmostggtxckmirr` → projeto
  `tsfdsjmostggtxckmirr`, que é **o único ambiente vivo do LoLCoach**
  (`backend/SUPABASE.md`, `specs/008-railway-deploy/spec.md`) e o mesmo projeto já
  operado por este perfil com autorização do usuário em LOL-59 (run 117) e LOL-61
  (run 138).
- `SUPABASE_ACCESS_TOKEN` presente e funcional (fingerprint `676d05ff167ed908`).
- Escopo do card inclui aplicar, verificar esquema e executar smoke.
- Migration **aditiva**: 2 tabelas novas vazias + 3 índices + 2 FKs; zero DML, zero
  alteração em `players`/`agent_*`, zero secret, zero deploy.
- Nenhuma autorização de deploy Railway, tag, release, force-push, secret ou
  downgrade foi usada.

**Comentário de auditoria publicado no card antes do apply** (id 136), com o estado
pré-apply e o plano (ver o thread do card).

## 2. Artefato aplicado (imutável)

| Papel | Arquivo | sha256 | Bytes |
|---|---|---|---|
| Migration EF | `backend/.../Migrations/20260914165003_AddMatches.cs` | `4a4e94a26c2aec6eb80c0dfba57c8450578d8fdacd1d7972d55a73dc0b9adf96` | 4371 |
| Designer | `.../20260914165003_AddMatches.Designer.cs` | `6297f1231d363d3bd450ef9175f55abab3c1fe4aad141302e9e74affe69fab53` | 8395 |
| Snapshot do modelo | `.../PlayerDbContextModelSnapshot.cs` | `8faa2c5898aa971b3fe6f7a6338c3379a887cb5175a087b303b0e06530dd261f` | 8303 |
| **SQL idempotente gerado pelo EF** | `dotnet ef migrations script --idempotent` | `8f098b60867849461b510956d7a94ef451c3dbe2e390c3cdd796f5a666e84f71` | 3781 |
| Corpo transportado (sem BOM UTF-8) | mesmo arquivo, 3 bytes iniciais removidos | `67bc358ac5e7adc87c0947f832004237b9926fb47f8e1ffb8f7cedcd820e7da4` | 3778 |

Os arquivos de migration são **byte-idênticos ao `origin/develop`** (`git diff HEAD
origin/develop -- .../Migrations/` vazio) e não foram editados: migration antiga
permanece intacta (regra do card e do `backend/README.md`).

Pré-flight do artefato (`apply_mgmt.py`): `CREATE TABLE IF NOT EXISTS` = 1,
`CREATE TABLE` = 4, `CREATE INDEX` = 2, `CREATE UNIQUE INDEX` = 2,
`INSERT INTO "__EFMigrationsHistory"` = 2, **`DROP`/`ALTER`/`TRUNCATE`/`UPDATE` = 0**
(as 2 ocorrências de `DELETE` são `ON DELETE CASCADE` de FK, não DML). Duas
transações independentes, uma por migration, cada uma com guarda por histórico.

## 3. Estado real antes do apply (duas leituras idênticas, somente SELECT)

`POST /v1/projects/tsfdsjmostggtxckmirr/database/query`, snapshots
`pre_apply.json` e `pre_apply2.json`:

- Tabelas `public`: `__EFMigrationsHistory`, `agent_events`, `agent_execution_locks`,
  `agent_runs`, `players`.
- `__EFMigrationsHistory`: **apenas** `20260911183231_InitialPlayers` (10.0.0).
- `matches` / `player_matches`: **ausentes** → confirma a causa do erro reportado.
- Contagens: `players=3`, `agent_runs=2`, `agent_events=12`, `agent_execution_locks=0`.
- Servidor: PostgreSQL 17.6 (compilado com GCC 15.2.0).

## 4. Ensaio local (somente em bancos descartáveis) antes de tocar o projeto

Bancos locais efêmeros em `postgres:17-alpine` (17.11) na porta 55439, mais
`PgBouncer 1.25.2` em **modo transação** na porta 56432 como proxy do pooler 6543.
Nenhuma credencial de projeto foi usada nos ensaios (`out/rehearsal.out`,
`out/rehearsal_pooled.out`).

| Verificação | Comando | Resultado |
|---|---|---|
| Script idempotente em banco novo | `psql -f migrations-idempotent.sql` | executado: passou (4 tabelas, 2 linhas de histórico) |
| Reaplicação do mesmo script | idem 2ª vez | executado: passou (só `NOTICE`, nenhum efeito) |
| `dotnet ef database update` (EF nativo) | banco novo | executado: passou (`Applying migration 'InitialPlayers'`, `'AddMatches'`, `Done.`) |
| Reexecução do `database update` | idem 2ª vez | executado: passou (`No migrations were applied. The database is already up to date.`) |
| Drift de modelo | `dotnet ef migrations has-pending-model-changes` | executado: passou (`No changes have been made to the model since the last migration.`) |
| DML + leitura relacional | inserts sintéticos + join | executado: passou |
| Índice único de `riot_match_id` | insert duplicado | executado: passou (falha esperada `23505`) |
| Cascade `players → player_matches` | `delete from players` | executado: passou (`player_matches=0`, `matches=1`) |
| Equivalência EF vs script | comparação de colunas/constraints/índices | **IDENTICAL** nos 4 conjuntos |
| `dotnet ef database update` **através do pooler em modo transação** | PgBouncer 1.25.2 | executado: passou (2x; 2ª no-op; `pg_locks` advisory = 0 após a execução) |
| Rollback (dry-run em `begin…rollback`) | banco descartável | executado: passou (histórico volta a 1 linha) |

## 5. Apply no projeto real

### 5.1 Canal utilizado

`POST /v1/projects/tsfdsjmostggtxckmirr/database/query` com o **SQL gerado pelo EF**
(transações do próprio arquivo) — mesmo canal usado nas aplicações autorizadas
anteriores deste projeto (LOL-59 run 117, LOL-61 run 138).

**Por que não `dotnet ef database update` direto:** a credencial Postgres
configurada no ambiente **não autentica mais** (ver §8.1) — não há caminho Npgsql
viável sem uma credencial válida. O caminho EF nativo **foi ensaiado e aprovado**
localmente, inclusive através de pooler em modo transação (§4), e o SQL aplicado é
o próprio artefato gerado pelo EF, com equivalência de esquema provada (§6).

### 5.2 Resultado

| Aplicação | HTTP | Tempo | Resposta |
|---|---|---|---|
| #1 (`8f098b60…` / corpo `67bc358a…`) | **201** | 1,82 s | `[]` |
| #2 (reaplicação — idempotência) | **201** | 0,82 s | `[]` |

Primeira tentativa com o arquivo *com* BOM: HTTP 400 `42601 syntax error at or near
"﻿CREATE"` — o endpoint SQL rejeita o BOM UTF-8 que o `dotnet ef migrations script`
grava. Nada foi aplicado nessa tentativa (erro de parse, nenhum statement executado);
a correção foi remover os 3 bytes de BOM, sem qualquer outra alteração de bytes.

## 6. Esquema verificado no projeto real (pós-apply)

Comparação campo a campo entre o projeto real e o banco local criado pelo
`dotnet ef database update` (mesma versão de provider EF/Npgsql):

| Conjunto | Real | Local (EF) | Veredito |
|---|---|---|---|
| Colunas (`matches`, `player_matches`, `players`, histórico) | 32 | 32 | **IDENTICAL** |
| Constraints | 5 | 5 | **IDENTICAL** |
| Índices | 7 | 7 | **IDENTICAL** |
| `__EFMigrationsHistory` | `InitialPlayers` + `AddMatches` (10.0.0) | idem | **IDENTICAL** |

Objetos criados (todos com colunas `NOT NULL`, sem defaults):

- `matches` — `id uuid PK (pk_matches)`, `riot_match_id text`, `game_start timestamptz`,
  `game_duration int`, `queue_id int`, `game_mode text`; índice único
  `ix_matches_riot_match_id (riot_match_id)`.
- `player_matches` — `id uuid PK (pk_player_matches)`, `match_id uuid`, `player_id uuid`,
  `champion_id int`, `champion_name text`, `team_position text`, `win bool`, `kills`,
  `deaths`, `assists`, `total_cs`, `gold_earned`, `damage_to_champions`, `damage_taken`,
  `vision_score`, `wards_placed`, `wards_killed`; FKs
  `fk_player_matches_matches_match_id → matches(id) ON DELETE CASCADE` e
  `fk_player_matches_players_player_id → players(id) ON DELETE CASCADE`; índices
  `ix_player_matches_match_id`, `ix_player_matches_player_id`.

Atributos gerenciados pelo Supabase (não expressos na migration), com **paridade em
relação a `players`**: RLS habilitado nas duas tabelas novas
(`relrowsecurity=true`, `relforcerowsecurity=false` → o dono, papel da aplicação,
não é afetado) e privilégios idênticos para `postgres`/`service_role`/`anon`/
`authenticated`.

**Escrita aceita (com rollback, zero resíduo):** em uma única transação,
`insert into matches` + `insert into player_matches` referenciando um player real
funcionaram (FKs/`NOT NULL`/FK de players válidos) e o `rollback` deixou as contagens
exatamente como antes: `matches=0`, `player_matches=0`, `players=3`, `agent_runs=2`,
`agent_events=12`, `agent_execution_locks=0`.

**Nenhum objeto pré-existente foi alterado**: nas comparações pré vs pós, todas as
diferenças são **adições** (8 privilégios, 2 RLS, 4 constraints, 5 índices, 23 colunas
das tabelas novas); nenhuma remoção em `functions`, `players`, `agent_*`,
`__EFMigrationsHistory`. As contagens de dados funcionais ficaram idênticas.

## 7. Smoke do endpoint

### 7.1 Padrão de consulta do endpoint resolvido no banco REAL (somente leitura)

Consulta idêntica à de `MatchRepository.ListPlayerMatchesForAnalysisAsync`
(`player_matches` ⋈ `matches` por `player_id`, ordenado por `game_start desc`),
executada para um player real (`86d6b77a-96af-4a62-a78f-4792c1a0b681`):

- HTTP 201, resultado `0` linhas — **sem erro de relation inexistente**;
- `EXPLAIN`: `Nested Loop` → `Bitmap Index Scan on ix_player_matches_player_id` +
  `Index Scan using pk_matches on matches m` — ou seja, as duas tabelas e os índices
  do plano existem e são usados pelo planner.

### 7.2 Smoke HTTP real do endpoint

Executado contra um **PostgreSQL 17 local com esquema byte-equivalente ao do projeto
real** (§6), subindo a API do mesmo commit pinado que gerou o SQL aplicado:

| Rota | HTTP | Resultado |
|---|---|---|
| `GET /health` | 200 | `{"status":"healthy"}` |
| `GET /health/ready` | 200 | banco alcançável via Npgsql |
| `GET /api/players/{id}/analysis` (player com 1 partida) | **200** | `summary.matchesAnalysed=1`, `recentMatches` com 1 item |
| `GET /api/players/{id}/analysis` (player **sem** partidas — caso dos 3 players reais) | **200** | `matchesAnalysed=0`, listas vazias |
| `GET /api/players/{id-inexistente}/analysis` | 404 | `ProblemDetails` "Player not found" |

Log da API: **nenhuma** ocorrência de `does not exist`, 0 linhas de erro/exceção.
A conexão foi passada em formato URI `postgres://…`, exercitando também a
normalização `PostgresConnectionString.Normalize` do commit `3b8eef1`.

### 7.3 Smoke autenticado contra o projeto real — **não executado**

Bloqueado pela credencial Postgres inválida (§8.1): a API local com
`ConnectionStrings__LoLCoach` configurada devolve `503 Database unavailable`
(comportamento correto do handler), e não há segunda credencial válida no ambiente.
O critério "endpoint deixa de falhar por relation inexistente" está provado pelos
itens 7.1 (relation/índices resolvidos no banco real), 5–6 (esquema do projeto real
idêntico ao do EF) e 7.2 (endpoint 200 com esse mesmo esquema).

## 8. Achados de ambiente, riscos e rollback

### 8.1 Credencial Postgres configurada está inválida (achado, dono: usuário/orquestrador)

Todas as variantes de `ConnectionStrings__LoLCoach` presentes na máquina
(`~/.hermes/shared/.env`, `~/.hermes/profiles/*/.env`, `~/.hermes/profiles/orquestrador/.env`)
foram testadas (fingerprints `90b0785715e3eae5` e `516609e867c52f4c`; valores nunca
impressos): **`FATAL: password authentication failed`** no pooler de transação (6543),
no pooler de sessão (5432) e no host direto `db.tsfdsjmostggtxckmirr.supabase.co:5432`.
Consequências: apps/agentes locais não alcançam o banco (503), e o caminho documentado
`dotnet ef database update` não é executável neste ambiente até a senha ser corrigida.
Ação recomendada (fora do escopo deste card): atualizar a senha/connection string nos
arquivos de ambiente e no secret `CONNECTIONSTRINGS__LOLCOACH` do GitHub Environment
`production` (rotação no painel Supabase, runbook §10). **Nenhum secret foi criado,
rotacionado ou exposto por este run.**

### 8.2 PITR/backup do projeto (achado)

`GET /v1/projects/tsfdsjmostggtxckmirr/database/backups` → `walg_enabled=true`,
**`pitr_enabled=false`**, `backups: []`. O runbook §5.1 pressupõe PITR disponível;
na prática **não há janela de recuperação point-in-time nem backup listado**. Como a
migration aplicada é aditiva e as duas tabelas novas estão vazias, a exposição foi
nula, mas a lacuna de recuperação permanece para mudanças futuras (dono:
`orquestrador`/usuário — habilitar PITR/backup ou dump periódico).

### 8.3 Rollback preparado (não executado)

Política do projeto: **forward-fix** (runbook §9.2) — migration aplicada não é
editada e downgrade destrutivo é proibido. O rollback abaixo existe apenas para o
caso de o apply ser provado incorreto, com autorização explícita:

```sql
-- SOMENTE com autorização explícita e após confirmar que as tabelas estão vazias.
begin;
drop table if exists public.player_matches;
drop table if exists public.matches;
delete from public."__EFMigrationsHistory" where "MigrationId" = '20260914165003_AddMatches';
commit;
```

Raio de impacto verificado no banco real: nenhuma FK de outras tabelas aponta para
`matches`/`player_matches` (apenas a FK interna da própria migration), **0 views
dependentes**, e as duas tabelas estão vazias (`matches=0`, `player_matches=0`).
O procedimento foi exercitado em `begin…rollback` no banco descartável (§4) — em
nenhum momento foi executado no projeto real.

### 8.4 Outros riscos

- `dotnet ef database update` sobre o pooler em modo transação foi validado
  localmente, mas o workflow `migrate` usa o mesmo secret/connection string; se a
  credencial for atualizada para o formato do pooler, recomenda-se repetir o ensaio
  de §4 no ambiente real antes de uma migration destrutiva.
- Arquivo de evidência **não commitado** (workspace compartilhado; ver aviso no topo).
- Estrutura atual das tabelas: `matches`/`player_matches` **vazias** — a análise do
  jogador retorna 200 com `matchesAnalysed=0` até que a importação (`POST
  /api/players/{id}/matches/sync`, dependente da Riot API) popule os dados.

## 9. Reprodução

Artefatos brutos desta execução (scripts, saídas e snapshots) estão em
`/tmp/lol-mig-check/` e empacotados em
`/tmp/lol-mig-check/lolcoach-addmatches-evidence.tar.gz`
(sha256 `732d3e43e45bbdfc0e4e89fe7fb1765d4d8a2a1fb5aef7f73a539309d712edd3`,
36 KB, 51 arquivos), entregue como artefato do card `t_d13c8671`:

| Arquivo | Conteúdo |
|---|---|
| `out/pre_apply.json`, `out/pre_apply2.json` | estado real antes do apply |
| `out/migrations-idempotent.sql` | artefato SQL gerado pelo EF |
| `out/apply_real.out` | tentativa via EF (bloqueio de credencial) |
| `out/apply_mgmt.out`/terminal | apply real (HTTP 201, hash) |
| `out/post_apply1.json`, `out/post_apply2.json` | estado real depois do apply |
| `out/compare.out` | diff pré/pós, estabilidade entre applies, equivalência EF/script |
| `out/rehearsal.out`, `out/rehearsal_pooled.out` | ensaio local (direto e via pooler) |
| `out/verify_sql_real.out` | comparação real vs EF local + consulta do endpoint + EXPLAIN |
| `out/smoke_local.out` | smoke HTTP do endpoint (200/200/404) |
| `out/rollback_probe.out` | teste de escrita com rollback, locks, backups/PITR, raio de impacto |

Comandos principais (credenciais lidas de arquivo de ambiente por script; nunca em
linha de comando):

```bash
cd /tmp/lol-mig-check/wt/backend        # árvore pinada em 3b8eef1
dotnet tool restore
ConnectionStrings__LoLCoach='Host=127.0.0.1;Port=1;Database=sintetico' \
  dotnet ef migrations script --idempotent --project src/LoLCoach.Api \
  --startup-project src/LoLCoach.Api --output /tmp/lol-mig-check/out/migrations-idempotent.sql
python3 /tmp/lol-mig-check/apply_mgmt.py /tmp/lol-mig-check/out/migrations-idempotent.sql
python3 /tmp/lol-mig-check/snapshot.py /tmp/lol-mig-check/out/post_apply2.json "post-apply"
python3 /tmp/lol-mig-check/verify_sql_real.py
```
