-- Reset da fixture local (somente o banco de teste em container).
truncate table public.agent_events, public.agent_execution_locks, public.agent_runs restart identity cascade;
select 'reset ok' as status;
