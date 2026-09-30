-- LOL-61 — smoke local do contrato de payloads (roda depois do apply do SQL publicado).
-- Cada bloco DO valida um item do contrato e levanta excecao em caso de falha.
\set ON_ERROR_STOP on
begin;

-- ---------------------------------------------------------------- cenario 1: claim
do $$
declare
  v_ok boolean;
  v_payload jsonb;
begin
  v_ok := public.hermes_claim_run('run_00000000000000000000000001', 'LOL61-LOCAL-A', 'devops', 'medium', 'auto', 'staging', 120);
  if v_ok is not true then raise exception 'FAIL claim returned %', v_ok; end if;

  select payload into v_payload from public.agent_events
  where run_id = 'run_00000000000000000000000001' and event_type = 'run.created';
  if v_payload is distinct from '{}'::jsonb then raise exception 'FAIL run.created payload %', v_payload; end if;

  select payload into v_payload from public.agent_events
  where run_id = 'run_00000000000000000000000001' and event_type = 'lock.acquired';
  if v_payload->>'ttl_seconds' <> '120' or (v_payload->>'expires_at') is null then
    raise exception 'FAIL lock.acquired payload %', v_payload;
  end if;

  select payload into v_payload from public.agent_events
  where run_id = 'run_00000000000000000000000001' and event_type = 'run.started';
  if v_payload->>'risk' <> 'medium' or v_payload->>'execution_mode' <> 'auto' or v_payload->>'environment' <> 'staging' then
    raise exception 'FAIL run.started payload %', v_payload;
  end if;

  if exists (select 1 from public.agent_events where run_id = 'run_00000000000000000000000001' and payload is null) then
    raise exception 'FAIL payload nulo apos claim';
  end if;
  raise notice 'PASS cenario 1 claim + payloads run.created/lock.acquired/run.started';
end $$;

-- ------------------------------------------- cenario 2: conflito (lock.rejected)
do $$
declare
  v_ok boolean;
  v_holder text;
  v_payload jsonb;
begin
  v_ok := public.hermes_claim_run('run_00000000000000000000000002', 'LOL61-LOCAL-A', 'devops', 'low', 'auto', 'local', 120);
  if v_ok is not false then raise exception 'FAIL conflito retornou %', v_ok; end if;
  if exists (select 1 from public.agent_runs where run_id = 'run_00000000000000000000000002') then
    raise exception 'FAIL run rejeitado persistido';
  end if;
  select run_id into v_holder from public.agent_execution_locks where linear_issue_id = 'LOL61-LOCAL-A';
  select payload into v_payload from public.agent_events
  where event_type = 'lock.rejected' and linear_issue_id = 'LOL61-LOCAL-A' order by id desc limit 1;
  if v_holder <> 'run_00000000000000000000000001' then raise exception 'FAIL holder %', v_holder; end if;
  if v_payload->>'reason' <> 'RunConflict' or v_payload->>'attempted_run_id' <> 'run_00000000000000000000000002' then
    raise exception 'FAIL lock.rejected payload %', v_payload;
  end if;
  raise notice 'PASS cenario 2 conflito + lock.rejected no detentor + sem run rejeitado';
end $$;

-- --------------------------------------------- cenario 3: heartbeat e expiracao
do $$
declare
  v_ok boolean;
begin
  v_ok := public.hermes_heartbeat_run('run_00000000000000000000000001', 'LOL61-LOCAL-A', 120);
  if v_ok is not true then raise exception 'FAIL heartbeat do detentor retornou %', v_ok; end if;
  v_ok := public.hermes_heartbeat_run('run_00000000000000000000000002', 'LOL61-LOCAL-A', 120);
  if v_ok is not false then raise exception 'FAIL heartbeat de run estranho retornou %', v_ok; end if;
  update public.agent_execution_locks set expires_at = pg_catalog.now() - interval '1 second'
  where linear_issue_id = 'LOL61-LOCAL-A';
  v_ok := public.hermes_heartbeat_run('run_00000000000000000000000001', 'LOL61-LOCAL-A', 120);
  if v_ok is not false then raise exception 'FAIL heartbeat de lock vencido retornou %', v_ok; end if;
  raise notice 'PASS cenario 3 heartbeat (detentor/estranho/vencido)';
end $$;

-- ------------------------------------------ cenario 4: recovery do lock vencido
do $$
declare
  v_ok boolean;
  v_payload jsonb;
