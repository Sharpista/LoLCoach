using LoLCoach.Api.Infrastructure;
using Npgsql;

namespace LoLCoach.Tests;

public sealed class PostgresConnectionStringTests
{
    [Fact]
    public void NormalizesSupabaseUriWithEncodedCredentials()
    {
        var result = PostgresConnectionString.Normalize(
            "postgresql://postgres.project:p%40ss%3Bword@pooler.example.com:6543/postgres");

        var parsed = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("pooler.example.com", parsed.Host);
        Assert.Equal(6543, parsed.Port);
        Assert.Equal("postgres.project", parsed.Username);
        Assert.Equal("p@ss;word", parsed.Password);
        Assert.Equal("postgres", parsed.Database);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void LeavesNpgsqlConnectionStringIntact()
    {
        const string connection = "Host=localhost;Database=postgres;Username=postgres;Password=test";
        Assert.Equal(connection, PostgresConnectionString.Normalize(connection));
    }
}
