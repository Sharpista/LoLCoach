using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using LoLCoach.Api.Application;
using Microsoft.Extensions.Options;

namespace LoLCoach.Api.Infrastructure;

public sealed partial class GeminiAiCoach(HttpClient httpClient, IOptions<AiCoachOptions> options) : IAiCoach
{
    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;
    private readonly AiCoachOptions _options = options.Value;

    public async Task<CoachReport> CreateReportAsync(CoachAnalysisInput input, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            throw new AiCoachUnavailableException("AI Coach is disabled.");
        if (!string.Equals(_options.Provider, "Gemini", StringComparison.OrdinalIgnoreCase))
            throw new AiCoachUnavailableException("Configured AI Coach provider is not supported by this adapter.");
        if (string.IsNullOrWhiteSpace(_options.GeminiApiKey))
            throw new AiCoachUnavailableException("Gemini API key is not configured.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            var response = await httpClient.PostAsJsonAsync(BuildEndpoint(), BuildRequest(input), JsonOptions, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new AiCoachUnavailableException("Gemini AI Coach request failed.");

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, timeout.Token);
            var text = ExtractCandidateText(payload);
            var report = JsonSerializer.Deserialize<CoachReport>(text, JsonOptions)
                ?? throw new AiCoachUnavailableException("Gemini AI Coach returned an empty report.");

            return ValidateReport(report with { GeneratedByAi = true }, input);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiCoachUnavailableException("Gemini AI Coach request timed out.", exception);
        }
        catch (JsonException exception)
        {
            throw new AiCoachUnavailableException("Gemini AI Coach returned invalid JSON.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AiCoachUnavailableException("Gemini AI Coach request failed.", exception);
        }
    }

    private string BuildEndpoint()
    {
        var endpoint = _options.GeminiEndpoint.Replace("{model}", Uri.EscapeDataString(_options.Model), StringComparison.Ordinal);
        var separator = endpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{endpoint}{separator}key={Uri.EscapeDataString(_options.GeminiApiKey!)}";
    }

    private static object BuildRequest(CoachAnalysisInput input) => new
    {
        contents = new[]
        {
            new
            {
                role = "user",
                parts = new[] { new { text = BuildPrompt(input) } }
            }
        },
        generationConfig = new
        {
            responseMimeType = "application/json",
            temperature = 0.2,
        }
    };

    private static string BuildPrompt(CoachAnalysisInput input)
        => $$"""
Você é o AI Coach do LoLCoach. Use somente o payload estruturado abaixo.
Não invente estatísticas, não altere números calculados e não use JSON bruto da Riot.
Todo número mencionado deve existir literalmente no payload.
Responda apenas JSON válido no formato:
{"periodSummary":"texto","strengths":["até 3"],"weaknesses":["até 3"],"mainPriority":"texto","plan":[{"goalMetric":"metric","targetValue":0,"action":"texto"}],"generatedByAi":true}
Cada item de plan deve usar goalMetric e targetValue de uma recomendação fornecida.
Payload validado:
{{JsonSerializer.Serialize(input, JsonOptions)}}
""";

    private static string ExtractCandidateText(JsonElement payload)
    {
        if (!payload.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            throw new AiCoachUnavailableException("Gemini AI Coach returned no candidates.");

        var parts = candidates[0].GetProperty("content").GetProperty("parts");
        if (parts.GetArrayLength() == 0 || !parts[0].TryGetProperty("text", out var text))
            throw new AiCoachUnavailableException("Gemini AI Coach returned no text.");

        return text.GetString() ?? throw new AiCoachUnavailableException("Gemini AI Coach returned empty text.");
    }

    private static CoachReport ValidateReport(CoachReport report, CoachAnalysisInput input)
    {
        if (report.Strengths.Count > 3 || report.Weaknesses.Count > 3 || report.Plan.Count > 3)
            throw new AiCoachUnavailableException("Gemini AI Coach exceeded output limits.");

        var allowedGoals = input.Recommendations
            .Select(recommendation => (recommendation.GoalMetric, recommendation.TargetValue))
            .ToHashSet();
        if (report.Plan.Any(goal => !allowedGoals.Contains((goal.GoalMetric, goal.TargetValue))))
            throw new AiCoachUnavailableException("Gemini AI Coach returned a plan outside provided recommendations.");

        var allowedNumbers = CollectAllowedNumbers(input);
        var text = string.Join(' ', [report.PeriodSummary, report.MainPriority, .. report.Strengths, .. report.Weaknesses, .. report.Plan.Select(goal => goal.Action)]);
        foreach (Match match in NumberRegex().Matches(text))
        {
            var token = match.Value.TrimEnd('%');
            if (decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) && !allowedNumbers.Contains(number))
                throw new AiCoachUnavailableException("Gemini AI Coach returned a number outside the validated payload.");
        }

        return report;
    }

    private static HashSet<decimal> CollectAllowedNumbers(CoachAnalysisInput input)
    {
        HashSet<decimal> numbers =
        [
            input.Summary.MatchesAnalysed,
            input.Summary.Winrate,
            input.Summary.Kda,
            input.Summary.CsPerMin,
            input.Summary.VisionPerMin,
            input.Summary.DamagePerMin,
        ];

        foreach (var insight in input.Insights)
        {
            numbers.Add(insight.CurrentValue);
            numbers.Add(insight.TargetValue);
            numbers.Add(insight.MatchesAnalyzed);
        }

        foreach (var recommendation in input.Recommendations)
        {
            numbers.Add(recommendation.TargetValue);
        }

        return numbers;
    }

    [GeneratedRegex(@"\b\d+(?:\.\d+)?%?\b", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();
}