begin
  v_ok := public.hermes_recover_expired_lock('LOL61-LOCAL-A');
  if v_ok is not true then raise exception 'FAIL recover retornou %', v_ok; end if;
  select payload into v_payload from public.agent_events
  where event_type = 'lock.expired' and linear_issue_id = 'LOL61-LOCAL-A' order by id desc limit 1;
  if (v_payload->>'expired_at') is null or (v_payload->>'last_heartbeat_at') is null then
    raise exception 'FAIL lock.expired payload %', v_payload;
  end if;
  select payload into v_payload from public.agent_events
  where event_type = 'lock.recovered' and linear_issue_id = 'LOL61-LOCAL-A' order by id desc limit 1;
  if v_payload->>'previous_run_id' <> 'run_00000000000000000000000001' then
    raise exception 'FAIL lock.recovered payload %', v_payload;
  end if;
  if (select status from public.agent_runs where run_id = 'run_00000000000000000000000001') <> 'failed' then
    raise exception 'FAIL run nao marcado failed no recovery';
  end if;
  if exists (select 1 from public.agent_execution_locks where linear_issue_id = 'LOL61-LOCAL-A') then
    raise exception 'FAIL lock residual apos recovery';
  end if;
  v_ok := public.hermes_finish_run('run_00000000000000000000000001', 'LOL61-LOCAL-A', 'completed', '{}'::jsonb);
  if v_ok is not false then raise exception 'FAIL finish tardio retornou %', v_ok; end if;
  -- issue volta a aceitar claim depois do recovery
  v_ok := public.hermes_claim_run('run_00000000000000000000000003', 'LOL61-LOCAL-A', 'devops', 'low', 'auto', 'local', 60);
  if v_ok is not true then raise exception 'FAIL re-claim apos recovery retornou %', v_ok; end if;
  raise notice 'PASS cenario 4 recovery + lock.expired/lock.recovered + finish tardio + re-claim';
end $$;

-- ------------------------------- cenario 5: payload por status final (4 variantes)
do $$
declare
  v_ok boolean;
  v_payload jsonb;
  v_fields jsonb;
begin
  -- completed
  v_fields := pg_catalog.jsonb_build_object(
    'commit_sha', repeat('a', 40),
    'pull_request_url', 'https://github.com/Sharpista/LoLCoach/pull/31',
    'railway_deployment_id', 'deploy-1',
    'tests_status', 'passed',
    'review_status', 'approved',
    'error', null,
    'campo_nao_previsto', 'ignorado');
  v_ok := public.hermes_finish_run('run_00000000000000000000000003', 'LOL61-LOCAL-A', 'completed', v_fields);
  if v_ok is not true then raise exception 'FAIL finish completed retornou %', v_ok; end if;
  select payload into v_payload from public.agent_events
  where run_id = 'run_00000000000000000000000003' and event_type = 'run.completed';
  if v_payload->>'tests_status' <> 'passed' or v_payload->>'review_status' <> 'approved'
     or v_payload->>'commit_sha' <> repeat('a', 40)
     or (v_payload ? 'error') or (v_payload ? 'campo_nao_previsto') then
    raise exception 'FAIL run.completed payload %', v_payload;
  end if;
  select payload into v_payload from public.agent_events
  where run_id = 'run_00000000000000000000000003' and event_type = 'lock.released';
  if v_payload->>'finished_status' <> 'completed' then
    raise exception 'FAIL lock.released payload %', v_payload;
  end if;

  -- failed (com fallback de erro curto e sanitizado)
  v_ok := public.hermes_claim_run('run_00000000000000000000000004', 'LOL61-LOCAL-B', 'devops', 'low', 'auto', 'local', 60);
  if v_ok is not true then raise exception 'FAIL claim B'; end if;
  v_ok := public.hermes_finish_run('run_00000000000000000000000004', 'LOL61-LOCAL-B', 'failed', '{}'::jsonb);
  if v_ok is not true then raise exception 'FAIL finish failed'; end if;
  select payload into v_payload from public.agent_events
  where run_id = 'run_00000000000000000000000004' and event_type = 'run.failed';
  if v_payload->>'error' <> 'RuntimeError' then raise exception 'FAIL run.failed payload %', v_payload; end if;

  -- blocked
  v_ok := public.hermes_claim_run('run_00000000000000000000000005', 'LOL61-LOCAL-C', 'devops', 'low', 'auto', 'local', 60);
  if v_ok is not true then raise exception 'FAIL claim C'; end if;
  v_ok := public.hermes_finish_run('run_00000000000000000000000005', 'LOL61-LOCAL-C', 'blocked',
    pg_catalog.jsonb_build_object('blocked_reason', 'needs_input', 'kanban_task_id', 't_abc123', 'tests_status', 'passed'));
  if v_ok is not true then raise exception 'FAIL finish blocked'; end if;
  select payload into v_payload from public.agent_events
  where run_id = 'run_00000000000000000000000005' and event_type = 'run.blocked';
  if v_payload->>'error' <> 'ExecutionBlocked' or v_payload->>'blocked_reason' <> 'needs_input' then
    raise exception 'FAIL run.blocked payload %', v_payload;
  end if;

  -- canceled
  v_ok := public.hermes_claim_run('run_00000000000000000000000006', 'LOL61-LOCAL-D', 'devops', 'low', 'auto', 'local', 60);
  if v_ok is not true then raise exception 'FAIL claim D'; end if;
  v_ok := public.hermes_finish_run('run_00000000000000000000000006', 'LOL61-LOCAL-D', 'canceled',
    pg_catalog.jsonb_build_object('canceled_by', 'operator', 'reason', 'manual'));
  if v_ok is not true then raise exception 'FAIL finish canceled'; end if;
  select payload into v_payload from public.agent_events
  where run_id = 'run_00000000000000000000000006' and event_type = 'run.canceled';
  if v_payload->>'canceled_by' <> 'operator' then raise exception 'FAIL run.canceled payload %', v_payload; end if;

  raise notice 'PASS cenario 5 payload por status final (completed/failed/blocked/canceled)';
