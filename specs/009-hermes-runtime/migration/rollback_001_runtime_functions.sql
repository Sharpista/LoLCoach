-- LOL-59 (devops) — ROLLBACK da migration 001_runtime_functions.sql.
--
-- PREPARADO, NAO EXECUTADO. Executar somente se o smoke pos-aplicacao falhar
-- ou por decisao explicita de reverter as RPCs do runtime.
--
-- Efeito: remove as quatro funcoes public.hermes_* e restaura o privilegio de
-- sequence do baseline Supabase. Nao toca tabelas, colunas, indices, constraints
-- ou dados.
--
-- Referencia: a alternativa por funcao e
--   drop function if exists public.<nome>(<assinatura>);
--
-- Verificacao esperada apos o rollback:
--   select count(*) from pg_proc p join pg_namespace n on n.oid=p.pronamespace
--    where n.nspname='public' and p.proname like 'hermes\_%';  -- 0
--   POST /rest/v1/rpc/hermes_claim_run  -> 404 PGRST202
begin;

drop function if exists public.hermes_claim_run(text,text,text,text,text,text,integer);
drop function if exists public.hermes_heartbeat_run(text,text,integer);
drop function if exists public.hermes_finish_run(text,text,text,jsonb);
drop function if exists public.hermes_recover_expired_lock(text);

-- Restaura o estado pre-A4 da sequence (baseline Supabase).
grant usage, select, update on sequence public.agent_events_id_seq to anon, authenticated;

commit;
