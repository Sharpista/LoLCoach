#!/usr/bin/env python3
"""LOL-61 — aplica a revisao de payloads (bytes publicados, sha256 bf752895...) no
projeto Supabase tsfdsjmostggtxckmirr, em transacao unica, via Management API.

Autorizacao: registrada no card t_afa9faf1 (orquestrador). Escreve apenas DDL de RPC
+ grants da sequence; nao altera tabelas, dados funcionais, secrets ou deploy.
"""
from __future__ import annotations

import hashlib
import json
import os
import sys
import time
import urllib.error
import urllib.request

PROJECT_REF = "tsfdsjmostggtxckmirr"
ENV_FILE = os.path.expanduser("~/.hermes/shared/.env")
SQL_FILE = "/tmp/lol61/published.sql"
EXPECTED_SHA = "bf752895693990812933e026a260fde78db3f56d956b0d1777162087ab365537"


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


def query(sql: str, token: str, timeout: int = 120) -> object:
    request = urllib.request.Request(
        f"https://api.supabase.com/v1/projects/{PROJECT_REF}/database/query",
        data=json.dumps({"query": sql}).encode("utf-8"),
        headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(request, timeout=timeout) as response:
        return response.status, json.loads(response.read().decode("utf-8") or "null")


def main() -> int:
    script = open(SQL_FILE, "r", encoding="utf-8").read()
    digest = hashlib.sha256(script.encode("utf-8")).hexdigest()
    print(f"artefato: {SQL_FILE} sha256={digest} bytes={len(script.encode())}")
    if digest != EXPECTED_SHA:
        print("ABORTADO: hash diferente do publicado (bf752895...)")
        return 2
    token = env_value("SUPABASE_ACCESS_TOKEN")

    started = time.monotonic()
    try:
        status, body = query(script, token)
    except urllib.error.HTTPError as exc:
        print(f"APPLY FALHOU http={exc.code} body={exc.read().decode()[:500]}")
        return 1
    elapsed = time.monotonic() - started
    print(f"APPLY http={status} em {elapsed:.2f}s body={json.dumps(body)[:200]}")

    _, inventory = query("""
        select p.proname as name, pg_get_function_result(p.oid) as result, p.prosecdef as secdef,
               coalesce(p.proconfig::text,'') as proconfig,
               coalesce((select string_agg(case when a.grantee=0 then 'PUBLIC'
                       else pg_catalog.pg_get_userbyid(a.grantee) end || ':' || a.privilege_type, ',' order by 1)
                    from aclexplode(p.proacl) a),'') as acl,
               pg_catalog.md5(pg_get_functiondef(p.oid)) as def_md5,
               pg_catalog.length(pg_get_functiondef(p.oid)) as def_len
        from pg_proc p join pg_namespace n on n.oid = p.pronamespace
        where n.nspname='public' and p.proname like 'hermes%' order by p.proname
    """, token)
    print("== pos-apply inventory ==")
    for row in inventory:
        print(json.dumps(row, sort_keys=True, default=str))
    _, markers = query("""
        select pg_get_functiondef(p.oid) like '%jsonb_strip_nulls%' as has_strip_nulls,
               pg_get_functiondef(p.oid) like '%finished_status%' as has_finished_status,
               pg_get_functiondef(p.oid) like '%previous_run_id%' as has_previous_run_id,
               p.proname
        from pg_proc p join pg_namespace n on n.oid = p.pronamespace
        where n.nspname='public' and p.proname like 'hermes%' order by p.proname
    """, token)
    print("== marcadores de payload ==")
    for row in markers:
        print(json.dumps(row, sort_keys=True, default=str))
    with open("/tmp/lol61/real_apply.json", "w", encoding="utf-8") as handle:
        json.dump({"sha256": digest, "http": status, "seconds": elapsed, "inventory": inventory,
                   "markers": markers}, handle, indent=2, sort_keys=True, default=str)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
