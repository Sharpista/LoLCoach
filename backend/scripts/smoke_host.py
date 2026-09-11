"""Smoke de transporte do host base e do endpoint player-search (validação sem dependências externas)."""
import json
import os
from pathlib import Path
import re
import subprocess
import time
import urllib.error
import urllib.request

backend = Path(__file__).resolve().parents[1]
artifacts = backend / "artifacts"
artifacts.mkdir(exist_ok=True)
dll = backend / "src/LoLCoach.Api/bin/Debug/net10.0/LoLCoach.Api.dll"
if not dll.exists():
    raise SystemExit("Execute dotnet build backend/LoLCoach.slnx primeiro.")
env = os.environ.copy()
env["ASPNETCORE_ENVIRONMENT"] = "Production"
env["ASPNETCORE_URLS"] = "http://127.0.0.1:0"
# Smoke cannot accidentally depend on a supplied production database or Riot key.
env.pop("ConnectionStrings__LoLCoach", None)
env.pop("Riot__ApiKey", None)
# A placeholder connection string lets the DbContext resolve without ever connecting:
# EF Core is lazy and validation fails before any query is issued.
env["ConnectionStrings__LoLCoach"] = "Host=127.0.0.1;Port=1;Database=lolcoach_smoke"
log_path = artifacts / "smoke-host.log"
with log_path.open("w") as log:
    process = subprocess.Popen(["dotnet", str(dll)], cwd=backend / "src/LoLCoach.Api",
                               env=env, stdout=log, stderr=subprocess.STDOUT)
    try:
        url = None
        for _ in range(100):
            if process.poll() is not None:
                raise RuntimeError("Host encerrou antes do smoke; veja artifacts/smoke-host.log")
            match = re.search(r"Now listening on: (http://127[.]0[.]0[.]1:[0-9]+)", log_path.read_text())
            if match:
                url = match.group(1)
                break
            time.sleep(0.1)
        if url is None:
            raise RuntimeError("Host não iniciou dentro de 10 segundos")
        observations = []
        # GET / has no route -> 404. POST search with empty body fails validation before
        # any Riot/DB access -> 400, which also proves the endpoint is wired without a key.
        for path, method, payload, expected in [
            ("/", "GET", None, 404),
            ("/api/players/search", "POST", b'{}', 400),
        ]:
            request = urllib.request.Request(url + path, data=payload, method=method,
                                             headers={"Content-Type": "application/json"})
            try:
                response = urllib.request.urlopen(request, timeout=5)
                status = response.status
            except urllib.error.HTTPError as response:
                status = response.code
            assert status == expected, f"{method} {path}: {status}, esperado {expected}"
            observations.append({"method": method, "path": path, "status": status})
        result = {"scope": "host base + player-search validation (no Riot key, no DB)",
                  "pid": process.pid, "url": url, "command": ["dotnet", str(dll)],
                  "observations": observations}
    finally:
        process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=5)
    result["processStopped"] = process.poll() is not None
    (artifacts / "smoke-host.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, indent=2))
