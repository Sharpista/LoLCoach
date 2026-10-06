# LOL-121 — CI: guards, secret scan e revisão de path filters

- Card Hermes: `t_bfa23f40` (perfil `github-profile`) · Linear: [LOL-121](https://linear.app/lolcoach/issue/LOL-121/ci-lolcoach-guards-secret-scan-e-revisao-de-path-filters)
- Base do PR: `develop` · Branch: `chore/LOL-121-ci-guards`
- Insumo: auditoria [LOL-119](https://linear.app/lolcoach/issue/LOL-119/auditoria-definir-ci-minimo-para-runtimepoller-e-docs) (card `t_5a1658fb`), riscos **R2**, **R4**, **R5** e **R8**.

## Escopo e não-escopo

Entregue nesta branch:

1. **`.github/workflows/ci-guards.yml`** — guard estático sempre-ligado (shell,
   YAML de workflow, actionlint, secret scan).
2. **`scripts/ci/secret_scan.py`** — secret scan estático em Python puro
   (stdlib), sem rede e sem ler nada fora do repositório.
3. **`scripts/ci/validate_workflows.py`** — parser YAML seguro + políticas do
   repositório sobre `.github/workflows/`.
4. Esta revisão de path filters e de `production.yml` (documento, não execução).

Fora de escopo (confirmado): não altera secrets, branch protection, required
checks, deploy, tag ou release; não executa `deploy.yml`, `migrate.yml` nem
`production.yml`; não publica imagem; não roda migração.

## 1. Revisão de path filters

Estado antes desta branch (workflows e gatilhos reais):

| Workflow | Gatilho | Filtro `paths` | O que roda |
| --- | --- | --- | --- |
| `backend-ci.yml` | `push`, `pull_request` | `backend/**`, o próprio YAML | build + test + format .NET |
| `frontend-ci.yml` | `push`, `pull_request` | `frontend/**`, o próprio YAML | `npm ci` + build + testes |
| `python-ci.yml` | `push`, `pull_request` | `**/*.py`, o próprio YAML | `python -m compileall -q .` |
| `production.yml` | `push` em `main` | **nenhum** | build + test .NET (Release) |
| `deploy.yml` | `workflow_dispatch` + `release: published` | — | build/push GHCR + deploy Railway |
| `migrate.yml` | `workflow_dispatch` | — | script SQL + `database update` |
| **`ci-guards.yml`** (novo) | `push` (`main`, `develop`), `pull_request`, `workflow_dispatch` | **nenhum (por projeto)** | shell + YAML/políticas + actionlint + secret scan |

**Lacuna fechada (R2).** Como os três workflows de conteúdo têm filtro `paths`, um
PR que mexesse apenas em `.github/**`, `specs/**` ou `docs/**` produzia **zero
checks** — e ausência de CI não é CI verde. `ci-guards.yml` é deliberadamente
**sem filtro** (roda em todo PR, para qualquer base, e em push de `main`/`develop`);
por isso ele também é quem valida os próprios workflows. A política "ci-guards não
pode ganhar `paths`" virou **checagem executável** em
`scripts/ci/validate_workflows.py` (teste de regressão), não só um comentário.

**`python-ci.yml` — mantido como está.** Não há suíte de testes Python no repo
(só `backend/scripts/smoke_host.py` e os repros de `specs/**`, que têm paths
absolutos do host). `compileall -q .` já cobre `scripts/ci/*.py`. Adicionar passos
sem suíte seria ruído; a checagem de shell passou para o guard dedicado.

## 2. Revisão do `production.yml` (duplicação e risco)

Achados (leitura estática; **nada foi alterado neste arquivo**):

- **Duplicação**: `on: push: branches: [main]` sem filtro roda build + test .NET
  Release sobre os mesmos arquivos que o `backend-ci` já builda/testa em `push`
  de qualquer branch (inclusive `main`). O passo é redundante.
- **Superfície de secret**: o job declara `environment: production` e injeta
  `SUPABASE_SECRET_KEY`, `DATABASE_URL` e `CONNECTIONSTRINGS__LOLCOACH` em **todos**
  os passos, incluindo build e test — que não precisam de credencial (os testes
  usam banco efêmero/Testcontainers, como o próprio `backend-ci.yml` documenta).
  Ou seja, todo push em `main` expõe secrets de produção a passos de build/test.
- **Sem passo de publicação**: o workflow não faz deploy nem migrate — colidir com
  `deploy.yml`/`migrate.yml` não é o problema; o problema é o custo duplicado e a
  superfície de secret desnecessária.
- Não é referenciado por nenhum outro arquivo do repo (`grep` por `production.yml`
  só encontra a listagem de workflows).

**Recomendação (não aplicada aqui).** Uma destas, em card de `devops` com
autorização explícita, porque mexe em superfície de *environment*/*secrets*:

1. remover `production.yml` (o `backend-ci` cobre build+test em `main`); ou
2. mantê-lo como `workflow_dispatch` (runbook/verificação manual em produção), sem
   gatilho `push` e sem injetar secrets no job de build/test.

Nesta branch ele permanece inalterado de propósito: o card proíbe alterar deploy
e a alteração real toca secrets/`environment`, que exigem autorização específica.
O guard registra o risco como **aviso** para manter o item visível.

## 3. O que o guard faz (e o que não faz)

`ci-guards.yml` — três jobs, `permissions: contents: read`, nenhum secret:

- **shell** — `sh -n` / `bash -n` em todo script de shell do índice
  (interprete escolhido pelo shebang). Hoje: `backend/scripts/verify.sh`.
- **workflows** — `scripts/ci/validate_workflows.py` (PyYAML `safe_load`) +
  `actionlint 1.7.12` (versão passada como argumento ao script de bootstrap, que
  é baixado de um **commit imutável** do repo do actionlint,
  `914e7df21a07ef503a81201c76d2b11c789d3fca`). Esse script **não confere
  checksum**: baixa o tarball da release por HTTPS e descompacta — a integridade
  do binário depende de TLS e da origem GitHub, não de hash (ver §5.3).
  Políticas: gatilho e `jobs` obrigatórios, `runs-on`/`uses` por job, `run`|`uses`
  por step, `uses` com ref pinada (aviso para `@main`/`@master`/`@HEAD`),
  `ci-guards.yml` sem `paths`, `deploy.yml`/`migrate.yml` sem gatilho `push`.
- **secrets** — `scripts/ci/secret_scan.py` (stdlib). Padrões: JWT, `sb_secret_*`,
  atribuição de `SUPABASE_SERVICE_ROLE_KEY`/`SUPABASE_ACCESS_TOKEN`/`SUPABASE_SECRET_KEY`,
  atribuição de `LINEAR_API_KEY`/`RAILWAY_TOKEN`/`GEMINI_API_KEY`/`DATABASE_URL`,
  `;Password=` de connection string, chave privada PEM, `AKIA`/`ASIA`, token GitHub,
  `Bearer eyJ`. Também falha se um `.env` estiver **rastreado**. Só reporta arquivo,
  linha, rótulo e comprimento (mascarado) — nunca o valor.

Não faz: não lê `~/.hermes/shared/.env` nem qualquer path do host, não roda o
canário/PIN do poller, não fala com a rede além do download do actionlint, não
dispara deploy/migração.

## 4. Evidências de validação (executadas nesta branch)

| Verificação | Comando | Resultado |
| --- | --- | --- |
| Secret scan no repo | `python3 scripts/ci/secret_scan.py` | `limpo` — 239 arquivos do índice, 9 padrões, 0 achado, nenhum `.env` rastreado (rc 0) |
| Validator no repo | `python3 scripts/ci/validate_workflows.py` | 7 workflows, **0 erros**, 1 aviso (`production.yml` push sem `paths`) (rc 0) |
| actionlint (real) | `actionlint -color` 1.7.12 | rc 0 nos 7 workflows (binário baixado por HTTPS/commit pinado; **sem** conferência de checksum pelo CI — sha256 do tarball medido localmente: `8aca8db9…`, ver §5.3) |
| actionlint + shellcheck | idem com `shellcheck 0.10.0` no PATH (= runner) | rc 0 |
| Sintaxe Python | `python3 -m compileall -q scripts/ci` | rc 0 |
| Sintaxe shell | `sh -n` / `bash -n backend/scripts/verify.sh` | OK |
| Probe positivo do scanner | 7 amostras sintéticas (JWT, `sb_secret_`, `LINEAR_API_KEY=`, `;Password=`, PEM, `AKIA`, `ghp_`) | 7/7 detectadas |
| Probe negativo do scanner | loopback de teste, password sintético do `migrate.yml`, código `.cs`, doc de grep, valor `<redacted>` | 5/5 sem falso positivo |
| Probe do `.env` rastreado | repo temporário com `.env` no índice | rc 1 (detecta) |
| Probe de regressão do validator | `ci-guards` com `paths` + `deploy` com `push` injetados | rc 1, 2 ERROs nomeados |

Ferramentas usadas localmente: `actionlint` e `shellcheck` **não existem no host**;
foram baixadas pinadas apenas para esta validação (o CI instala o actionlint no
job). `yamllint`/`gitleaks`/`trufflehog` seguem ausentes (não usados aqui).

## 5. Limitações conhecidas

1. **Nenhuma branch protection / required check** em `main` e `develop` (auditoria
   LOL-119, R3). Os guards passam a *existir*, mas continuam **não obrigatórios**:
   o merge não depende deles enquanto a proteção não for aplicada. Isso exige
   autorização específica e está no card separado (`t_2bc266bd`).
2. **Secret scan é da árvore, não do histórico** (`fetch-depth: 1`). Um segredo
   antigo, já removido, não é detectado — para isso seria preciso
   gitleaks/trufflehog com histórico completo (card futuro; a licença da
   `gitleaks-action` em organização precisa ser confirmada antes).
3. **Integridade do actionlint (claim corrigido em LOL-129).** O bootstrap aponta
   para o **commit** do script de download, mas o script em si vem do GitHub e
   **não confere checksum**: o `download-actionlint.bash` pinado baixa o tarball
   da release com `curl -L "${url}" | tar xvz` (sem `sha256sum`/`shasum` —
   conferido na fonte em 2026-10-06). A cadeia real é `commit imutável → script
   (sem hash) → tarball por HTTPS`, e a garantia de integridade é **TLS + origem
   GitHub**, não hash. Duas ressalvas: o script pinado declara default `1.7.11` e
   a versão efetiva vem do argumento explícito `1.7.12`; e existe checksum
   publicado e rastreável na própria release
   (`actionlint_1.7.12_checksums.txt`, em que o tarball `linux_amd64` confere com
   `8aca8db96f1b94770f1b0d72b6dddcb1ebb8123cb3712530b08cc387b349a3d8`,
   verificado localmente com `sha256sum -c`), mas **o CI atual não o usa**.
4. Guard estático não substitui teste de aplicação: `backend-ci`/`frontend-ci`
   continuam sendo quem prova build e testes.

## 6. Próximas ações

1. Revisão/QA desta branch no PR (sem merge nesta task).
2. Card de `devops` com autorização para tratar `production.yml` (remover ou virar
   `workflow_dispatch`), conforme a seção 2.
3. Card `t_2bc266bd`: branch protection + required checks em `main`/`develop`
   incluindo `ci-guards` — sem ele, o guard é informativo.

## 7. Errata — LOL-129 (2026-10-06): claim de sha256 do actionlint

Este documento (e o comentário do passo `actionlint` em `ci-guards.yml`) afirmava
que o bootstrap "confere o sha256 do tarball antes de instalar". **A afirmação era
incorreta.** Foi verificado lendo o script pinado
(`scripts/download-actionlint.bash` no commit
`914e7df21a07ef503a81201c76d2b11c789d3fca`): ele baixa o tarball da release com
`curl -L "${url}" | tar xvz`, sem nenhuma checagem de checksum.

Correção aplicada na branch `docs/LOL-129-ci-guards-actionlint-sha256` (PR
separado, sem merge):

- §3, §4 (linha do actionlint) e §5.3 reescritos para o modelo real de
  integridade: **TLS + origem GitHub e commit imutável do script**, sem hash;
- comentário do passo `actionlint` em `.github/workflows/ci-guards.yml`
  corrigido (não menciona mais sha256 nem "sem curl|bash solto").

Evidências do diagnóstico (2026-10-06, linux/amd64):

| Verificação | Resultado |
| --- | --- |
| `curl -sSfL <script no commit pinado>` + `sha256sum` | script sha256 `72fa3e45ac20f3c3a512d6747b4fcf719e21f890e8c43e78d48a41fdfb900c4e`; linha de download real: `curl -L "${url}" \| tar xvz`; nenhuma ocorrência de `sha256sum`/`shasum` |
| sha256 do `actionlint_1.7.12_linux_amd64.tar.gz` da release | `8aca8db96f1b94770f1b0d72b6dddcb1ebb8123cb3712530b08cc387b349a3d8` (2.353.908 B) |
| `sha256sum -c` contra `actionlint_1.7.12_checksums.txt` da release | `OK` — o checksum publicado confere com o tarball baixado |
| Versão do binário baixado | `actionlint 1.7.12` (linux/amd64) |

Validação desta branch (mesmos passos do `ci-guards` executados localmente):

| Verificação | Comando | Resultado |
| --- | --- | --- |
| Secret scan | `python3 scripts/ci/secret_scan.py` | `limpo` — 242 arquivos do índice, 9 padrões, 0 achado, nenhum `.env` rastreado (rc 0) |
| Validator | `python3 scripts/ci/validate_workflows.py` | 7 workflows, **0 erros**, 1 aviso pré-existente (`production.yml`) (rc 0) |
| actionlint | `actionlint -color` 1.7.12 nos 7 workflows | rc 0, sem saída (YAML do comentário alterado continua válido) |
| Sintaxe Python | `python3 -m compileall -q scripts/ci` | rc 0 |
| Sintaxe shell | `bash -n backend/scripts/verify.sh` | OK |

**Follow-up recomendado (não implementado: sairia do "diff mínimo" deste card).**
Substituir o bootstrap por download direto + `sha256sum -c` do valor acima (que é
rastreável ao arquivo de checksums publicado pela release), atualizando o pino a
cada bump de versão. Isso protegeria contra substituição do asset da release —
mas cria um valor a manter e acopla o passo à arquitetura `linux_amd64`; merece
card próprio.
