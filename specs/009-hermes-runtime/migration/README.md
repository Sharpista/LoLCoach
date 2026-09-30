# Migration do runtime Hermes (LOL-59) — estado aplicado

Dono: `devops` (card `t_cd2022e2` / LOL-59). Um único dono das migrations do runtime.

## Arquivos

| Arquivo | Papel |
|---|---|
| `001_runtime_functions.proposed.sql` | SQL canônico do runtime, **byte-idêntico** a `hermes-agent-runtime/sql/001_runtime_functions.sql`. Carrega a revisão aplicada no run 117 (LOL-59) e, desde LOL-61, a revisão de payloads por evento **ainda não aplicada** (ver "Revisão pendente de aplicação"). |
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
# bf752895…  -> revisao canonica atual (LOL-61, payloads por evento) — NAO aplicada
# 2cdb809d…  -> hash do artefato aplicado no run 117 (LOL-59); confere com
#               ../evidence/lol-59-runtime-supabase-apply-run117.md
python3 specs/009-hermes-runtime/migration/repro/remote/720_catalog_after.py
```

## Revisão pendente de aplicação (LOL-61 — payloads por evento)

`001_runtime_functions.proposed.sql` foi sincronizado com o SQL canônico do pacote publicado em `Sharpista/hermes_agent_runtime` (PR #3, commit `ddaf744d45e906876e887933f4ad6ab18f4cf1a6`, merge `8f2e2d42e8372d18c0e08278773704b257a9a202`). A revisão adiciona os payloads JSON por evento exigidos pela Spec 009 §6.4/§6.7.

| Estado | sha256 | Bytes |
|---|---|---|
| Artefato **aplicado** no projeto `tsfdsjmostggtxckmirr` (run 117, LOL-59) | `2cdb809da5e4e27f8bf0bf4be18090625c2f1886c3fa2f35e744e110fa5bc9c5` | 9502 |
| Revisão **pendente** agora no arquivo (LOL-61) | `bf752895693990812933e026a260fde78db3f56d956b0d1777162087ab365537` | 10283 |

- Projeção de banco: a revisão pendente **não foi aplicada** e nenhuma função `public.hermes_*` do projeto foi alterada por esta entrega. Aplicar exige card de `devops` com autorização explícita, revalidação por hash depois da aplicação e uso do `rollback_001_runtime_functions.sql` se necessário.
- Delta funcional: `hermes_claim_run` grava payload em `run.created`/`lock.acquired`/`run.started`; `hermes_finish_run` valida `p_fields` como objeto JSON e grava o payload por status final em `run.<status>` + `lock.released.finished_status`; `hermes_recover_expired_lock` enriquece `lock.expired` (`last_heartbeat_at`) e `lock.recovered` (`previous_run_id`). Nenhum status, TTL, ownership ou grant de RPC mudou.
- Evidência completa (hashes do candidato revisado, tabela de verificação, smoke em PostgreSQL 16 e releitura remota): `../evidence/runtime-event-payload-implementation.md`.

## Nota sobre o cabeçalho do arquivo

O cabeçalho devops da proposta do run 115 ("NAO aplicar em producao por este card", base `684ba8a9…` + ajustes A1–A4) foi substituído pelo cabeçalho do SQL canônico do pacote quando a cópia LoLSaas passou a ser sincronizada com `hermes-agent-runtime/sql/001_runtime_functions.sql` (LOL-61), para manter os dois arquivos byte-idênticos. A proveniência do artefato aplicado (base `684ba8a9…` + A1–A4 → `2cdb809d…`) continua registrada em `../evidence/lol-59-runtime-supabase-validation.md` e `../evidence/lol-59-runtime-supabase-apply-run117.md`.

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
