-- LOL-61 — fixture fiel do baseline Supabase (projeto tsfdsjmostggtxckmirr).
-- Reconstruida a partir do catalogo real lido por Management API (ver
-- evidence/lol-61-payload-apply.md). Inclui: roles, default privileges do Supabase
-- (EXECUTE em funcoes e rwU em sequences para anon/authenticated), tabelas, indices,
-- CHECKs, trigger e RLS habilitado sem policies.
begin;

do $$
begin
  if not exists (select 1 from pg_roles where rolname = 'anon') then
    create role anon nologin;
  end if;
  if not exists (select 1 from pg_roles where rolname = 'authenticated') then
    create role authenticated nologin;
  end if;
  if not exists (select 1 from pg_roles where rolname = 'service_role') then
    create role service_role nologin bypassrls;
  end if;
end $$;

-- Baseline Supabase: default privileges de public (default_acl observado no catalogo real).
alter default privileges in schema public grant execute on functions to anon, authenticated, service_role;
alter default privileges in schema public grant select, insert, update, delete, truncate, references, trigger on tables to anon, authenticated, service_role;
alter default privileges in schema public grant usage, select, update on sequences to anon, authenticated, service_role;

create or replace function public.set_updated_at() returns trigger
language plpgsql as $$
begin
  new.updated_at = pg_catalog.now();
  return new;
end;
$$;

create table public.agent_runs (
  id uuid primary key default gen_random_uuid(),
  run_id text not null unique check (run_id ~ '^run_[0-9A-HJKMNP-TV-Z]{26}$'),
  linear_issue_id text not null,
  agent text not null,
  status text not null check (status in ('queued','running','reviewing','blocked','failed','completed','canceled')),
  risk text not null default 'low' check (risk in ('low','medium','high','critical')),
  execution_mode text not null default 'auto' check (execution_mode in ('auto','human','blocked')),
  environment text not null default 'local' check (environment in ('local','preview','staging','production')),
  started_at timestamptz not null default now(),
  finished_at timestamptz,
  heartbeat_at timestamptz not null default now(),
  workspace_path text,
  branch text,
  commit_sha text,
  pull_request_url text,
  railway_deployment_id text,
  tests_status text,
  review_status text,
  error text,
  metadata jsonb not null default '{}'::jsonb,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create unique index uq_agent_runs_active_issue on public.agent_runs (linear_issue_id)
  where status in ('queued','running','reviewing','blocked');
create index idx_agent_runs_agent on public.agent_runs (agent);
create index idx_agent_runs_linear_issue on public.agent_runs (linear_issue_id);
create index idx_agent_runs_started_at on public.agent_runs (started_at desc);
create index idx_agent_runs_status on public.agent_runs (status);

create trigger trg_agent_runs_updated_at before update on public.agent_runs
  for each row execute function public.set_updated_at();

create table public.agent_events (
  id bigint generated always as identity primary key,
  run_id text not null references public.agent_runs(run_id) on delete cascade,
  linear_issue_id text not null,
  agent text not null,
  event_type text not null,
  payload jsonb not null default '{}'::jsonb,
  created_at timestamptz not null default now()
);

create index idx_agent_events_linear_issue_created_at on public.agent_events (linear_issue_id, created_at);
create index idx_agent_events_run_id_created_at on public.agent_events (run_id, created_at);

create table public.agent_execution_locks (
  linear_issue_id text primary key,
  run_id text not null unique references public.agent_runs(run_id) on delete cascade,
  agent text not null,
  acquired_at timestamptz not null default now(),
  heartbeat_at timestamptz not null default now(),
  expires_at timestamptz not null,
  metadata jsonb not null default '{}'::jsonb
);

alter table public.agent_runs enable row level security;
alter table public.agent_events enable row level security;
alter table public.agent_execution_locks enable row level security;

commit;
