#!/usr/bin/env python3
"""LOL-61 — contrato HTTP do PostgREST 14.5 (mesma versao do projeto) sobre a
fixture local com o SQL publicado aplicado. Prova por papel:
  - service_role: 200 nas RPCs, 201 no insert de evento
  - anon/authenticated: 401 com SQLSTATE 42501 (sem token valido / sem EXECUTE)
"""
from __future__ import annotations

import json
import sys
import urllib.error
import urllib.request

BASE = "http://127.0.0.1:3300"
SERVICE_JWT = open("/tmp/lol61/jwt_service_role.txt", encoding="utf-8").read().strip()
AUTH_JWT = open("/tmp/lol61/jwt_authenticated.txt", encoding="utf-8").read().strip()
ANON_KEY = "lol61-anon-apikey"  # qualquer apikey sem Authorization -> papel anon

CHECKS: list[tuple[str, bool, str]] = []


def check(name: str, ok: bool, detail: str = "") -> None:
    CHECKS.append((name, ok, detail))
    print(f"{'PASS' if ok else 'FAIL'} {name} {detail}".rstrip())


def call(method: str, path: str, *, headers: dict[str, str], body: object | None = None) -> tuple[int, str]:
    data = json.dumps(body).encode() if body is not None else None
    request = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=20) as response:
            return response.status, response.read().decode()
    except urllib.error.HTTPError as exc:
        return exc.code, exc.read().decode()


def main() -> int:
    svc = {"apikey": SERVICE_JWT, "Authorization": "Bearer " + SERVICE_JWT, "Content-Type": "application/json"}
    anon = {"apikey": ANON_KEY, "Content-Type": "application/json"}
    auth = {"apikey": AUTH_JWT, "Authorization": "Bearer " + AUTH_JWT, "Content-Type": "application/json"}

    status, body = call("POST", "/rpc/hermes_claim_run", headers=svc, body={
        "p_run_id": "run_11111111111111111111111111", "p_issue_id": "LOL61-HTTP-A",
        "p_agent": "devops", "p_risk": "low", "p_mode": "auto", "p_environment": "local",
        "p_ttl_seconds": 60})
    check("service_role claim -> 200 true", status == 200 and body.strip() == "true", f"(http {status} {body[:40]})")

    status, body = call("POST", "/rpc/hermes_claim_run", headers=anon, body={
        "p_run_id": "run_22222222222222222222222222", "p_issue_id": "LOL61-HTTP-B",
        "p_agent": "anon", "p_risk": "low", "p_mode": "auto", "p_environment": "local",
        "p_ttl_seconds": 60})
    check("anon sem token -> 401 42501", status == 401 and "42501" in body, f"(http {status} {body[:60]})")

    status, body = call("POST", "/rpc/hermes_claim_run", headers=auth, body={
        "p_run_id": "run_33333333333333333333333333", "p_issue_id": "LOL61-HTTP-C",
        "p_agent": "authenticated", "p_risk": "low", "p_mode": "auto", "p_environment": "local",
        "p_ttl_seconds": 60})
    # Com JWT valido de um papel sem EXECUTE o PostgREST responde 403 (nao 401):
    # o 401 fica reservado para quando nao consegue resolver o papel (anon sem token).
    check("authenticated com JWT -> 403 42501", status == 403 and "42501" in body, f"(http {status} {body[:60]})")

    status, body = call("POST", "/agent_events", headers=svc, body={
        "run_id": "run_11111111111111111111111111", "linear_issue_id": "LOL61-HTTP-A",
        "agent": "devops", "event_type": "tests.completed",
        "payload": {"tests_status": "passed", "total": 3}})
    check("service_role insert evento -> 201", status == 201, f"(http {status} {body[:60]})")

    status, body = call("POST", "/rpc/hermes_claim_run", headers=svc, body={
        "p_run_id": "run_44444444444444444444444444", "p_issue_id": "LOL61-HTTP-A",
        "p_agent": "devops", "p_risk": "low", "p_mode": "auto", "p_environment": "local",
        "p_ttl_seconds": 60})
    check("conflito de lock -> 200 false", status == 200 and body.strip() == "false", f"(http {status} {body[:40]})")

    status, body = call("POST", "/rpc/hermes_finish_run", headers=svc, body={
        "p_run_id": "run_11111111111111111111111111", "p_issue_id": "LOL61-HTTP-A",
        "p_status": "completed", "p_fields": {"tests_status": "passed"}})
    check("service_role finish -> 200 true", status == 200 and body.strip() == "true", f"(http {status} {body[:40]})")

    status, body = call("GET", "/agent_events?run_id=eq.run_11111111111111111111111111&select=event_type,payload", headers=svc)
    rows = json.loads(body) if status == 200 else []
    kinds = set(row["event_type"] for row in rows)
    expected = {"run.created", "lock.acquired", "run.started", "lock.rejected", "tests.completed",
                "run.completed", "lock.released"}
    check("GET eventos do run -> ciclo completo", status == 200 and expected <= kinds,
          f"(http {status} rows={len(rows)} {sorted(kinds)})")
    check("payloads preenchidos via REST", all(isinstance(row["payload"], dict) for row in rows),
          json.dumps(rows)[:160])

    failed = [name for name, ok, _ in CHECKS if not ok]
    print(f"== http contract: {len(CHECKS) - len(failed)}/{len(CHECKS)} PASS ==")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
