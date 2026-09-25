using Npgsql;

namespace LoLCoach.Api.Infrastructure;

public static class PostgresConnectionString
{
    public static string Normalize(string connection)
    {
        if (!connection.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
            !connection.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            return connection;

        var uri = new Uri(connection);
        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2 || string.IsNullOrWhiteSpace(uri.Host) || uri.AbsolutePath == "/")
            throw new ArgumentException("The PostgreSQL URI must contain a user, password, host and database.");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1]),
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            SslMode = SslMode.Require,
        };
        return builder.ConnectionString;
    }
}
