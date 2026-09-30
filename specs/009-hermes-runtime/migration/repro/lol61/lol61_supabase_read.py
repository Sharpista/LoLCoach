#!/usr/bin/env python3
"""LOL-61 — leitura somente-leitura do estado real do projeto Supabase.

Nao escreve nada: todas as queries sao SELECT sobre catalogos/dados.
Uso: python3 lol61_supabase_read.py <saida.json>
"""
from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request

PROJECT_REF = "tsfdsjmostggtxckmirr"
ENV_FILE = os.path.expanduser("~/.hermes/shared/.env")

QUERIES: dict[str, str] = {
    "server": "select current_database() as db, current_setting('server_version') as pg_version, "
              "current_setting('is_superuser') as is_superuser, now() as read_at",
    "rpc_inventory": """
        select p.proname as name,
               pg_get_function_identity_arguments(p.oid) as args,
               pg_get_function_result(p.oid) as result,
               p.prosecdef as secdef,
               coalesce(p.proconfig::text, '') as proconfig,
               coalesce((select string_agg(case when a.grantee = 0 then 'PUBLIC'
                                                else pg_catalog.pg_get_userbyid(a.grantee) end
                                            || ':' || a.privilege_type, ',' order by 1)
                         from aclexplode(p.proacl) a), '') as acl,
               pg_catalog.md5(pg_get_functiondef(p.oid)) as def_md5,
               pg_catalog.length(pg_get_functiondef(p.oid)) as def_len
        from pg_proc p join pg_namespace n on n.oid = p.pronamespace
        where n.nspname = 'public' and p.proname like 'hermes%'
        order by p.proname
    """,
    "rpc_defs": """
        select p.proname as name, pg_get_functiondef(p.oid) as def
        from pg_proc p join pg_namespace n on n.oid = p.pronamespace
        where n.nspname = 'public' and p.proname like 'hermes%'
        order by p.proname
    """,
    "sequence_acl": """
        select c.relname, c.relkind::text as relkind,
               coalesce(c.relacl::text, '(default)') as acl,
               c.relowner::regrole::text as owner,
               has_sequence_privilege('anon', c.oid, 'USAGE') as anon_usage,
               has_sequence_privilege('anon', c.oid, 'SELECT') as anon_select,
               has_sequence_privilege('authenticated', c.oid, 'USAGE') as auth_usage,
               has_sequence_privilege('service_role', c.oid, 'USAGE') as svc_usage
        from pg_class c join pg_namespace n on n.oid = c.relnamespace
        where n.nspname = 'public' and c.relkind = 'S'
        order by c.relname
    """,
    "tables": """
        select c.relname, c.relrowsecurity as rls,
               pg_catalog.pg_get_userbyid(c.relowner) as owner,
               coalesce(c.relacl::text, '(default)') as acl
        from pg_class c join pg_namespace n on n.oid = c.relnamespace
        where n.nspname = 'public' and c.relname like 'agent%' and c.relkind = 'r'
        order by c.relname
    """,
    "columns": """
        select table_name, column_name, data_type, is_nullable, column_default
        from information_schema.columns
        where table_schema = 'public' and table_name like 'agent%'
        order by table_name, ordinal_position
    """,
    "constraints": """
        select c.conname, t.relname as table_name, c.contype::text as type,
               pg_get_constraintdef(c.oid) as def
        from pg_constraint c join pg_class t on t.oid = c.conrelid
        join pg_namespace n on n.oid = t.relnamespace
        where n.nspname = 'public' and t.relname like 'agent%'
        order by t.relname, c.conname
    """,
    "indexes": """
        select i.relname as index_name, t.relname as table_name, pg_get_indexdef(x.indexrelid) as def
        from pg_index x join pg_class i on i.oid = x.indexrelid
        join pg_class t on t.oid = x.indrelid
        join pg_namespace n on n.oid = t.relnamespace
        where n.nspname = 'public' and t.relname like 'agent%'
        order by t.relname, i.relname
    """,
    "table_grants": """
        select table_name, grantee, privilege_type
        from information_schema.role_table_grants
        where table_schema = 'public' and table_name like 'agent%'
        order by table_name, grantee, privilege_type
    """,
    "event_payload_stats": """
        select event_type,
               count(*) as n,
               count(*) filter (where payload is null) as payload_nulls,
               count(*) filter (where payload is not null and jsonb_typeof(payload) <> 'object') as payload_non_object,
               count(*) filter (where payload = '{}'::jsonb) as payload_empty_object,
               min(occurred_at) as first_at, max(occurred_at) as last_at
        from public.agent_events
        group by event_type
        order by event_type
    """,
    "event_payload_totals": """
        select count(*) as events_total,
               count(*) filter (where payload is null) as payload_nulls,
               count(*) filter (where payload is not null) as payload_present,
               (select count(*) from public.agent_runs) as runs_total,
               (select count(*) from public.agent_execution_locks) as locks_total
        from public.agent_events
    """,
    "rpc_effective_privileges": """
        select p.proname,
               has_function_privilege('service_role', p.oid, 'EXECUTE') as svc_execute,
               has_function_privilege('anon', p.oid, 'EXECUTE') as anon_execute,
               has_function_privilege('authenticated', p.oid, 'EXECUTE') as auth_execute
        from pg_proc p join pg_namespace n on n.oid = p.pronamespace
        where n.nspname = 'public' and p.proname like 'hermes%'
        order by p.proname
    """,
}


def env_value(name: str) -> str:
    with open(ENV_FILE, "r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            key, value = line.split("=", 1)
            if key.strip() == name:
                return value.strip().strip('"').strip("'")
    raise SystemExit(f"missing env var: {name}")


def rpc(sql: str, token: str) -> object:
    request = urllib.request.Request(
        f"https://api.supabase.com/v1/projects/{PROJECT_REF}/database/query",
        data=json.dumps({"query": sql}).encode("utf-8"),
        headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(request, timeout=90) as response:
        return json.loads(response.read().decode("utf-8"))


def main() -> int:
    out_path = sys.argv[1] if len(sys.argv) > 1 else "/tmp/lol61/supabase_read.json"
    token = env_value("SUPABASE_ACCESS_TOKEN")
    results: dict[str, object] = {}
    for name, sql in QUERIES.items():
        try:
            results[name] = rpc(sql, token)
        except urllib.error.HTTPError as exc:
            results[name] = {"error": f"HTTP {exc.code}", "detail": exc.read().decode("utf-8")[:400]}
        except Exception as exc:  # noqa: BLE001
            results[name] = {"error": type(exc).__name__}
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as handle:
        json.dump(results, handle, indent=2, sort_keys=True, default=str)

    print("== server ==")
    print(json.dumps(results.get("server"), indent=2, default=str))
    print("== rpc_inventory ==")
    for row in results.get("rpc_inventory", []) or []:
        print(json.dumps(row, default=str))
    print("== rpc_effective_privileges ==")
    for row in results.get("rpc_effective_privileges", []) or []:
        print(json.dumps(row, default=str))
    print("== sequence_acl ==")
    for row in results.get("sequence_acl", []) or []:
        print(json.dumps(row, default=str))
    print("== tables ==")
    for row in results.get("tables", []) or []:
        print(json.dumps(row, default=str))
    print("== event_payload_stats ==")
    for row in results.get("event_payload_stats", []) or []:
        print(json.dumps(row, default=str))
    print("== event_payload_totals ==")
    print(json.dumps(results.get("event_payload_totals"), default=str))
    print(f"== raw json: {out_path} ==")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