end $$;

-- ------------------------- cenario 6: validacao de entrada invalida (nenhum write)
do $$
declare
  v_ok boolean;
  v_count_before bigint;
  v_count_after bigint;
  v_failed boolean := false;
begin
  select count(*) into v_count_before from public.agent_events;
  v_ok := public.hermes_claim_run('run_00000000000000000000000007', 'LOL61-LOCAL-E', 'devops', 'low', 'auto', 'local', 60);
  if v_ok is not true then raise exception 'FAIL claim E'; end if;

  begin
    perform public.hermes_finish_run('run_00000000000000000000000007', 'LOL61-LOCAL-E', 'completed', '[1,2]'::jsonb);
  exception when others then
    v_failed := true;
  end;
  if not v_failed then raise exception 'FAIL p_fields array aceito'; end if;

  v_failed := false;
  begin
    perform public.hermes_finish_run('run_00000000000000000000000007', 'LOL61-LOCAL-E', 'archived', '{}'::jsonb);
  exception when others then
    v_failed := true;
  end;
  if not v_failed then raise exception 'FAIL status final invalido aceito'; end if;

  v_failed := false;
  begin
    perform public.hermes_claim_run('run_00000000000000000000000008', 'LOL61-LOCAL-F', 'devops', 'low', 'auto', 'local', 5);
  exception when others then
    v_failed := true;
  end;
  if not v_failed then raise exception 'FAIL TTL < 10 aceito'; end if;

  select count(*) into v_count_after from public.agent_events;
  if exists (select 1 from public.agent_events where run_id = 'run_00000000000000000000000007'
             and event_type in ('run.completed','run.failed','run.blocked','run.canceled')) then
    raise exception 'FAIL evento de status persistido em entrada invalida';
  end if;
  if (select status from public.agent_runs where run_id = 'run_00000000000000000000000007') <> 'running' then
    raise exception 'FAIL run alterado por entrada invalida';
  end if;
  raise notice 'PASS cenario 6 p_fields nao-objeto / status invalido / TTL < 10 rejeitados sem write (eventos % -> %)',
    v_count_before, v_count_after;
end $$;

-- ------------------------------------- cenario 7: payload nao nulo e objeto jsonb
do $$
declare
  v_total bigint;
  v_nulls bigint;
  v_non_object bigint;
begin
  select count(*), count(*) filter (where payload is null),
         count(*) filter (where jsonb_typeof(payload) <> 'object')
  into v_total, v_nulls, v_non_object from public.agent_events;
  if v_nulls <> 0 or v_non_object <> 0 then
    raise exception 'FAIL payloads nulos=% nao-objeto=% de %', v_nulls, v_non_object, v_total;
  end if;
  -- compatibilidade com linha legada gravada sem payload (default '{}')
  insert into public.agent_events (run_id, linear_issue_id, agent, event_type)
  values ('run_00000000000000000000000001', 'LOL61-LOCAL-A', 'devops', 'legacy.event');
  if (select payload from public.agent_events where event_type = 'legacy.event') is distinct from '{}'::jsonb then
    raise exception 'FAIL default {} ausente em linha legada';
  end if;
  raise notice 'PASS cenario 7 payload nao nulo/objeto jsonb + default {} legado (total %)', v_total;
end $$;

-- ------------------------------------------- cenario 8: grants e sequence (A4)
do $$
declare
  v_oid oid;
begin
  select p.oid into v_oid from pg_proc p join pg_namespace n on n.oid = p.pronamespace
  where n.nspname = 'public' and p.proname = 'hermes_claim_run';
  if not has_function_privilege('service_role', v_oid, 'EXECUTE') then raise exception 'FAIL service_role sem EXECUTE'; end if;
  if has_function_privilege('anon', v_oid, 'EXECUTE') then raise exception 'FAIL anon com EXECUTE'; end if;
  if has_function_privilege('authenticated', v_oid, 'EXECUTE') then raise exception 'FAIL authenticated com EXECUTE'; end if;
  if exists (
    select 1 from pg_proc p join pg_namespace n on n.oid = p.pronamespace, aclexplode(p.proacl) a
    where n.nspname = 'public' and p.proname like 'hermes%' and a.grantee = 0
  ) then
    raise exception 'FAIL PUBLIC com EXECUTE';
  end if;
  if has_sequence_privilege('anon', 'public.agent_events_id_seq', 'USAGE')
     or has_sequence_privilege('authenticated', 'public.agent_events_id_seq', 'USAGE') then
    raise exception 'FAIL anon/authenticated com USAGE na sequence';
  end if;
  if not has_sequence_privilege('service_role', 'public.agent_events_id_seq', 'USAGE') then
    raise exception 'FAIL service_role sem USAGE na sequence';
  end if;
  raise notice 'PASS cenario 8 grants (RPC e sequence)';
end $$;

rollback;
