using System.ComponentModel.DataAnnotations;

namespace LoLCoach.Api.Infrastructure;

public sealed class AiCoachOptions
{
    public const string SectionName = "AiCoach";

    public bool Enabled { get; set; }

    [Required]
    public string Provider { get; set; } = "Gemini";

    [Required]
    public string Model { get; set; } = "gemini-3.8";

    [Range(1, 30)]
    public int TimeoutSeconds { get; set; } = 10;

    public string? GeminiApiKey { get; set; }

    [Required]
    public string GeminiEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
}
