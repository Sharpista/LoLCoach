using System.Net;
using LoLCoach.Api.Application;
using LoLCoach.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoLCoach.Tests;

public sealed class HostTests
{
    [Fact]
    public async Task Host_registers_PostgreSQL_repository_without_connecting_or_migrating()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LoLCoach"] = "Host=127.0.0.1;Port=1;Database=lolcoach_no_connection"
                })));
        using var scope = factory.Services.CreateScope();
        Assert.IsType<PlayerRepository>(scope.ServiceProvider.GetRequiredService<IPlayerRepository>());
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL",
            scope.ServiceProvider.GetRequiredService<PlayerDbContext>().Database.ProviderName);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/")).StatusCode);
    }
}
