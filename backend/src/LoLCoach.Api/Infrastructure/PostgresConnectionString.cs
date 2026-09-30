using System.Net;
using Npgsql;

namespace LoLCoach.Api.Infrastructure;

public static class PostgresConnectionString
{
    public static string Normalize(string connection)
    {
        if (!Uri.TryCreate(connection, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return connection;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            SslMode = RequiresTls(uri.Host) ? SslMode.Require : SslMode.Prefer,
        };

        if (userInfo.Length > 0 && userInfo[0].Length > 0)
        {
            builder.Username = Uri.UnescapeDataString(userInfo[0]);
        }

        if (userInfo.Length > 1)
        {
            builder.Password = Uri.UnescapeDataString(userInfo[1]);
        }

        foreach (var parameter in uri.Query.TrimStart('?')
                     .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = parameter.Split('=', 2);
            var key = Uri.UnescapeDataString(pair[0]).Replace('_', ' ');
            var value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
            builder[key] = value;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// URIs remotas (Supabase, pooler) exigem TLS por padrao. Hosts de loopback
    /// (desenvolvimento local e Testcontainers) permanecem em Prefer para nao
    /// exigir TLS de um PostgreSQL sem certificado, e um parametro de query
    /// explicito (por exemplo <c>?sslmode=require</c>) prevalece sobre o padrao.
    /// </summary>
    private static bool RequiresTls(string host)
    {
        if (host is "localhost" or "127.0.0.1" or "::1" or "[::1]")
        {
            return false;
        }

        return !(IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
    }
}
