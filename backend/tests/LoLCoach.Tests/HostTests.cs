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
        AssertResponseContentTypes(operation, "200", "application/json");
        AssertResponseContentTypes(operation, "400", "application/problem+json");
        AssertResponseContentTypes(operation, "404", "application/problem+json");
        AssertResponseContentTypes(operation, "429", "application/problem+json");
        AssertResponseContentTypes(operation, "503", "application/problem+json");
        var requestSchema = operation.GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");
        Assert.Equal("#/components/schemas/SearchPlayerCommand", requestSchema.GetProperty("$ref").GetString());
        var properties = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("SearchPlayerCommand").GetProperty("properties");
        Assert.Contains("gameName", properties.EnumerateObject().Select(property => property.Name));
        Assert.Contains("tagLine", properties.EnumerateObject().Select(property => property.Name));
        Assert.Contains("region", properties.EnumerateObject().Select(property => property.Name));
        var required = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("SearchPlayerCommand").GetProperty("required")
            .EnumerateArray().Select(value => value.GetString()!).ToArray();
        Assert.Equal(["gameName", "tagLine", "region"], required);
        var gameName = properties.GetProperty("gameName");
        Assert.Equal(3, gameName.GetProperty("minLength").GetInt32());
        Assert.Equal(16, gameName.GetProperty("maxLength").GetInt32());
        Assert.DoesNotContain("pattern", gameName.EnumerateObject().Select(property => property.Name));
        var tagLine = properties.GetProperty("tagLine");
        Assert.Equal(2, tagLine.GetProperty("minLength").GetInt32());
        Assert.Equal(5, tagLine.GetProperty("maxLength").GetInt32());
        Assert.Equal("^ *[A-Za-z0-9]+ *$", tagLine.GetProperty("pattern").GetString());
        var regions = properties.GetProperty("region").GetProperty("enum")
            .EnumerateArray().Select(value => value.GetString()!).ToArray();
        Assert.Equal(["br1", "eun1", "euw1", "jp1", "kr", "la1", "la2", "na1",
            "oc1", "ph2", "ru", "sg2", "th2", "tr1", "tw2", "vn2"], regions);
    }

    private static void AssertResponseContentTypes(JsonElement operation, string statusCode, params string[] expectedContentTypes)
    {
        var contentTypes = operation.GetProperty("responses").GetProperty(statusCode).GetProperty("content")
            .EnumerateObject().Select(property => property.Name).ToArray();
        Assert.Equal(expectedContentTypes, contentTypes);
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
