# Migration do runtime Hermes (LOL-59) — estado aplicado

Dono: `devops` (card `t_cd2022e2` / LOL-59). Um único dono das migrations do runtime.

## Arquivos

| Arquivo | Papel |
|---|---|
| `001_runtime_functions.proposed.sql` | **artefato aplicado**. Bytes congelados: aplicar/verificar sempre por hash, nunca por reinterpretação. |
| `rollback_001_runtime_functions.sql` | rollback preparado (drop das 4 RPCs + restauração do grant de sequence). **Não executado** — o smoke passou. |
| `repro/` | reprodução local fiel (PostgreSQL 17 + PostgREST 14.5) e sondas remotas somente leitura. |
| `repro/remote/` | scripts da aplicação e validação remota (run 117). |
| `../evidence/lol-59-runtime-supabase-validation.md` | validação pré-aplicação (run 115). |
| `../evidence/lol-59-runtime-supabase-apply-run117.md` | **aplicação + validação remota pós-aplicação (run 117)**. |

## Artefato aplicado

- Projeto: `tsfdsjmostggtxckmirr` (LoLCoach), PostgreSQL 17.6, API REST `https://tsfdsjmostggtxckmirr.supabase.co`.
- `sha256(001_runtime_functions.proposed.sql)` = `2cdb809da5e4e27f8bf0bf4be18090625c2f1886c3fa2f35e744e110fa5bc9c5` (9502 bytes).
- Aplicado em 2026-09-24 (run 117) via Management API (`POST /v1/projects/<ref>/database/query`), com autorização explícita registrada no card. Transação única (`begin; … commit;`).
- Conteúdo: 4 funções `public.hermes_*` (`security invoker`, `search_path=''`, `returns boolean`), `revoke` de EXECUTE para `public/anon/authenticated`, `grant execute` para `service_role`, hardening da sequence `agent_events_id_seq` (A4). **Sem** DDL de tabela/coluna/índice/constraint e sem escrita de dados.

Verificação rápida (sem expor valores):

```bash
sha256sum specs/009-hermes-runtime/migration/001_runtime_functions.proposed.sql
# 2cdb809d…  -> confere com o artefato aplicado no projeto
python3 specs/009-hermes-runtime/migration/repro/remote/720_catalog_after.py
```

## Nota sobre o cabeçalho do arquivo

O cabeçalho de `001_runtime_functions.proposed.sql` ainda diz "NAO aplicar em producao por
este card" — texto correto no momento da proposta (run 115) e mantido **de propósito**: o
arquivo é o artefato aplicado e seus bytes estão congelados por hash. A autorização de
aplicação e o resultado estão em `../evidence/lol-59-runtime-supabase-apply-run117.md`.

## Rollback

```bash
# equivalente ao arquivo rollback_001_runtime_functions.sql, transacional
python3 specs/009-hermes-runtime/migration/repro/remote/900_rollback.py   # ver evidência
```

Efeito: remove as 4 funções e restaura `usage, select, update` da sequence para
`anon`/`authenticated` (estado pré-A4). Não afeta tabelas nem dados.

## RPCs aplicadas

| Função | Assinatura | Retorno |
|---|---|---|
| `hermes_claim_run` | `(text,text,text,text,text,text,integer)` | `boolean` (`true` = lock obtido, `false` = conflito) |
| `hermes_heartbeat_run` | `(text,text,integer)` | `boolean` (renova se o lock for do run e não estiver vencido) |
| `hermes_finish_run` | `(text,text,text,jsonb)` | `boolean` (status final, campos e release do lock) |
| `hermes_recover_expired_lock` | `(text)` | `boolean` (recupera lock vencido e falha o run) |

EXECUTE: apenas `postgres` (owner) e `service_role`. `anon`/`authenticated` recebem
`401 {42501}` no REST.
