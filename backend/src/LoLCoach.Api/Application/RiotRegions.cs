namespace LoLCoach.Api.Application;

/// <summary>
/// Maps LoL platform identifiers to the Riot API regional routing consumed by ACCOUNT-V1.
/// The user supplies a platform (e.g. br1, euw1, na1, kr); the routing (americas, asia,
/// europe, sea) is derived internally and is never persisted or returned.
/// </summary>
public static class RiotRegions
{
    private static readonly IReadOnlyDictionary<string, string> PlatformToRouting =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["na1"] = "americas",
            ["br1"] = "americas",
            ["la1"] = "americas",
            ["la2"] = "americas",
            ["kr"] = "asia",
            ["jp1"] = "asia",
            ["euw1"] = "europe",
            ["eun1"] = "europe",
            ["tr1"] = "europe",
            ["ru"] = "europe",
            ["oc1"] = "sea",
            ["ph2"] = "sea",
            ["sg2"] = "sea",
            ["th2"] = "sea",
            ["tw2"] = "sea",
            ["vn2"] = "sea",
        };

    public static IReadOnlyCollection<string> SupportedPlatforms => PlatformToRouting.Keys
        .Order(StringComparer.Ordinal)
        .ToArray();

    /// <summary>Normalizes a platform to lowercase and reports whether it is supported.</summary>
    public static bool TryNormalizePlatform(string? region, out string platform)
    {
        platform = string.Empty;
        if (string.IsNullOrWhiteSpace(region))
        {
            return false;
        }

        var candidate = region.Trim();
        if (!PlatformToRouting.ContainsKey(candidate))
        {
            return false;
        }

        platform = candidate.ToLowerInvariant();
        return true;
    }

    /// <summary>Returns the Riot regional routing for an already-normalized platform.</summary>
    public static bool TryGetRouting(string platform, out string routing)
        => PlatformToRouting.TryGetValue(platform, out routing!);
}
