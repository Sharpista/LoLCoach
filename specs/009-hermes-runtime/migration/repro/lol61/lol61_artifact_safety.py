#!/usr/bin/env python3
"""LOL-61 — checagem de seguranca do artefato publicado antes do apply.

Lista os statements de nivel superior e garante que nao existe DML de dados
(insert/update/delete/truncate) fora dos corpos das funcoes, nem DDL de tabela.
"""
from __future__ import annotations

import re

text = open("/tmp/lol61/published.sql", encoding="utf-8").read()

# remove corpos de funcao ($$ ... $$)
without_bodies = re.sub(r"\$\$.*?\$\$", "$$BODY$$", text, flags=re.DOTALL)
without_comments = re.sub(r"--[^\n]*", "", without_bodies)
statements = [s.strip() for s in without_comments.split(";") if s.strip()]

print("== top-level statements ==")
for statement in statements:
    print(" *", " ".join(statement.split())[:110])

forbidden = re.compile(r"\b(insert|update|delete|truncate|alter\s+table|drop\s+table|create\s+table)\b", re.IGNORECASE)
violations = [s for s in statements if forbidden.search(s)]
print("== forbidden DML/DDL de tabela fora de funcao ==")
print(violations if violations else "nenhum")

print("== wrap transacional ==")
print("begin primeiro:", statements[0].lower() == "begin")
print("commit ultimo:", statements[-1].lower() == "commit")
print("== md5 do recorte sem corpos ==")
import hashlib

print(hashlib.sha256(without_comments.encode()).hexdigest())
