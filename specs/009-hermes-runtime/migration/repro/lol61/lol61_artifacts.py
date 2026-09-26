#!/usr/bin/env python3
"""LOL-61 — comparacao de proveniencia: publicado vs candidato vs aplicado vs banco real.

Saidas:
  - tokens sem comentarios/espacos (comparacao semantica)
  - por funcao: aplicado vs pg_get_functiondef do banco real
"""
from __future__ import annotations

import json
import re
import sys

COMMENT = re.compile(r"--[^\n]*")
WS = re.compile(r"\s+")


def strip_comments(text: str) -> str:
    return COMMENT.sub("", text)


def tokens(text: str) -> str:
    return WS.sub("", strip_comments(text)).lower()


def spaced(text: str) -> str:
    return WS.sub(" ", strip_comments(text)).lower()


def load(path: str) -> str:
    with open(path, "r", encoding="utf-8") as handle:
        return handle.read()


def split_functions(text: str) -> dict[str, str]:
    """Extrai cada bloco `create or replace function public.<name>(...) ... $$;`."""
    pattern = re.compile(
        r"create\s+or\s+replace\s+function\s+public\.(\w+)\s*\(.*?\$\$;",
        re.IGNORECASE | re.DOTALL,
    )
    return {match.group(1): match.group(0) for match in pattern.finditer(text)}


def normalize_live(def_text: str) -> str:
    text = def_text.replace("$function$", "$BODY$").replace("$$", "$BODY$")
    return spaced(text)


def normalize_file_statement(statement: str) -> str:
    text = statement.replace("$$", "$BODY$")
    return spaced(text)


def main() -> int:
    applied = load("/tmp/lol61/applied.sql")
    published = load("/tmp/lol61/published.sql")
    candidate = load("/tmp/lol61/candidate.sql")
    runtime_main = load("/tmp/lol61/runtime_main.sql")
    raw = json.load(open("/tmp/lol61/supabase_read.json"))
    live = {row["name"]: row["def"] for row in raw["rpc_defs"]}

    result: dict[str, object] = {}
    result["tokens_equal_published_vs_candidate"] = tokens(published) == tokens(candidate)
    result["tokens_equal_published_vs_runtime_main"] = tokens(published) == tokens(runtime_main)
    result["tokens_equal_applied_vs_published"] = tokens(applied) == tokens(published)
    result["spaced_equal_published_vs_candidate"] = spaced(published) == spaced(candidate)

    pub_fns = split_functions(published)
    cand_fns = split_functions(candidate)
    app_fns = split_functions(applied)
    result["functions_published"] = sorted(pub_fns)
    result["functions_candidate"] = sorted(cand_fns)
    result["functions_applied"] = sorted(app_fns)

    per_fn = {}
    for name, statement in app_fns.items():
        entry = {"applied_vs_live": normalize_file_statement(statement) == normalize_live(live.get(name, ""))}
        if name in pub_fns:
            entry["candidate_vs_published"] = tokens(cand_fns[name]) == tokens(pub_fns[name])
            entry["applied_vs_published_tokens_equal"] = tokens(statement) == tokens(pub_fns[name])
        per_fn[name] = entry
    result["per_function"] = per_fn

    # marcadores de payload da revisao pendente
    markers = ["jsonb_strip_nulls", "ttl_seconds", "expires_at", "finished_status",
               "previous_run_id", "last_heartbeat_at", "error_code", "p_fields must be a JSON object"]
    result["markers_in_live"] = {m: (m in live.get("hermes_finish_run", "") + live.get("hermes_claim_run", "")
                                     + live.get("hermes_recover_expired_lock", "")) for m in markers}
    result["markers_in_published"] = {m: (m in published) for m in markers}

    with open("/tmp/lol61/artifact_compare.json", "w", encoding="utf-8") as handle:
        json.dump(result, handle, indent=2, sort_keys=True)
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
