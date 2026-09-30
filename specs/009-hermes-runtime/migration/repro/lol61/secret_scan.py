#!/usr/bin/env python3
"""Varredura de credenciais nos artefatos de evidencia/scratch do LOL-61.

Nao imprime valores: apenas nome do arquivo, padrao encontrado e contagem.
Sai com 1 se encontrar algo do projeto (JWT/segredo) nos artefatos.
"""
from __future__ import annotations

import hashlib
import os
import re

TARGETS = ["/home/alexandre/LoLSaas/specs/009-hermes-runtime/evidence/lol-61-payload-apply.md"]
SCAN_DIR = "/tmp/lol61"
PATTERNS = {
    "jwt": re.compile(r"eyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}"),
    "sb_secret": re.compile(r"sb_secret_[A-Za-z0-9_\-]{10,}"),
    "service_role_key_env": re.compile(r"SUPABASE_SERVICE_ROLE_KEY\s*=\s*\S{8,}"),
    "access_token_env": re.compile(r"SUPABASE_ACCESS_TOKEN\s*=\s*\S{8,}"),
    "bearer_literal": re.compile(r"Bearer\s+eyJ"),
}

found = False
paths = list(TARGETS)
for name in sorted(os.listdir(SCAN_DIR)):
    full = os.path.join(SCAN_DIR, name)
    if os.path.isfile(full) and not name.endswith((".pem", ".key")):
        paths.append(full)
for path in paths:
    try:
        with open(path, "r", encoding="utf-8", errors="ignore") as handle:
            text = handle.read()
    except OSError:
        continue
    hits = {label: len(pattern.findall(text)) for label, pattern in PATTERNS.items()}
    hits = {k: v for k, v in hits.items() if v}
    if hits:
        found = True
        print(f"ENCONTRADO {path}: {hits}")
print("varredura de credenciais:", "FALHOU (credencial em artefato)" if found else "limpa (nenhum segredo do projeto)")

# fingerprint das credenciais reais para conferir contra o .env sem imprimir o valor.
env = os.path.expanduser("~/.hermes/shared/.env")
values = {}
with open(env, "r", encoding="utf-8") as handle:
    for line in handle:
        if "=" in line and not line.strip().startswith("#"):
            key, value = line.split("=", 1)
            values[key.strip()] = value.strip()
for key in ("SUPABASE_SERVICE_ROLE_KEY", "SUPABASE_ACCESS_TOKEN", "LINEAR_API_KEY"):
    value = values.get(key, "")
    print(f"{key}: presente={bool(value)} fingerprint={hashlib.sha256(value.encode()).hexdigest()[:16] if value else '-'}")
print("anon key fingerprint registrado na evidencia: 7650ca244458fb96")
