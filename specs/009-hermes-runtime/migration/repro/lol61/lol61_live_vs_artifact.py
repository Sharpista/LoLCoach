#!/usr/bin/env python3
"""LOL-61 — igualdade semantica aplicado-vs-banco real, com normalizacao dos
artefatos de renderizacao do pg_get_functiondef (SECURITY INVOKER omitido por ser
o default, search_path reescrito, terminador de dollar-quote, comentarios, espacos).
"""
from __future__ import annotations

import difflib
import json
import re

COMMENT = re.compile(r"--[^\n]*")
WS = re.compile(r"\s+")


def canon(text: str) -> list[str]:
    text = text.replace("$function$", "$BODY$").replace("$$", "$BODY$")
    text = COMMENT.sub("", text)
    text = text.lower()
    # artefatos de renderizacao do pg_get_functiondef
    text = re.sub(r"security\s+invoker", "", text)
    text = re.sub(r"security\s+definer", "securitydefiner", text)
    text = re.sub(r"set\s+search_path\s*(?:to|=)\s*''", "search_path_empty", text)
    text = re.sub(r"search_path\s*=\s*''", "search_path_empty", text)
    text = text.replace("$body$;", "$body$")
    text = re.sub(r"\s*([(),])\s*", r"\1", text)
    return WS.sub(" ", text).split()


def split_functions(text: str) -> dict[str, str]:
    pattern = re.compile(
        r"create\s+or\s+replace\s+function\s+public\.(\w+)\s*\(.*?\$\$;",
        re.IGNORECASE | re.DOTALL,
    )
    return {m.group(1): m.group(0) for m in pattern.finditer(text)}


applied = open("/tmp/lol61/applied.sql", encoding="utf-8").read()
raw = json.load(open("/tmp/lol61/supabase_read.json"))
live = {row["name"]: row["def"] for row in raw["rpc_defs"]}
app_fns = split_functions(applied)

summary = {}
for name, statement in app_fns.items():
    a, b = canon(statement), canon(live.get(name, ""))
    same = a == b
    summary[name] = {"identical": same, "artifact_tokens": len(a), "live_tokens": len(b)}
    print(f"===== {name}: identical={same}")
    if not same:
        for line in list(difflib.unified_diff(a, b, "artifact", "live", lineterm="", n=2))[:30]:
            print("   ", line)
print(json.dumps(summary, indent=2, sort_keys=True))
with open("/tmp/lol61/live_vs_artifact.json", "w", encoding="utf-8") as handle:
    json.dump(summary, handle, indent=2, sort_keys=True)
