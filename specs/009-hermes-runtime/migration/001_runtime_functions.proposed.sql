-- LOL-59 (devops) — PROPOSTA de ajuste do migration do runtime.
--
-- Base: /home/alexandre/hermes-agent-runtime/sql/001_runtime_functions.sql
--       revisao de working tree de 2026-09-24T20:31+02:00,
--       sha256 684ba8a9b470fc3e9b6893d9796c589d9ae9773f9dfd43491c2983f89f0051ca
--       (HEAD ed6a2d9 = f160742f0b68cfaccb82c64d4666da1a1c18fbbfbc678795c8cdca9d32b1a3ac, claim RETURNS void)
--
-- Ajustes propostos e motivo (evidencia em evidence/lol-59-runtime-supabase-validation.md):
--   A1  preamble DROP FUNCTION IF EXISTS: torna o arquivo reaplicavel e seguro a mudanca
--       de tipo de retorno (hoje void -> boolean aborta a transacao inteira com 42P13).
--   A2  handler de conflito cobrindo TAMBEM o insert em agent_runs: hoje o conflito real
--       (issue ja possui run ativo) falha em uq_agent_runs_active_issue antes do handler de
--       lock, portanto 'lock.rejected' nunca e gravado nesse caminho.
--   A3  run criado e depois rejeitado fica 'failed' (nunca 'queued'), evitando bloquear a
--       issue pela partial unique index.
--   A4  revoke de USAGE/SELECT/UPDATE da sequence agent_events_id_seq para anon/authenticated.
-- Sem mudanca de regra de negocio: status, TTL, ownership, grants de RPC e semantica de
-- 'blocked' (que continua exigindo intervencao humana) permanecem identicos.
--
-- NAO aplicar em producao por este card. Reconciliar com a revisao canonica do pacote
-- hermes-agent-runtime antes de aplicar (ver secao "Hotspot de arquivo" na evidencia).

begin;

-- A1: permite reaplicar e permite mudanca de assinatura/tipo de retorno.
drop function if exists public.hermes_claim_run(text,text,text,text,text,text,integer);
drop function if exists public.hermes_heartbeat_run(text,text,integer);
drop function if exists public.hermes_finish_run(text,text,text,jsonb);
drop function if exists public.hermes_recover_expired_lock(text);

create or replace function public.hermes_claim_run(
  p_run_id text, p_issue_id text, p_agent text, p_risk text,
  p_mode text, p_environment text, p_ttl_seconds integer
) returns boolean language plpgsql security invoker set search_path = '' as $$
declare
  v_created boolean := false;
  v_holder_run text;
  v_holder_agent text;
begin
  if p_ttl_seconds < 10 or p_ttl_seconds > 86400 then
    raise exception 'Invalid lock TTL';
  end if;
  -- Serializes claims against recovery for this issue. The unique index/PK is the final guard.
  perform pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_issue_id, 0));
  begin
    -- A2: run e lock na mesma transacao; qualquer conflito cai no handler abaixo.
    insert into public.agent_runs
      (run_id, linear_issue_id, agent, status, risk, execution_mode, environment)
    values (p_run_id, p_issue_id, p_agent, 'queued', p_risk, p_mode, p_environment);
    v_created := true;
    insert into public.agent_execution_locks
      (linear_issue_id, run_id, agent, expires_at)
    values (p_issue_id, p_run_id, p_agent, pg_catalog.now() + pg_catalog.make_interval(secs => p_ttl_seconds));
  exception when unique_violation then
    -- A3: run recem-criado e rejeitado nunca permanece 'queued'.
    if v_created then
      update public.agent_runs set status = 'failed', finished_at = pg_catalog.now(), error = 'RunConflict'
      where run_id = p_run_id and linear_issue_id = p_issue_id and status = 'queued';
    end if;
    -- Auditoria contra quem detem o lock/issue: o run_id tentado pode nao existir.
    select l.run_id, l.agent into v_holder_run, v_holder_agent
    from public.agent_execution_locks l
    where l.linear_issue_id = p_issue_id;
    if v_holder_run is null then
      select r.run_id, r.agent into v_holder_run, v_holder_agent
      from public.agent_runs r
      where r.linear_issue_id = p_issue_id
        and r.status in ('queued', 'running', 'reviewing', 'blocked')
      order by r.started_at desc
      limit 1;
    end if;
    if v_holder_run is not null then
      insert into public.agent_events (run_id, linear_issue_id, agent, event_type, payload)
      values (v_holder_run, p_issue_id, v_holder_agent, 'lock.rejected',
              pg_catalog.jsonb_build_object('reason', 'RunConflict',
                                            'attempted_run_id', p_run_id));
    end if;
    return false;
  end;
  update public.agent_runs set status = 'running', heartbeat_at = pg_catalog.now()
  where run_id = p_run_id and linear_issue_id = p_issue_id;
  insert into public.agent_events (run_id, linear_issue_id, agent, event_type)
  values (p_run_id, p_issue_id, p_agent, 'run.created'),
         (p_run_id, p_issue_id, p_agent, 'lock.acquired'),
         (p_run_id, p_issue_id, p_agent, 'run.started');
  return true;
