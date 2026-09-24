# Evidência de entrega — PR do runtime Hermes (LOL-56/57/58/59)

- **Card Kanban:** `t_ea74cf6c` (`github-profile`)
- **run_id:** `run_01M3A9VMVX4JFQ6CP7ZPWRKM8K`
- **run local:** 120
- **Data:** 2026-09-24 ~21:25 CEST
- **Issues Linear:** [LOL-56](https://linear.app/lolcoach/issue/LOL-56/implementar-roteamento-automatico-linear-hermes-por-labels-agent), LOL-57, LOL-58, LOL-59
- **Autorização:** o card autoriza commit, push normal da feature branch e abertura de PR; proíbe merge, deploy Railway, tag, release, force push e alteração de secrets. Nada além disso foi executado.

## 1. Artefatos publicados

| Artefato | Repositório | Branch | SHA publicado | PR |
|---|---|---|---|---|
| Pacote `hermes_agent_runtime` (código) | `Sharpista/hermes_agent_runtime` | `feat/LOL-56-59-runtime-lifecycle` | `c5db74373856a28a361434842fa225797bc94f52` | [#1](https://github.com/Sharpista/hermes_agent_runtime/pull/1) (base `main`) |
| Spec 009 + evidências (docs) | `Sharpista/LoLCoach` | `feat/LOL-56-runtime` | `135f861ceacfb902b932f828ad679986039367a5` | [#27](https://github.com/Sharpista/LoLCoach/pull/27) (base `develop`) |

Ambos os PRs estão `OPEN`, `MERGEABLE`, `mergeStateStatus=CLEAN`, sem draft.

## 2. Rastreabilidade candidato revisado → SHA publicado

O candidato aprovado por QA/review **não era um commit**, e sim `ed6a2d9` (init) **+ working tree** de `/home/alexandre/hermes-agent-runtime`. A publicação foi feita a partir de uma cópia byte-idêntica dessa árvore, e o SHA publicado foi conferido contra a árvore revisada lendo os blobs de volta do remoto:

| Arquivo | sha256 (árvore revisada == blob remoto) |
|---|---|
| `README.md` | `79f7547612aa86a702d6866273f3f8e7ea9c7ba4cdbce8f296e484ffaccc62b8` |
| `sql/001_runtime_functions.sql` | `684ba8a9b470fc3e9b6893d9796c589d9ae9773f9dfd43491c2983f89f0051ca` |
| `src/hermes_agent_runtime/__init__.py` | `873fdf6c93465d3366d1d5b38ec6558e23d802e51fc2d5f9059ae4db9439e975` |
| `src/hermes_agent_runtime/runtime.py` | `d2a8f06dcb0dc6d4ef295bfc3b4637d24156e9d7f51e3ea8292749083fd2d2e2` |
| `src/hermes_agent_runtime/supabase.py` | `012799e3178d5973ac5f5b84e9d43ce242acf7c7462acd109f4ae1c58c88dcc6` |
| `src/hermes_agent_runtime/kanban.py` | `63fc24997cd4dc37950bc984b29e41a48bbe12cd9bb2e6797028fd77f07f5468` |
| `tests/test_runtime.py` | `2686053c99f2cfdd60678e2b5fbf5271e09acde70adaaa97d956350bd6c97de7` |

Método: `sha256sum` em cada arquivo da árvore revisada antes do commit, e depois `git show <sha-remoto>:<path> | sha256sum` sobre o ref buscado do remoto (`git fetch origin`). Todos os 7 arquivos: `MATCH`.

Os 12 arquivos da spec 009 no LoLCoach foram conferidos pelo mesmo método contra o blob remoto: todos `MATCH`.

## 3. Evidência herdada (verificada quanto a coerência, não reexecutada)

| Item | Resultado | Fonte |
|---|---|---|
| Testes unitários do pacote | 10/10 OK | `t_0b723f09` (review) e reexecução local na cópia publicada |
| QA independente | APROVADO — 24/24 (10 unit + 14 integração), 1 skipped (`SUPABASE_ANON_KEY` ausente) | `t_4c601578`, `evidence/qa-concurrency-lifecycle-report.md` |
| Code review final | APROVADO COM NOTAS — 0 bloqueante, 0 correção necessária; M1/M2/M3 não bloqueantes | `t_0b723f09`, `evidence/code-review-report.md` |
| Migration LOL-59 | aplicada e validada (run 117): 24+15+17 = 56/56 PASS | `t_cd2022e2`, `evidence/lol-59-runtime-supabase-apply-run117.md` |

Reprodução local executada nesta entrega, na cópia que gerou o commit publicado:
`PYTHONPATH=src python3 -m unittest discover -s tests -v` → `Ran 10 tests ... OK`.

## 4. CI / checks

| PR | Checks | Leitura independente |
|---|---|---|
| runtime #1 | nenhum | `gh pr checks 1` → “no checks reported”; `gh run list` no repositório → vazio. O repositório não possui `.github/workflows` |
| LoLCoach #27 | nenhum | `gh pr checks 27` → “no checks reported”; `gh run list --branch feat/LOL-56-runtime` → vazio |

As mudanças da PR #27 são `.md` e `.sql`; os workflows do LoLCoach têm path filters (`backend/**`, `frontend/**`, `**/*.py`) e não são acionados. **Ausência de execução por path filter não é CI verde** — nenhum check foi executado e nenhum é exigido (mergeStateStatus `CLEAN`).

## 5. Exclusões deliberadas

- `migration/repro/**` (reprodução local e dumps de ambiente de `t_cd2022e2`): não versionado. `migration/README.md` referencia esses caminhos, então os comandos de repro citados nele não resolvem a partir do repositório. Dono para eventual versionamento: `devops`.
- `qa_runtime_integration.py` (suíte de integração de `t_4c601578`, citada em `Artefatos` do relatório de QA em caminho absoluto do worktree): não versionado neste PR. Dono: `qualidade`.
- Ambos foram excluídos por serem artefatos de scratch/repro em repositório **público**, sem credencial detectada (scans de `eyJ…`, `sk-…`, `gh[opsur]_…`, `AKIA…`, `sb_secret_…`, `Password=…` no diff staged: nenhum hit).

## 6. Revisão de segurança da publicação

- Nenhuma credencial no diff das duas PRs (scan por padrões antes de cada commit).
- Os artefatos de evidência citam o ref do projeto Supabase (`tsfdsjmostggtxckmirr`) e o papel `service_role`, **sem chave**: nenhum JWT/`anon`/`service_role` key, connection string ou token está presente. O ref do projeto é um identificador de endpoint, não uma credencial.
- `sql/001_runtime_functions.proposed.sql` publicado confere por sha256 com o artefato aplicado (`2cdb809d…`).

## 7. Estado e limites

- **Preparado e publicado:** 2 PRs abertas, com SHA remoto conferido por leitura independente.
- **Não executado:** merge, tag, release, deploy, aplicação de migration, alteração de secrets/permissões.
- **Pendências com dono:** M1/M2/M3 do code review (dev-backend + devops) como follow-up não bloqueante; eventual versionamento de `migration/repro/**` (devops) e da suíte de integração (qualidade).
- As branches locais/worktrees dos demais perfis não foram alteradas nem removidas.
