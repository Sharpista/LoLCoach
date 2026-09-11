# LoLCoach backend — entrega parcial, spec 001 BLOCKED

Base de persistência executável em .NET 10. **POST /api/players/search ainda não existe.**
As Open Questions em `../specs/001-player-search/spec.md` precisam de decisão sobre `region` e validação de IDs legados antes de implementar o fluxo dependente.

## Executar verificação reproduzível

Requisitos: SDK .NET 10, Docker acessível, Python 3 (somente smoke). Não exige chave Riot, banco previamente criado ou cluster local. Os testes criam PostgreSQL 16 descartável via Testcontainers, senha aleatória, porta dinâmica vinculada a loopback, banco/usuário `lolcoach_tests`, container `lolcoach-tests-<guid>`. Testcontainers também gerencia seu container auxiliar de limpeza Ryuk. Não desabilite a limpeza nem reutilize banco de outro projeto.

A partir da raiz `/home/alexandre/LoLSaas`:

```bash
bash backend/scripts/verify.sh
```

Comandos individuais:

```bash
dotnet build backend/LoLCoach.slnx -v minimal
dotnet test backend/LoLCoach.slnx -v minimal
dotnet format backend/LoLCoach.slnx --verify-no-changes --no-restore
python3 backend/scripts/smoke_host.py
```

Sem Docker, somente testes sem persistência:

```bash
dotnet test backend/LoLCoach.slnx --filter 'FullyQualifiedName!~PlayerRepositoryTests'
```

Essa seleção **não valida PostgreSQL/migrations**. A suíte completa falha, em vez de pular silenciosamente, quando Docker está indisponível.

## Configuração segura e migrations

`ConnectionStrings__LoLCoach` é a única configuração de persistência usada. Não há fallback para localhost:5432, schema compartilhado ou conexão de outro projeto. Configure no ambiente por mecanismo seguro de injeção de secrets. Formato (placeholders, não credencial):

```text
Host=<host>;Port=<porta>;Database=<banco-exclusivo-lolcoach>;Username=<usuario>;Password=<secret>
```

Não incluir valores em source, argumentos de processo, logs, shell history ou `appsettings*.json`. Não habilitar `EnableSensitiveDataLogging` nem `Include Error Detail` em ambientes compartilhados. `Riot__ApiKey` é configuração futura: não há client que a utilize nesta entrega.

Com a variável segura já configurada e banco **exclusivo autorizado** provisionado:

```bash
cd /home/alexandre/LoLSaas/backend
dotnet tool restore
dotnet ef database update --project src/LoLCoach.Api
```

Migration nova: `20260911183231_InitialPlayers`. Testes aplicam via `Database.MigrateAsync`, nunca `EnsureCreated`, e conferem ausência de model drift. O startup não migra nem abre conexão automaticamente. A factory design-time exige env; não carrega secrets de outros projetos.

Para iniciar apenas o host base, sem banco/key:

```bash
dotnet run --project backend/src/LoLCoach.Api --no-launch-profile --urls http://127.0.0.1:5181
```

Selecione porta livre. O host não tem rotas de negócio; `/` e `/api/players/search` retornam 404 de **rota ausente**, não o 404 de jogador inexistente da spec. O script smoke usa porta efêmera, registra PID/porta/comando em `backend/artifacts/smoke-host.json` e encerra o próprio processo.

## Estrutura e dependências

- `src/LoLCoach.Api/Domain/Player.cs`: entidade básica, sem inventar validação de Riot ID.
- `Application`: command de entrada, DTO local camelCase quando serializado com defaults Web, interface repository.
- `Infrastructure`: EF/Npgsql, factory design-time, upsert PostgreSQL parametrizado e migration.
- `Program.cs`: composição DI do host; sem controller/endpoint incompleto ou resposta fake.
- `tests/LoLCoach.Tests`: entidade/contratos, DI/host e integração PostgreSQL.
- EF Core Design 10.0.0 + ferramenta local dotnet-ef 10.0.0: geração/aplicação de migrations.
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0: provider compatível com net10.0.
- xUnit/Test SDK/runner: execução de testes; coverlet veio do template para coleta opcional (nenhum percentual alegado).
- Microsoft.AspNetCore.Mvc.Testing 10.0.0: composição real da API em TestServer.
- Testcontainers.PostgreSql 4.15.0: PostgreSQL descartável, verificação do índice e concorrência. A tentativa inicial 4.7.0 trouxe SSH.NET vulnerável; versão atualizada removeu o alerta sem suprimir auditoria.
- FluentValidation/HttpClientFactory serão usados no fluxo bloqueado; sem dependências ociosas adicionadas agora.

## Evidência e limites

Ver `VERIFICATION.md`. Artefatos de execução ficam ignorados em `artifacts/`; não são secrets nem parte do código.
Não há integração Riot real ou fake aprovada nesta entrega. Testes usam **dados sintéticos de conta** diretamente na camada de persistência; não simulam resposta HTTP da Riot.
Frontend, QA e code review pendentes. Não marcar a feature DONE/REVIEW enquanto faltar implementação.
