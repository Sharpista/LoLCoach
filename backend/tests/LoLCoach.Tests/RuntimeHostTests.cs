using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LoLCoach.Api.Application;
using LoLCoach.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LoLCoach.Tests;

public sealed class RuntimeHostTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private sealed class FakeRiotAccountClient : IRiotAccountClient
    {
        public Func<string, string, string, CancellationToken, Task<RiotAccount>> Handler { get; set; } = null!;

        public Task<RiotAccount> GetAccountAsync(
            string gameName,
            string tagLine,
            string platform,
            CancellationToken cancellationToken = default)
            => Handler(gameName, tagLine, platform, cancellationToken);
    }

    private sealed class RuntimeWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly string _webRoot;
        private readonly Action<IServiceCollection>? _configureServices;
        private readonly Dictionary<string, string?> _settings;

        public RuntimeWebApplicationFactory(
            string connectionString,
            string webRoot,
            Action<IServiceCollection>? configureServices = null,
            Dictionary<string, string?>? settings = null)
        {
            _connectionString = connectionString;
            _webRoot = webRoot;
            _configureServices = configureServices;
            _settings = settings ?? [];
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseWebRoot(_webRoot);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var values = new Dictionary<string, string?>(_settings)
                {
                    ["ConnectionStrings:LoLCoach"] = _connectionString,
                };
                configuration.AddInMemoryCollection(values);
            });
            builder.ConfigureServices(services => _configureServices?.Invoke(services));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && Directory.Exists(_webRoot))
            {
                Directory.Delete(_webRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Health_returns_healthy_json_without_database_connectivity()
    {
        await using var factory = CreateFactory("Host=127.0.0.1;Port=1;Database=lolcoach_no_connection");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("healthy", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Readiness_returns_503_without_database_connectivity()
    {
        await using var factory = CreateFactory("Host=127.0.0.1;Port=1;Database=lolcoach_no_connection;Username=test;Password=secret-do-not-log");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-do-not-log", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Username=test", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Readiness_returns_200_with_database_connectivity()
    {
        await using var factory = CreateFactory(postgres.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("healthy", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Spa_fallback_serves_index_html_but_api_unknown_route_stays_problem_details()
    {
        await using var factory = CreateFactory(postgres.ConnectionString);
        using var client = factory.CreateClient();

        var root = await client.GetAsync("/");
        var player = await client.GetAsync($"/player/{Guid.NewGuid()}");
        var missingApi = await client.GetAsync("/api/nao-existe");

        Assert.Equal(HttpStatusCode.OK, root.StatusCode);
        Assert.Equal("text/html", root.Content.Headers.ContentType?.MediaType);
        Assert.Contains("LoLCoach test shell", await root.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, player.StatusCode);
        Assert.Equal("text/html", player.Content.Headers.ContentType?.MediaType);
        Assert.Contains("LoLCoach test shell", await player.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, missingApi.StatusCode);
        Assert.Equal("application/problem+json", missingApi.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("LoLCoach test shell", await missingApi.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_endpoint_contract_remains_camel_case_after_spa_fallback()
    {
        var puuid = $"puuid-runtime-{Guid.NewGuid():N}";
        await using var factory = CreateFactory(
            postgres.ConnectionString,
            services =>
            {
                services.RemoveAll<IRiotAccountClient>();
                services.AddSingleton<IRiotAccountClient>(new FakeRiotAccountClient
                {
                    Handler = (_, _, _, _) => Task.FromResult(new RiotAccount(puuid, "Example", "TAG"))
                });
            });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/players/search",
            new { gameName = "Example", tagLine = "TAG", region = "br1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(new[] { "createdAt", "gameName", "id", "lastUpdatedAt", "puuid", "region", "tagLine" },
            root.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(puuid, root.GetProperty("puuid").GetString());
        Assert.Equal("Example", root.GetProperty("gameName").GetString());
        Assert.Equal("TAG", root.GetProperty("tagLine").GetString());
        Assert.Equal("br1", root.GetProperty("region").GetString());
    }

    [Fact]
    public async Task Cors_denies_unconfigured_origin_in_production()
    {
        await using var factory = CreateFactory(postgres.ConnectionString);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://evil.example");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Forwarded_headers_enabled_accepts_proxy_scheme_header()
    {
        await using var factory = CreateFactory(
            postgres.ConnectionString,
            settings: new Dictionary<string, string?>
            {
                ["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true",
            });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", "203.0.113.10");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static RuntimeWebApplicationFactory CreateFactory(
        string connectionString,
        Action<IServiceCollection>? configureServices = null,
        Dictionary<string, string?>? settings = null)
    {
        var webRoot = Directory.CreateTempSubdirectory("lolcoach-wwwroot-");
        File.WriteAllText(Path.Combine(webRoot.FullName, "index.html"),
            "<!doctype html><html><head><title>LoLCoach test shell</title></head><body>LoLCoach test shell</body></html>");

        return new RuntimeWebApplicationFactory(connectionString, webRoot.FullName, configureServices, settings);
    }
}
