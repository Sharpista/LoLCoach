#!/usr/bin/env python3
"""LOL-61 — smoke controlado no projeto real tsfdsjmostggtxckmirr.

Escreve apenas fixtures sinteticas (prefixo unico por execucao) via PostgREST com a
chave server-side, valida os payloads do contrato, sonda o papel anon e remove as
fixtures (o FK `on delete cascade` de agent_runs limpa eventos e locks). Ao final
compara as contagens globais com o estado pre-apply.
"""
from __future__ import annotations

import hashlib
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

sys.path.insert(0, "/home/alexandre/hermes-agent-runtime/src")
from hermes_agent_runtime.payloads import validate_event_payload  # noqa: E402
from hermes_agent_runtime.runtime import new_run_id  # noqa: E402

PROJECT_REF = "tsfdsjmostggtxckmirr"
ENV_FILE = os.path.expanduser("~/.hermes/shared/.env")
PRE_APPLY_TOTALS = {"events_total": 12, "runs_total": 2, "locks_total": 0}
PREFIX = "LOL61-APPLY-" + str(int(time.time()))
CHECKS: list[tuple[str, bool, str]] = []
CREATED_RUNS: list[str] = []


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


def check(name: str, ok: bool, detail: str = "") -> None:
    CHECKS.append((name, ok, detail))
    print(f"{'PASS' if ok else 'FAIL'} {name} {detail}".rstrip()[:400])


