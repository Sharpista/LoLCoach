#!/usr/bin/env python3
"""Gera JWT HS256 local (role=service_role) para o PostgREST de fixture e o segredo.

O segredo NAO fica no repositorio: defina LOL61_FIXTURE_JWT_SECRET (>= 32 chars) no
ambiente ao reproduzir. Ele serve apenas ao PostgREST local descartavel da fixture.
"""
from __future__ import annotations

import base64
import hashlib
import hmac
import json
import os
import time

SECRET = os.environ["LOL61_FIXTURE_JWT_SECRET"]
OUT_DIR = "/tmp/lol61"


def b64(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode()


def token(role: str) -> str:
    header = {"alg": "HS256", "typ": "JWT"}
    now = int(time.time())
    payload = {"role": role, "iss": "lol61-fixture", "iat": now, "exp": now + 3600}
    signing_input = b64(json.dumps(header, separators=(",", ":")).encode()) + "." + \
        b64(json.dumps(payload, separators=(",", ":")).encode())
    signature = hmac.new(SECRET.encode(), signing_input.encode(), hashlib.sha256).digest()
    return signing_input + "." + b64(signature)


def main() -> int:
    os.makedirs(OUT_DIR, exist_ok=True)
    for role in ("service_role", "authenticated"):
        path = os.path.join(OUT_DIR, f"jwt_{role}.txt")
        with open(path, "w", encoding="utf-8") as handle:
            handle.write(token(role))
        os.chmod(path, 0o600)
        print(f"{role}: jwt escrito em {path} (len={len(token(role))})")
    with open(os.path.join(OUT_DIR, "jwt_secret.txt"), "w", encoding="utf-8") as handle:
        handle.write(SECRET)
    os.chmod(os.path.join(OUT_DIR, "jwt_secret.txt"), 0o600)
    print("segredo: /tmp/lol61/jwt_secret.txt")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
