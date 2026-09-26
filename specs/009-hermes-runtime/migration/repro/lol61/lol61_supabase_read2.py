#!/usr/bin/env python3
"""LOL-61 — segunda leitura somente-leitura: detalhes necessarios para reconstruir
a fixture local (identity/serial, policies, triggers, roles, RLS)."""
from __future__ import annotations

import json
import os
import urllib.request

PROJECT_REF = "tsfdsjmostggtxckmirr"
ENV_FILE = os.path.expanduser("~/.hermes/shared/.env")

QUERIES = {
    "identity": """
        select table_name, column_name, is_identity, identity_generation, is_generated
        from information_schema.columns
        where table_schema = 'public' and table_name like 'agent%'
        order by table_name, ordinal_position
    """,
    "sequences": """
        select s.schemaname, s.sequencename, s.data_type, s.start_value, s.increment_by,
               pg_get_serial_sequence('public.' || t.relname, a.attname) as owned_by
        from pg_sequences s
        left join pg_class t on t.relname = s.sequencename
        left join pg_attribute a on a.attrelid = t.oid and a.attnum > 0
        where s.schemaname = 'public'
        order by s.sequencename, a.attnum
    """,
    "policies": """
        select schemaname, tablename, policyname, permissive, roles::text, cmd, qual, with_check
        from pg_policies where schemaname = 'public' order by tablename, policyname
    """,
    "triggers": """
        select t.relname, tr.tgname, pg_get_triggerdef(tr.oid) as def
        from pg_trigger tr join pg_class t on t.oid = tr.tgrelid
        join pg_namespace n on n.oid = t.relnamespace
        where n.nspname = 'public' and not tr.tgisinternal
        order by t.relname, tr.tgname
    """,
    "roles": """
        select rolname, rolsuper, rolbypassrls, rolcanlogin, rolvaliduntil is null as valid_forever
        from pg_roles where rolname in ('anon','authenticated','service_role','postgres','public')
        order by rolname
    """,
    "default_privileges": """
        select defaclrole::regrole::text as grantor, defaclnamespace::regnamespace::text as schema_,
               defaclobjtype::text as objtype, defaclacl::text as acl
        from pg_default_acl order by 1, 2, 3
    """,
    "event_payload_stats": """
        select event_type,
               count(*) as n,
               count(*) filter (where payload is null) as payload_nulls,
               count(*) filter (where payload = '{}'::jsonb) as payload_empty,
               count(*) filter (where payload is not null and payload <> '{}'::jsonb) as payload_filled,
               min(created_at) as first_at, max(created_at) as last_at
        from public.agent_events
        group by event_type order by event_type
    """,
    "runs_state": """
        select status, count(*) as n,
               count(*) filter (where finished_at is null) as open_runs
        from public.agent_runs group by status order by status
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
    token = env_value("SUPABASE_ACCESS_TOKEN")
    results = {}
    for name, sql in QUERIES.items():
        try:
            results[name] = rpc(sql, token)
        except Exception as exc:  # noqa: BLE001
            results[name] = {"error": type(exc).__name__, "detail": str(exc)[:300]}
    with open("/tmp/lol61/supabase_read2.json", "w", encoding="utf-8") as handle:
        json.dump(results, handle, indent=2, sort_keys=True, default=str)
    for name in QUERIES:
        print("==", name, "==")
        value = results[name]
        if isinstance(value, list):
            for row in value:
                print(json.dumps(row, sort_keys=True, default=str))
        else:
            print(json.dumps(value, default=str)[:400])
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
