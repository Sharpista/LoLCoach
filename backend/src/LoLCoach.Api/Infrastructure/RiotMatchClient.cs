using System.Net;
using System.Net.Http.Json;
using LoLCoach.Api.Application;

namespace LoLCoach.Api.Infrastructure;

public sealed class RiotMatchClient(HttpClient httpClient, IConfiguration configuration) : IRiotMatchClient
{
    private const int MaxMatchCount = 20;

    public async Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, string platform, int count,
        CancellationToken cancellationToken = default)
    {
        if (!TryBuildRequestBase(platform, out var routing, out var apiKey))
        {
            throw new RiotServiceUnavailableException();
        }

        var safeCount = Math.Clamp(count, 1, MaxMatchCount);
        var url = $"https://{routing}.api.riotgames.com/lol/match/v5/matches/by-puuid/" +
                  $"{Uri.EscapeDataString(puuid)}/ids?start=0&count={safeCount}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Riot-Token", apiKey);

        using var response = await SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>(cancellationToken) ?? [];
        }

        ThrowForFailure(response);
        throw new RiotServiceUnavailableException();
    }

    public async Task<RiotMatchDetails> GetMatchAsync(string matchId, string platform,
        CancellationToken cancellationToken = default)
    {
        if (!TryBuildRequestBase(platform, out var routing, out var apiKey))
        {
            throw new RiotServiceUnavailableException();
        }

        var url = $"https://{routing}.api.riotgames.com/lol/match/v5/matches/{Uri.EscapeDataString(matchId)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Riot-Token", apiKey);

        using var response = await SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadFromJsonAsync<RiotMatchDetails>(cancellationToken);
            return details ?? throw new RiotServiceUnavailableException();
        }

        ThrowForFailure(response);
        throw new RiotServiceUnavailableException();
    }

    private bool TryBuildRequestBase(string platform, out string routing, out string apiKey)
    {
        apiKey = configuration["Riot:ApiKey"] ?? string.Empty;
        return RiotRegions.TryGetRouting(platform, out routing) && !string.IsNullOrWhiteSpace(apiKey);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RiotServiceUnavailableException();
        }
        catch (HttpRequestException)
        {
            throw new RiotServiceUnavailableException();
        }
    }

    private static void ThrowForFailure(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            response.Headers.TryGetValues("Retry-After", out var values);
            var retryAfter = values is not null && int.TryParse(values.FirstOrDefault(), out var seconds)
                ? seconds
                : (int?)null;
            throw new RiotRateLimitedException(retryAfter);
        }

        throw new RiotServiceUnavailableException();
    }
}