end;
$$;

create or replace function public.hermes_heartbeat_run(
  p_run_id text, p_issue_id text, p_ttl_seconds integer
) returns boolean language plpgsql security invoker set search_path = '' as $$
declare v_agent text;
begin
  if p_ttl_seconds < 10 or p_ttl_seconds > 86400 then
    raise exception 'Invalid lock TTL';
  end if;
  update public.agent_execution_locks
  set heartbeat_at = pg_catalog.now(),
      expires_at = pg_catalog.now() + pg_catalog.make_interval(secs => p_ttl_seconds)
  where linear_issue_id = p_issue_id and run_id = p_run_id
    and expires_at > pg_catalog.now()
  returning agent into v_agent;
  if v_agent is null then return false; end if;
  update public.agent_runs set heartbeat_at = pg_catalog.now()
  where run_id = p_run_id and status in ('running', 'reviewing');
  return found;
end;
$$;

create or replace function public.hermes_finish_run(
  p_run_id text, p_issue_id text, p_status text, p_fields jsonb default '{}'::jsonb
) returns boolean language plpgsql security invoker set search_path = '' as $$
declare v_agent text;
begin
  if p_status not in ('completed', 'failed', 'blocked', 'canceled') then
    raise exception 'Invalid final status';
  end if;
  select agent into v_agent from public.agent_execution_locks
  where linear_issue_id = p_issue_id and run_id = p_run_id for update;
  if not found then return false; end if;
  update public.agent_runs set
    status = p_status,
    finished_at = pg_catalog.now(),
    commit_sha = p_fields->>'commit_sha',
    pull_request_url = p_fields->>'pull_request_url',
    railway_deployment_id = p_fields->>'railway_deployment_id',
    tests_status = p_fields->>'tests_status',
    review_status = p_fields->>'review_status',
    error = p_fields->>'error'
  where run_id = p_run_id and linear_issue_id = p_issue_id
    and status in ('running', 'reviewing');
  if not found then return false; end if;
  insert into public.agent_events (run_id, linear_issue_id, agent, event_type)
  values (p_run_id, p_issue_id, v_agent, 'run.' || p_status),
         (p_run_id, p_issue_id, v_agent, 'lock.released');
  delete from public.agent_execution_locks
  where linear_issue_id = p_issue_id and run_id = p_run_id;
  return true;
end;
$$;

-- 'blocked' e terminal no runtime mas permanece na partial unique index
-- uq_agent_runs_active_issue: nenhum claim novo entra para a issue ate um humano
-- liberar. Runbook (service_role / dashboard, jamais pelo runtime):
--   update public.agent_runs set status = 'canceled', finished_at = now(),
--          error = 'ReleasedByOperator'
--   where linear_issue_id = '<ISSUE>' and status = 'blocked';
create or replace function public.hermes_recover_expired_lock(p_issue_id text)
returns boolean language plpgsql security invoker set search_path = '' as $$
declare v_lock public.agent_execution_locks%rowtype;
begin
  perform pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_issue_id, 0));
  select * into v_lock from public.agent_execution_locks
  where linear_issue_id = p_issue_id for update;
  if not found or v_lock.expires_at > pg_catalog.now() then return false; end if;
  update public.agent_runs set status = 'failed', finished_at = pg_catalog.now(),
    error = 'LockExpired'
  where run_id = v_lock.run_id and status in ('running', 'reviewing', 'queued');
  insert into public.agent_events (run_id, linear_issue_id, agent, event_type,
    payload)
  values (v_lock.run_id, p_issue_id, v_lock.agent, 'lock.expired',
          pg_catalog.jsonb_build_object('expired_at', v_lock.expires_at)),
         (v_lock.run_id, p_issue_id, v_lock.agent, 'lock.recovered', '{}'::jsonb);
  delete from public.agent_execution_locks
  where linear_issue_id = p_issue_id and run_id = v_lock.run_id;
  return true;
end;
$$;

revoke all on function public.hermes_claim_run(text,text,text,text,text,text,integer) from public, anon, authenticated;
revoke all on function public.hermes_heartbeat_run(text,text,integer) from public, anon, authenticated;
revoke all on function public.hermes_finish_run(text,text,text,jsonb) from public, anon, authenticated;
revoke all on function public.hermes_recover_expired_lock(text) from public, anon, authenticated;
grant execute on function public.hermes_claim_run(text,text,text,text,text,text,integer) to service_role;
grant execute on function public.hermes_heartbeat_run(text,text,integer) to service_role;
grant execute on function public.hermes_finish_run(text,text,text,jsonb) to service_role;
grant execute on function public.hermes_recover_expired_lock(text) to service_role;

-- A4: hardening da sequence (baseline Supabase deixa anon/authenticated com USAGE/SELECT/UPDATE).
revoke all on sequence public.agent_events_id_seq from anon, authenticated;
grant usage, select on sequence public.agent_events_id_seq to service_role;

commit;