def rest(method: str, path: str, *, key: str, body: object | None = None,
         extra_headers: dict[str, str] | None = None, bearer: bool = True) -> tuple[int, str]:
    data = json.dumps(body).encode() if body is not None else None
    headers = {"apikey": key, "Content-Type": "application/json", "Accept": "application/json"}
    if bearer:
        headers["Authorization"] = "Bearer " + key
    if extra_headers:
        headers.update(extra_headers)
    request = urllib.request.Request(BASE + "/rest/v1/" + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.status, response.read().decode()
    except urllib.error.HTTPError as exc:
        return exc.code, exc.read().decode()


def select(path: str, key: str) -> list[dict]:
    status, body = rest("GET", path, key=key)
    if status != 200:
        return []
    return json.loads(body or "[]")


def events_for(run_id: str, key: str) -> dict[str, dict]:
    rows = select(f"agent_events?run_id=eq.{urllib.parse.quote(run_id)}"
                  "&select=event_type,payload&order=id", key)
    return {row["event_type"]: row["payload"] for row in rows}


def claim(key: str, issue: str, ttl: int = 90, risk: str = "medium", mode: str = "auto",
          environment: str = "staging") -> tuple[str, int, str]:
    run_id = new_run_id()
    CREATED_RUNS.append(run_id)
    status, body = rest("POST", "rpc/hermes_claim_run", key=key, body={
        "p_run_id": run_id, "p_issue_id": issue, "p_agent": "devops", "p_risk": risk,
        "p_mode": mode, "p_environment": environment, "p_ttl_seconds": ttl})
    return run_id, status, body.strip()


def finish(key: str, run_id: str, issue: str, state: str, fields: dict) -> tuple[int, str]:
    status, body = rest("POST", "rpc/hermes_finish_run", key=key, body={
        "p_run_id": run_id, "p_issue_id": issue, "p_status": state, "p_fields": fields})
    return status, body.strip()


def management_query(sql: str, token: str) -> object:
    request = urllib.request.Request(
        f"https://api.supabase.com/v1/projects/{PROJECT_REF}/database/query",
        data=json.dumps({"query": sql}).encode("utf-8"),
        headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(request, timeout=90) as response:
        return json.loads(response.read().decode())


def main() -> int:
    global BASE
    access = env_value("SUPABASE_ACCESS_TOKEN")
    service = env_value("SUPABASE_SERVICE_ROLE_KEY")
    BASE = env_value("SUPABASE_URL").rstrip("/")
    if not BASE.startswith("https://"):
        raise SystemExit("SUPABASE_URL invalida")
    print(f"projeto: {BASE} prefixo de fixture: {PREFIX}")

    api_keys = json.loads(urllib.request.urlopen(urllib.request.Request(
        f"https://api.supabase.com/v1/projects/{PROJECT_REF}/api-keys",
        headers={"Authorization": f"Bearer {access}"}), timeout=60).read().decode())
    anon_entry = next((k for k in api_keys if k.get("name") == "anon"), None)
    anon_key = (anon_entry or {}).get("api_key") or ""
    print(f"anon key disponivel={bool(anon_key)} fingerprint={hashlib.sha256(anon_key.encode()).hexdigest()[:16] if anon_key else '-'}")

    event_inventory: dict[str, dict] = {}
    leftovers = {}
    try:
        # ---------------------------------------------------------- cenario A
        issue_a = PREFIX + "-A"
        run_a, status, body = claim(service, issue_a)
        check("A claim service_role -> 200 true", status == 200 and body == "true", f"http={status} {body[:60]}")
        payloads = events_for(run_a, service)
        check("A run.created payload {}", payloads.get("run.created") == {}, json.dumps(payloads.get("run.created")))
        lock_acq = payloads.get("lock.acquired") or {}
        check("A lock.acquired ttl/expires_at", lock_acq.get("ttl_seconds") == 90 and bool(lock_acq.get("expires_at")),
              json.dumps(lock_acq))
        started = payloads.get("run.started") or {}
        check("A run.started risk/mode/env", (started.get("risk"), started.get("execution_mode"),
                                              started.get("environment")) == ("medium", "auto", "staging"),
              json.dumps(started))

        # ---------------------------------------------------------- cenario B
        run_b, status, body = claim(service, issue_a)
        check("B claim concorrente -> 200 false", status == 200 and body == "false", f"http={status} {body[:60]}")
        rejected = select(f"agent_runs?run_id=eq.{urllib.parse.quote(run_b)}&select=run_id", service)
        check("B run rejeitado nao persistido", rejected == [], json.dumps(rejected))
        rejected_events = select(f"agent_events?linear_issue_id=eq.{urllib.parse.quote(issue_a)}"
                                 "&event_type=eq.lock.rejected&select=run_id,payload", service)
        holder_ok = bool(rejected_events) and rejected_events[0]["run_id"] == run_a and \
            rejected_events[0]["payload"].get("reason") == "RunConflict" and \
            rejected_events[0]["payload"].get("attempted_run_id") == run_b
        check("B lock.rejected no detentor com attempted_run_id", holder_ok, json.dumps(rejected_events)[:200])

        # ---------------------------------------------------------- cenario C
        status, body = rest("POST", "rpc/hermes_heartbeat_run", key=service,
                            body={"p_run_id": run_a, "p_issue_id": issue_a, "p_ttl_seconds": 90})
        check("C heartbeat detentor -> 200 true", status == 200 and body == "true", f"http={status} {body[:40]}")
        status, body = finish(service, run_a, issue_a, "completed", {
            "commit_sha": "d" * 40, "pull_request_url": "https://github.com/Sharpista/LoLCoach/pull/31",
            "railway_deployment_id": "smoke-lol61", "tests_status": "passed", "review_status": "approved"})
        check("C finish completed -> 200 true", status == 200 and body == "true", f"http={status} {body[:60]}")
        payloads = events_for(run_a, service)
        completed = payloads.get("run.completed") or {}
        check("C run.completed payload allowlist",
              completed.get("tests_status") == "passed" and completed.get("review_status") == "approved"
              and completed.get("commit_sha") == "d" * 40 and "error" not in completed, json.dumps(completed))
        released = payloads.get("lock.released") or {}
        check("C lock.released finished_status", released.get("finished_status") == "completed", json.dumps(released))
        status, body = finish(service, run_a, issue_a, "failed", {})
        check("C finish tardio -> 200 false", status == 200 and body == "false", f"http={status} {body[:40]}")
        check("C lock liberado", select(f"agent_execution_locks?linear_issue_id=eq."
                                        f"{urllib.parse.quote(issue_a)}&select=run_id", service) == [])

        # ------------------------------------------------- cenario D: failed
        issue_d = PREFIX + "-D"
        run_d, status, body = claim(service, issue_d)
        check("D claim -> true", status == 200 and body == "true", body[:40])
        status, body = finish(service, run_d, issue_d, "failed",
                             {"error_code": "TimeoutError", "kanban_task_id": "t_smoke1", "kanban_outcome": "blocked"})
        check("D finish failed -> true", status == 200 and body == "true", body[:40])
        failed_payload = (events_for(run_d, service).get("run.failed") or {})
        check("D run.failed payload", failed_payload.get("error") == "RuntimeError"
              and failed_payload.get("error_code") == "TimeoutError"
              and failed_payload.get("kanban_task_id") == "t_smoke1", json.dumps(failed_payload))

        # ------------------------------------------------ cenario E: blocked
        issue_e = PREFIX + "-E"
        run_e, status, body = claim(service, issue_e)
        check("E claim -> true", status == 200 and body == "true", body[:40])
        status, body = finish(service, run_e, issue_e, "blocked",
                             {"blocked_reason": "needs_input", "kanban_task_id": "t_smoke2", "tests_status": "passed"})
        check("E finish blocked -> true", status == 200 and body == "true", body[:40])
        blocked_payload = (events_for(run_e, service).get("run.blocked") or {})
        check("E run.blocked payload", blocked_payload.get("error") == "ExecutionBlocked"
              and blocked_payload.get("blocked_reason") == "needs_input"
              and blocked_payload.get("tests_status") == "passed", json.dumps(blocked_payload))

        # ----------------------------------------------- cenario F: canceled
        issue_f = PREFIX + "-F"
        run_f, status, body = claim(service, issue_f)
        check("F claim -> true", status == 200 and body == "true", body[:40])
        status, body = finish(service, run_f, issue_f, "canceled", {"canceled_by": "operator", "reason": "smoke"})
        check("F finish canceled -> true", status == 200 and body == "true", body[:40])
        canceled_payload = (events_for(run_f, service).get("run.canceled") or {})
        check("F run.canceled payload", canceled_payload.get("canceled_by") == "operator", json.dumps(canceled_payload))

        # ------------------------------- cenario G: expiracao + recovery
        issue_g = PREFIX + "-G"
        run_g, status, body = claim(service, issue_g, ttl=60)
        check("G claim -> true", status == 200 and body == "true", body[:40])
        status, body = rest("PATCH", f"agent_execution_locks?linear_issue_id=eq.{urllib.parse.quote(issue_g)}",
                            key=service, body={"expires_at": "2020-01-01T00:00:00Z"},
                            extra_headers={"Prefer": "return=representation"})
        check("G lock forcado ao vencimento", status in (200, 204) and "2020-01-01" in body,
              f"http={status} {body[:80]}")
        status, body = rest("POST", "rpc/hermes_recover_expired_lock", key=service, body={"p_issue_id": issue_g})
        check("G recover -> 200 true", status == 200 and body == "true", f"http={status} {body[:40]}")
        payloads_g = events_for(run_g, service)
        expired = payloads_g.get("lock.expired") or {}
        recovered = payloads_g.get("lock.recovered") or {}
        check("G lock.expired expired_at/last_heartbeat_at",
              bool(expired.get("expired_at")) and bool(expired.get("last_heartbeat_at")), json.dumps(expired))
        check("G lock.recovered previous_run_id", recovered.get("previous_run_id") == run_g, json.dumps(recovered))
        run_g_rows = select(f"agent_runs?run_id=eq.{urllib.parse.quote(run_g)}&select=status,error", service)
        check("G run marcado failed/LockExpired",
              bool(run_g_rows) and run_g_rows[0]["status"] == "failed" and run_g_rows[0]["error"] == "LockExpired",
              json.dumps(run_g_rows))
        status, body = finish(service, run_g, issue_g, "completed", {})
        check("G finish tardio -> false", status == 200 and body == "false", body[:40])

        # ------------------------------ cenario H: entrada invalida sem write
        issue_h = PREFIX + "-H"
        run_h, status, body = claim(service, issue_h)
        check("H claim -> true", status == 200 and body == "true", body[:40])
        status, body = finish(service, run_h, issue_h, "completed", ["invalid"])  # type: ignore[arg-type]
        check("H p_fields nao-objeto rejeitado (4xx)", status >= 400, f"http={status} {body[:120]}")
        run_h_rows = select(f"agent_runs?run_id=eq.{urllib.parse.quote(run_h)}&select=status", service)
        check("H run permanece running", bool(run_h_rows) and run_h_rows[0]["status"] == "running",
              json.dumps(run_h_rows))
        h_events = events_for(run_h, service)
        check("H sem evento de status final", not any(k in h_events for k in
              ("run.completed", "run.failed", "run.blocked", "run.canceled")), json.dumps(sorted(h_events)))

        # --------------------------------------- cenario I: papel anon
        if anon_key:
            status, body = rest("POST", "rpc/hermes_claim_run", key=anon_key, bearer=False, body={
                "p_run_id": new_run_id(), "p_issue_id": PREFIX + "-ANON", "p_agent": "anon",
                "p_risk": "low", "p_mode": "auto", "p_environment": "local", "p_ttl_seconds": 60})
            check("I anon sem Authorization -> 401 42501", status == 401 and "42501" in body,
                  f"http={status} {body[:80]}")
        else:
            check("I anon sem Authorization -> 401 42501", False, "anon key indisponivel")

        # -------------------- leitura final: payloads x contrato Python
        fixture_runs = list(CREATED_RUNS)
        rows = select("agent_events?linear_issue_id=like." + urllib.parse.quote(PREFIX, safe="*-") +
                      "*&select=run_id,event_type,payload&order=id", service)
        event_inventory = {}
        for row in rows:
            event_inventory.setdefault(row["event_type"], row["payload"])
        invalid = []
        for row in rows:
            try:
                validate_event_payload(row["event_type"], row["payload"])
            except Exception as exc:  # noqa: BLE001
                invalid.append([row["event_type"], str(exc)])
        check("J todos os payloads aceitos pelo contrato Python", not invalid, json.dumps(invalid)[:200])
        check("J nenhum payload nulo", all(isinstance(row["payload"], dict) for row in rows),
              f"eventos de fixture={len(rows)}")
        print("== payloads gravados (fixture) ==")
        for kind, payload in sorted(event_inventory.items()):
            print(f"   {kind}: {json.dumps(payload)}")
        print(f"== runs de fixture: {len(fixture_runs)} ==")
    finally:
        # ------------------------------------------------------------- limpeza
        status, body = rest("DELETE", "agent_runs?linear_issue_id=like." +
                            urllib.parse.quote(PREFIX, safe="*-") + "*", key=service,
                            extra_headers={"Prefer": "return=representation"})
        print(f"cleanup agent_runs http={status} removidos={len(json.loads(body or '[]'))}")
        leftovers = {
            "events": select("agent_events?linear_issue_id=like." + urllib.parse.quote(PREFIX, safe="*-") +
                             "*&select=id", service),
            "locks": select("agent_execution_locks?linear_issue_id=like." +
                            urllib.parse.quote(PREFIX, safe="*-") + "*&select=run_id", service),
            "runs": select("agent_runs?linear_issue_id=like." + urllib.parse.quote(PREFIX, safe="*-") +
                           "*&select=run_id", service),
        }
        check("K limpeza sem residuo (events/locks/runs)", all(v == [] for v in leftovers.values()),
              json.dumps({k: len(v) for k, v in leftovers.items()}))

    # ------------------------------------------- contagens globais pos-smoke
    totals = management_query("""
        select count(*) as events_total,
               count(*) filter (where payload is null) as payload_nulls,
               (select count(*) from public.agent_runs) as runs_total,
               (select count(*) from public.agent_execution_locks) as locks_total
        from public.agent_events
    """, access)[0]
    check("L contagens globais iguais ao pre-apply", totals["events_total"] == PRE_APPLY_TOTALS["events_total"]
          and totals["runs_total"] == PRE_APPLY_TOTALS["runs_total"]
          and totals["locks_total"] == PRE_APPLY_TOTALS["locks_total"], json.dumps(totals))
    check("L nenhum payload nulo no projeto", totals["payload_nulls"] == 0, json.dumps(totals))

    failed = [name for name, ok, _ in CHECKS if not ok]
    print(f"== real smoke: {len(CHECKS) - len(failed)}/{len(CHECKS)} PASS ==")
    report = {"prefix": PREFIX, "checks": [{"name": n, "ok": o, "detail": d} for n, o, d in CHECKS],
              "failed": failed, "totals": totals, "payloads_observed": event_inventory,
              "runs_created": CREATED_RUNS, "leftovers": {k: len(v) for k, v in leftovers.items()}}
    with open("/tmp/lol61/real_smoke.json", "w", encoding="utf-8") as handle:
        json.dump(report, handle, indent=2, sort_keys=True, default=str)
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
