#!/usr/bin/env python3
"""Validação estática dos workflows do GitHub Actions (LOL-121).

Faz o papel de "actionlint mínimo" com um parser YAML seguro (PyYAML
`safe_load`) e um conjunto explícito de políticas do repositório. Não usa
rede, não lê secrets, não executa workflow algum.

Checagens estruturais (por arquivo em `.github/workflows/`):

1. o arquivo precisa ser um mapeamento YAML válido;
2. precisa declarar gatilho (`on:` — atenção: o YAML 1.1 lê `on` como `True`);
3. precisa ter `jobs` não vazio; cada job precisa de `runs-on` ou `uses`;
4. cada `step` precisa de `run` ou `uses`; `uses` de action externa precisa de
   `@` (referência pinada) e não pode ser `@main`/`@master`/`@HEAD`.

Políticas do repositório (fecham as lacunas R2/R5 da auditoria LOL-119):

5. `ci-guards.yml` precisa existir **e não pode** ter filtro `paths` — senão um
   PR que só mexe em `.github/`, `specs/` ou `docs/` volta a não produzir check;
6. nenhum workflow que possa publicar (`deploy.yml`, `migrate.yml`) pode ter
   gatilho `push` — deploy/migração são manuais;
7. workflow com gatilho `push` e sem filtro `paths` gera aviso, exceto
   `ci-guards.yml` (que é justamente o guard sempre-ligado).

Uso:
    pip install pyyaml
    python3 scripts/ci/validate_workflows.py [--dir .github/workflows]

Sai com 0 sem erro (avisos não falham), 1 com erro e 2 quando falta dependência.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

try:
    import yaml  # PyYAML, parser seguro (safe_load)
except ImportError:  # pragma: no cover - caminho de ambiente incompleto
    print("erro: PyYAML ausente (pip install pyyaml) — sem parser seguro, abortando")
    sys.exit(2)

# O YAML 1.1 parseia a chave `on` como booleano True.
TRIGGER_KEYS = (True, "on", "true")

ALWAYS_ON_WORKFLOW = "ci-guards.yml"
NO_PUSH_WORKFLOWS = {"deploy.yml", "migrate.yml"}
MUTABLE_REFS = {"main", "master", "HEAD"}

errors: list[str] = []
warnings: list[str] = []


def err(rel: str, msg: str) -> None:
    errors.append(f"{rel}: {msg}")


def warn(rel: str, msg: str) -> None:
    warnings.append(f"{rel}: {msg}")


def triggers(doc: dict) -> object:
    for key in TRIGGER_KEYS:
        if key in doc:
            return doc[key]
    return None


def push_body(trigger: object) -> object:
    """Retorna o corpo do gatilho `push`, ou None quando não há push."""
    if isinstance(trigger, dict):
        return trigger.get("push")
    if isinstance(trigger, list):
        return {} if "push" in trigger else None
    if isinstance(trigger, str):
        return {} if trigger == "push" else None
    return None


def has_paths(body: object) -> bool:
    return isinstance(body, dict) and "paths" in body


def any_path_filter(trigger: object) -> bool:
    """True se QUALQUER gatilho declarado tem filtro `paths`."""
    if isinstance(trigger, dict):
        return any(has_paths(body) for body in trigger.values())
    return False


def check_steps(rel: str, job_name: str, job: dict) -> None:
    steps = job.get("steps")
    if steps is None:
        return
    if not isinstance(steps, list) or not steps:
        err(rel, f"job '{job_name}': 'steps' precisa ser uma lista não vazia")
        return
    for index, step in enumerate(steps, start=1):
        where = f"job '{job_name}' step {index}"
        if not isinstance(step, dict):
            err(rel, f"{where}: precisa ser um mapeamento")
            continue
        if "run" not in step and "uses" not in step:
            err(rel, f"{where}: precisa de 'run' ou 'uses'")
        uses = step.get("uses")
        if isinstance(uses, str) and not uses.startswith("./"):
            if "@" not in uses:
                err(rel, f"{where}: action '{uses}' sem referência pinada (@ref)")
            else:
                ref = uses.rsplit("@", 1)[1]
                if ref in MUTABLE_REFS:
                    warn(rel, f"{where}: action '{uses}' aponta para ref móvel '@{ref}'")
        run = step.get("run")
        if run is not None and not isinstance(run, str):
            err(rel, f"{where}: 'run' precisa ser string")


def check_workflow(path: Path) -> None:
    rel = path.name
    raw = path.read_text(encoding="utf-8")
    try:
        doc = yaml.safe_load(raw)
    except yaml.YAMLError as exc:
        err(rel, f"YAML inválido: {str(exc).splitlines()[0]}")
        return

    if not isinstance(doc, dict):
        err(rel, "conteúdo de topo precisa ser um mapeamento YAML")
        return

    trigger = triggers(doc)
    if trigger is None:
        err(rel, "gatilho 'on:' ausente")

    jobs = doc.get("jobs")
    if not isinstance(jobs, dict) or not jobs:
        err(rel, "'jobs' precisa ser um mapeamento não vazio")
    else:
        for job_name, job in jobs.items():
            if not isinstance(job, dict):
                err(rel, f"job '{job_name}': precisa ser um mapeamento")
                continue
            if "runs-on" not in job and "uses" not in job:
                err(rel, f"job '{job_name}': precisa de 'runs-on' ou 'uses'")
            check_steps(rel, str(job_name), job)

    if rel == ALWAYS_ON_WORKFLOW and any_path_filter(trigger):
        err(rel, f"{ALWAYS_ON_WORKFLOW} não pode ter filtro 'paths' em nenhum "
                 "gatilho (reabriria a lacuna de PR sem nenhum check)")

    if rel in NO_PUSH_WORKFLOWS and push_body(trigger) is not None:
        err(rel, "workflow de deploy/migração não pode ser disparado por 'push'")

    body = push_body(trigger)
    if body is not None and not has_paths(body) and rel != ALWAYS_ON_WORKFLOW:
        warn(rel, "gatilho 'push' sem filtro 'paths' "
                  "(roda em todo push de main/develop)")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validação estática de workflows")
    parser.add_argument("--dir", default=None, help="diretório dos workflows")
    args = parser.parse_args(argv)

    root = Path(__file__).resolve().parents[2]
    wf_dir = Path(args.dir).resolve() if args.dir else root / ".github" / "workflows"
    if not wf_dir.is_dir():
        print(f"erro: diretório inexistente: {wf_dir}")
        return 2

    # Estado limpo: o módulo pode ser reutilizado em processo longo (testes).
    errors.clear()
    warnings.clear()

    files = sorted(list(wf_dir.glob("*.yml")) + list(wf_dir.glob("*.yaml")))
    if not files:
        print(f"erro: nenhum workflow em {wf_dir}")
        return 1

    for path in files:
        check_workflow(path)

    if not (wf_dir / ALWAYS_ON_WORKFLOW).is_file():
        err(f"{wf_dir.name}/", f"{ALWAYS_ON_WORKFLOW} ausente (guard sempre-ligado)")

    print(f"workflows validados: {len(files)}")
    for item in warnings:
        print("AVISO:", item)
    for item in errors:
        print("ERRO:", item)
    if errors:
        print(f"RESULTADO: FALHOU — {len(errors)} erro(s), {len(warnings)} aviso(s)")
        return 1
    print(f"RESULTADO: OK — 0 erro(s), {len(warnings)} aviso(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
