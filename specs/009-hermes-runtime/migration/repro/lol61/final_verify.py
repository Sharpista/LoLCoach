#!/usr/bin/env python3
"""LOL-61 — releitura final independente:
  1. cada funcao hermes_* viva == tokens do artefato publicado (bf752895...)
  2. dados funcionais pre-existentes intactos (12 eventos / 2 runs por tipo e timestamps)
"""
from __future__ import annotations

import json
import re
import sys

COMMENT = re.compile(r"--[^\n]*")
WS = re.compile(r"\s+")


def canon(text: str) -> list[str]:
    text = text.replace("$function$", "$BODY$").replace("$$", "$BODY$")
    text = COMMENT.sub("", text).lower()
    text = re.sub(r"security\s+invoker", "", text)
    text = re.sub(r"set\s+search_path\s*(?:to|=)\s*''", "search_path_empty", text)
    text = re.sub(r"search_path\s*=\s*''", "search_path_empty", text)
    text = text.replace("$body$;", "$body$")
    text = re.sub(r"\s*([(),])\s*", r"\1", text)
    return WS.sub(" ", text).split()


def split_functions(text: str) -> dict[str, str]:
    pattern = re.compile(r"create\s+or\s+replace\s+function\s+public\.(\w+)\s*\(.*?\$\$;",
                         re.IGNORECASE | re.DOTALL)
    return {m.group(1): m.group(0) for m in pattern.finditer(text)}


published = open("/tmp/lol61/published.sql", encoding="utf-8").read()
pub_fns = split_functions(published)
live = {row["name"]: row["def"] for row in
        json.load(open("/tmp/lol61/supabase_read_final.json"))["rpc_defs"]}

results = {}
for name, statement in pub_fns.items():
    results[name] = canon(statement) == canon(live.get(name, ""))
print("== funcoes vivas == artefato publicado ==")
for name, ok in sorted(results.items()):
    print(f"{'PASS' if ok else 'FAIL'} {name}")

pre = json.load(open("/tmp/lol61/supabase_read2_pre.json"))
post = json.load(open("/tmp/lol61/supabase_read2.json"))
pre1 = json.load(open("/tmp/lol61/supabase_read_pre.json"))
post1 = json.load(open("/tmp/lol61/supabase_read_final.json"))
same_events = pre["event_payload_stats"] == post["event_payload_stats"]
same_runs = pre["runs_state"] == post["runs_state"]
same_columns = pre1["columns"] == post1["columns"]
same_constraints = pre1["constraints"] == post1["constraints"]
same_indexes = pre1["indexes"] == post1["indexes"]
same_grants = pre1["table_grants"] == post1["table_grants"]
same_triggers = pre["triggers"] == post["triggers"]
same_policies = pre["policies"] == post["policies"]
same_totals = pre1["event_payload_totals"] == post1["event_payload_totals"]
print("== dados funcionais pre-existentes intactos ==")
print("PASS eventos por tipo/timestamps inalterados" if same_events else "FAIL eventos alterados")
print("PASS runs inalterados" if same_runs else "FAIL runs alterados")
print("PASS contagens (12 eventos / 2 runs / 0 locks) inalteradas" if same_totals else "FAIL contagens alteradas")
print(f"PASS schema e grants inalterados (colunas={same_columns} constraints={same_constraints} "
      f"indices={same_indexes} grants={same_grants} triggers={same_triggers} policies={same_policies})"
      if (same_columns and same_constraints and same_indexes and same_grants and same_triggers and same_policies)
      else f"FAIL schema alterado: colunas={same_columns} constraints={same_constraints} "
           f"indices={same_indexes} grants={same_grants} triggers={same_triggers} policies={same_policies}")
summary = {"functions_match_published": results, "events_unchanged": same_events, "runs_unchanged": same_runs,
           "totals_unchanged": same_totals,
           "schema_unchanged": all([same_columns, same_constraints, same_indexes, same_grants,
                                    same_triggers, same_policies])}
with open("/tmp/lol61/final_verify.json", "w", encoding="utf-8") as handle:
    json.dump(summary, handle, indent=2, sort_keys=True)
print(json.dumps(summary, sort_keys=True))
sys.exit(0 if all(results.values()) and same_events and same_runs and same_totals else 1)
