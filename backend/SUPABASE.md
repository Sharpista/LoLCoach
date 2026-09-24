# Supabase (PostgreSQL) — LoLCoach

Provisão do banco PostgreSQL hospedado (Supabase) para o backend LoLCoach.

## Identificação do projeto

| Campo              | Valor                                   |
|--------------------|-----------------------------------------|
| Project reference  | `tsfdsjmostggtxckmirr`                  |
| Nome do projeto    | `LoLCoach`                              |
| Organização        | `cjlywaadeyqeafxbivzt` (Sharpista's Org)|
| Região             | `us-east-1`                             |
| Status             | `ACTIVE_HEALTHY`                        |
| PostgreSQL         | 17.6                                    |

> O projeto já existia na organização (criado anteriormente) e foi reutilizado.
> A senha do banco foi rotacionada durante esta provisão (Management API:
> `PATCH /v1/projects/{ref}/database/password`).

## Conexão (pooler de transação)

| Campo     | Valor                                                        |
|-----------|--------------------------------------------------------------|
| Host      | `aws-0-us-east-1.pooler.supabase.com`                        |
| Porta     | `6543`                                                       |
| Database  | `postgres`                                                   |
| Usuário   | `postgres.tsfdsjmostggtxckmirr`                              |
| Auth      | SCRAM (TLS)                                                  |

Formato da connection string (senha omitida):

```
postgres://postgres.tsfdsjmostggtxckmirr:***@aws-0-us-east-1.pooler.supabase.com:6543/postgres
```

## Onde as credenciais estão armazenadas

A senha do banco e a connection string **nunca** são versionadas neste repositório.

- **CI/deploy (GitHub):** secret `CONNECTIONSTRINGS__LOLCOACH` no repositório
  `Sharpista/LoLCoach` (GitHub Actions Secrets). O valor é a connection string completa.
- **Desenvolvimento local:** variável `CONNECTIONSTRINGS__LOLCOACH` em
  `~/.hermes/profiles/orquestrador/.env` (fora do repositório, não versionado).
  Adicionar/confirmar:

  ```
  CONNECTIONSTRINGS__LOLCOACH=postgres://postgres.tsfdsjmostggtxckmirr:SENHA@aws-0-us-east-1.pooler.supabase.com:6543/postgres
  ```

## Como o backend consome

O backend .NET (EF Core / Npgsql) lê a connection string via variável de ambiente
`ConnectionStrings__LoLCoach` (o `__` mapeia para a seção `ConnectionStrings:LoLCoach`
do sistema de configuração do ASP.NET Core). Nenhuma alteração de código `.cs` é
necessária — basta expor a variável no ambiente do processo (ou o secret no CI).

## Conexão direta (alternativa, sem pooler)

- Host direto: `db.tsfdsjmostggtxckmirr.supabase.co`, porta `5432`, mesmo usuário/senha.
- Preferir o pooler (`...pooler.supabase.com:6543`) para aplicações com muitas conexões curtas.
