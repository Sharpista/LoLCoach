using System.Net;
using System.Text.Json;
using LoLCoach.Api.Application;
using LoLCoach.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoLCoach.Tests;

public sealed class HostTests
{
    [Fact]
    public async Task Swagger_document_contains_player_search_operation_without_starting_dependencies()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development").ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LoLCoach"] = "Host=127.0.0.1;Port=1;Database=lolcoach_no_connection"
                })));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operation = document.RootElement.GetProperty("paths")
            .GetProperty("/api/players/search").GetProperty("post");
        Assert.Contains("200", operation.GetProperty("responses").EnumerateObject().Select(property => property.Name));
        Assert.Contains("400", operation.GetProperty("responses").EnumerateObject().Select(property => property.Name));
        Assert.Contains("404", operation.GetProperty("responses").EnumerateObject().Select(property => property.Name));
        Assert.Contains("429", operation.GetProperty("responses").EnumerateObject().Select(property => property.Name));
        Assert.Contains("503", operation.GetProperty("responses").EnumerateObject().Select(property => property.Name));
        var requestSchema = operation.GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");
        Assert.Equal("#/components/schemas/SearchPlayerCommand", requestSchema.GetProperty("$ref").GetString());
        var properties = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("SearchPlayerCommand").GetProperty("properties");
        Assert.Contains("gameName", properties.EnumerateObject().Select(property => property.Name));
        Assert.Contains("tagLine", properties.EnumerateObject().Select(property => property.Name));
        Assert.Contains("region", properties.EnumerateObject().Select(property => property.Name));
    }

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
