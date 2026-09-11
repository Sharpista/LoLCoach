using System.Net;
using System.Net.Http.Json;
using LoLCoach.Api.Application;

namespace LoLCoach.Api.Infrastructure;

public sealed class RiotAccountClient(HttpClient httpClient, IConfiguration configuration) : IRiotAccountClient
{
    public async Task<RiotAccount> GetAccountAsync(string gameName, string tagLine, string platform,
        CancellationToken cancellationToken = default)
    {
        if (!RiotRegions.TryGetRouting(platform, out var routing))
        {
            throw new RiotServiceUnavailableException();
        }

        var apiKey = configuration["Riot:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new RiotServiceUnavailableException();
        }

        var url = $"https://{routing}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/" +
                  $"{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Riot-Token", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RiotServiceUnavailableException();
        }
        catch (HttpRequestException)
        {
            throw new RiotServiceUnavailableException();
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                var account = await response.Content.ReadFromJsonAsync<RiotAccountResponse>(
                    cancellationToken: cancellationToken);
                if (account is null)
                {
                    throw new RiotServiceUnavailableException();
                }

                return new RiotAccount(account.Puuid, account.GameName, account.TagLine);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new RiotAccountNotFoundException();
            }

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

    private sealed record RiotAccountResponse(string Puuid, string GameName, string TagLine);
}
