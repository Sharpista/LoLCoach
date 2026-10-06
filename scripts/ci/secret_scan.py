#!/usr/bin/env python3
"""Secret scan estático do repositório (LOL-121).

Objetivo: falhar o CI quando uma credencial plausível entrar no índice do Git.
Princípios (alinhados à auditoria LOL-119, R4/R9):

* **Somente stdlib** — sem rede, sem instalar scanner de terceiro.
* **Não lê nada fora do repositório** — em particular nunca abre
  `~/.hermes/shared/.env` nem qualquer arquivo do host; o scanner é read-only
  sobre o índice do próprio repo (`git ls-files`).
* **Nunca imprime valores** — o relatório mostra arquivo, linha, rótulo do
  padrão e o *comprimento* do trecho casado (mascarado), nunca o conteúdo.
* **Escopo estreito** — apenas padrões de credencial com formato reconhecível;
  não tenta ser um gitleaks/trufflehog (histórico completo fica fora do escopo;
  ver limitações no doc do card).

Uso:
    python3 scripts/ci/secret_scan.py [--root DIR]

Sai com 0 quando não há achado, 1 quando há achado e 2 quando o próprio
scanner não conseguiu rodar (ex.: repo sem git).
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

# --- Padrões -----------------------------------------------------------------
# Literais sensíveis são montados por concatenação de propósito: assim o texto
# deste arquivo não casa com os próprios padrões quando o scanner varre o repo.
JWT = re.compile(r"eyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}")
SUPABASE_SECRET = re.compile("sb_" "secret_" + r"[A-Za-z0-9_\-]{16,}")
SUPABASE_ASSIGN = re.compile(
    r"(SUPABASE_SERVICE_ROLE_KEY|SUPABASE_ACCESS_TOKEN|SUPABASE_SECRET_KEY)"
    r"\s*[=:]\s*[\"']?\S{16,}"
)
API_KEY_ASSIGN = re.compile(
    r"(LINEAR_API_KEY|RAILWAY_TOKEN|GEMINI_API_KEY|GOOGLE_API_KEY|OPENAI_API_KEY"
    r"|ANTHROPIC_API_KEY|DATABASE_URL)\s*[=:]\s*[\"']?\S{16,}"
)
CONN_PASSWORD = re.compile(r"(?i);[ \t]*Password[ \t]*=[ \t]*[^;'\"\s]{12,}")
PRIVATE_KEY = re.compile(r"-{5}BEGIN [A-Z ]*PRIVATE KEY-{5}")
AWS_KEY = re.compile(r"\b(?:AKIA|ASIA)[0-9A-Z]{16}\b")
GH_TOKEN = re.compile(r"\b(?:ghp|gho|ghs|ghr|github_pat)_[A-Za-z0-9_]{20,}\b")
BEARER_JWT = re.compile(r"(?i)\bBearer\s+eyJ[A-Za-z0-9_\-]{10,}")

PATTERNS = [
    ("jwt", JWT),
    ("supabase-secret-key", SUPABASE_SECRET),
    ("supabase-key-assign", SUPABASE_ASSIGN),
    ("api-key-assign", API_KEY_ASSIGN),
    ("connection-string-password", CONN_PASSWORD),
    ("private-key", PRIVATE_KEY),
    ("aws-access-key", AWS_KEY),
    ("github-token", GH_TOKEN),
    ("bearer-jwt", BEARER_JWT),
]

# Valores sintéticos conhecidos do repositório (ex.: migrate.yml usa uma
# connection string explicitamente não secreta; os testes .NET usam placeholders
# autoexplicativos). Nunca ganha allowlist genérica: apenas estes literais.
ALLOWED_VALUES = {
    "nao-e-secret",
    "secret-do-not-log",
    "secret-not-leaked",
    "changeme",
    "not-a-real",
}

# Hosts de teste/loopback: uma connection string de teste não é vazamento.
LOOPBACK_HOSTS = ("127.0.0.1", "localhost", "[::1]", "0.0.0.0")

# Marcadores de valor já redigido/parametrizado.
PLACEHOLDER_CHARS = "<>{}$*"

SKIP_SUFFIXES = (".png", ".jpg", ".jpeg", ".gif", ".ico", ".woff", ".woff2",
                 ".ttf", ".eot", ".pdf", ".zip", ".gz", ".dll", ".so", ".pdb")

SELF = Path(__file__).resolve()


def tracked_files(root: Path) -> list[str]:
    """Arquivos do índice do Git (respeita .gitignore; ignora worktrees alheios)."""
    out = subprocess.run(
        ["git", "-C", str(root), "ls-files", "-z"],
        capture_output=True, check=False,
    )
    if out.returncode != 0:
        raise SystemExit("erro: não foi possível listar arquivos do Git (git ls-files)")
    return [p for p in out.stdout.decode("utf-8", "replace").split("\0") if p]


def is_binary(path: Path) -> bool:
    try:
        with path.open("rb") as handle:
            return b"\0" in handle.read(4096)
    except OSError:
        return True


def line_of(text: str, position: int) -> str:
    start = text.rfind("\n", 0, position) + 1
    end = text.find("\n", position)
    return text[start:end if end != -1 else len(text)]


def is_benign(match_text: str, line: str) -> bool:
    """Descarta placeholders, hosts de loopback e valores já redigidos."""
    if any(allowed in match_text for allowed in ALLOWED_VALUES):
        return True
    if any(host in line for host in LOOPBACK_HOSTS):
        return True
    return any(char in match_text for char in PLACEHOLDER_CHARS)


def scan_text(text: str) -> list[tuple[str, int, int]]:
    """Retorna (rótulo, linha, comprimento do trecho) por achado."""
    hits: list[tuple[str, int, int]] = []
    for label, pattern in PATTERNS:
        for match in pattern.finditer(text):
            matched = match.group(0)
            if is_benign(matched, line_of(text, match.start())):
                continue
            line = text.count("\n", 0, match.start()) + 1
            hits.append((label, line, len(matched)))
    return hits


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Secret scan estático (stdlib)")
    parser.add_argument("--root", default=None, help="raiz do repositório")
    args = parser.parse_args(argv)

    root = Path(args.root).resolve() if args.root else SELF.parents[2]
    if not root.exists():
        print(f"erro: raiz inexistente: {root}")
        return 2

    findings: list[str] = []
    scanned = 0
    env_tracked: list[str] = []

    for rel in tracked_files(root):
        name = Path(rel).name
        if name == SELF.name and rel.endswith("ci/secret_scan.py"):
            continue
        if name == ".env" or (name.startswith(".env.") and name != ".env.example"):
            env_tracked.append(rel)
        if rel.endswith(SKIP_SUFFIXES):
            continue
        full = root / rel
        if not full.is_file() or is_binary(full):
            continue
        try:
            text = full.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        scanned += 1
        for label, line, length in scan_text(text):
            findings.append(f"{rel}:{line} [{label}] trecho mascarado ({length} chars)")

    for rel in env_tracked:
        findings.append(f"{rel} [arquivo .env rastreado] (deve sair do índice)")

    print(f"secret scan: {scanned} arquivo(s) do índice varridos, "
          f"{len(PATTERNS)} padrão(ões)")
    if findings:
        print("RESULTADO: FALHOU — credencial candidata encontrada:")
        for item in findings:
            print("  -", item)
        return 1
    print("RESULTADO: limpo — nenhuma credencial candidata; nenhum .env rastreado")
    return 0


if __name__ == "__main__":
    sys.exit(main())
