using Testcontainers.PostgreSql;
using LoLCoach.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LoLCoach.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithName($"lolcoach-tests-{Guid.NewGuid():N}")
        .WithDatabase("lolcoach_tests")
        .WithUsername("lolcoach_tests")
        .WithPassword(Guid.NewGuid().ToString("N"))
        .WithPortBinding(5432, true)
        .WithCreateParameterModifier(parameters =>
        {
            foreach (var binding in parameters.HostConfig!.PortBindings!.Values.SelectMany(value => value))
                binding.HostIP = "127.0.0.1";
        })
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public PlayerDbContext CreateContext() => new(new DbContextOptionsBuilder<PlayerDbContext>()
        .UseNpgsql(_container.GetConnectionString()).Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
