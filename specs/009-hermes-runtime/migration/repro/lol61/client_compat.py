#!/usr/bin/env python3
"""LOL-61 — compatibilidade real cliente<->SQL publicado.

Usa o cliente de verdade do pacote `hermes-agent-runtime` (SupabaseStore) contra o
proxy HTTPS local (equivalente ao Kong: /rest/v1 -> PostgREST 14.5) servindo a fixture
com o SQL publicado aplicado. Ao final, cada payload gravado pelo SQL e validado pelo
contrato Python (`validate_event_payload`) e relido por REST.
"""
from __future__ import annotations

import json
import sys
import urllib.parse
import urllib.request

sys.path.insert(0, "/home/alexandre/hermes-agent-runtime/src")

from hermes_agent_runtime.payloads import validate_event_payload  # noqa: E402
from hermes_agent_runtime.runtime import ExecutionContext, RunConflict, new_run_id  # noqa: E402
from hermes_agent_runtime.supabase import SupabaseStore  # noqa: E402

URL = "https://127.0.0.1:8443"
KEY = open("/tmp/lol61/jwt_service_role.txt", encoding="utf-8").read().strip()
ISSUE = "LOL61-CLI-A"
CHECKS: list[tuple[str, bool, str]] = []


def check(name: str, ok: bool, detail: str = "") -> None:
    CHECKS.append((name, ok, detail))
    print(f"{'PASS' if ok else 'FAIL'} {name} {detail}".rstrip())


def get(path: str) -> list[dict]:
    request = urllib.request.Request(
        URL + "/rest/v1/" + path,
        headers={"apikey": KEY, "Authorization": "Bearer " + KEY, "Accept": "application/json"},
    )
    with urllib.request.urlopen(request, timeout=20) as response:
        return json.loads(response.read() or b"[]")


def context(issue: str) -> ExecutionContext:
    return ExecutionContext(run_id=new_run_id(), linear_issue_id=issue, agent="devops",
                            risk="medium", execution_mode="auto", environment="staging")


def main() -> int:
    store = SupabaseStore(URL, KEY)
    ctx = context(ISSUE)

    store.claim(ctx, 90)
    check("cliente claim via proxy HTTPS", True, ctx.run_id)
    store.heartbeat(ctx, 90)
    check("cliente heartbeat", True)

    # conflito enquanto o lock esta ativo: o cliente deve traduzir em RunConflict
    conflicted = context(ISSUE)
    try:
        store.claim(conflicted, 60)
        check("conflito (claim concorrente) -> RunConflict", False, "claim deveria falhar")
    except RunConflict:
        check("conflito (claim concorrente) -> RunConflict", True, conflicted.run_id)
    rejected = get(f"agent_runs?run_id=eq.{urllib.parse.quote(conflicted.run_id)}&select=run_id")
    check("run rejeitado nao persistido", rejected == [], json.dumps(rejected))
    lock_holder = get(f"agent_execution_locks?linear_issue_id=eq.{ISSUE}&select=run_id")
    check("lock permanece com o detentor", lock_holder == [{"run_id": ctx.run_id}], json.dumps(lock_holder))
    rejected_event = get(f"agent_events?linear_issue_id=eq.{ISSUE}&event_type=eq.lock.rejected&select=payload")
    check("lock.rejected auditado no detentor",
          rejected_event and rejected_event[0]["payload"].get("attempted_run_id") == conflicted.run_id,
          json.dumps(rejected_event)[:120])

    store.event(ctx, "tests.completed", {"tests_status": "passed", "command": "python3 -m unittest",
                                        "exit_code": 0, "total": 23, "passed": 23, "failed": 0,
                                        "skipped": 0, "duration_ms": 1500})
    store.event(ctx, "review.completed", {"review_status": "approved", "reviewer": "code-reviewer"})
    store.finish(ctx, "completed", {"commit_sha": "c" * 40,
                                    "pull_request_url": "https://github.com/Sharpista/LoLCoach/pull/31",
                                    "tests_status": "passed", "review_status": "approved"})
    check("cliente finish completed", True)

    try:
        store.finish(ctx, "failed", {})
        check("finish tardio recusado", False, "finish deveria falhar")
    except RuntimeError:
        check("finish tardio recusado", True)
    check("lock liberado no finish",
          get(f"agent_execution_locks?linear_issue_id=eq.{ISSUE}&select=run_id") == [])

    rows = get(f"agent_events?run_id=eq.{urllib.parse.quote(ctx.run_id)}&select=event_type,payload&order=id")
    kinds = [row["event_type"] for row in rows]
    check("ciclo de eventos persistido", kinds == ["run.created", "lock.acquired", "run.started",
                                                   "lock.rejected", "tests.completed", "review.completed",
                                                   "run.completed", "lock.released"], json.dumps(kinds))
    check("payloads nao nulos", all(isinstance(row["payload"], dict) for row in rows))

    invalid = []
    for row in rows:
        try:
            validate_event_payload(row["event_type"], row["payload"])
        except Exception as exc:  # noqa: BLE001
            invalid.append((row["event_type"], type(exc).__name__, str(exc)))
    check("payloads do SQL aceitos pelo contrato Python", not invalid, json.dumps(invalid)[:200])

    run_rows = get(f"agent_runs?run_id=eq.{urllib.parse.quote(ctx.run_id)}"
                   "&select=status,tests_status,review_status,commit_sha")
    check("run.completed persistido",
          bool(run_rows) and run_rows[0]["status"] == "completed" and run_rows[0]["tests_status"] == "passed"
          and run_rows[0]["review_status"] == "approved", json.dumps(run_rows)[:160])

    failed = [name for name, ok, _ in CHECKS if not ok]
    print(f"== client compat: {len(CHECKS) - len(failed)}/{len(CHECKS)} PASS ==")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
