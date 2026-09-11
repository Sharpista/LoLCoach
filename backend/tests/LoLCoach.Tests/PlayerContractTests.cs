using System.Text.Json;
using LoLCoach.Api.Application;
using LoLCoach.Api.Domain;

namespace LoLCoach.Tests;

public sealed class PlayerContractTests
{
    [Fact]
    public void Dto_maps_only_local_player_and_serializes_full_camelCase_contract()
    {
        var time = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var player = new Player(Guid.NewGuid(), "fake-puuid", "Example", "TAG", "opaque-region", time, time);
        var dto = PlayerDto.FromPlayer(player);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, JsonSerializerOptions.Web));
        Assert.Equal(new[] { "createdAt", "gameName", "id", "lastUpdatedAt", "puuid", "region", "tagLine" },
            json.RootElement.EnumerateObject().Select(value => value.Name).Order().ToArray());
        Assert.Equal(player.Id, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(player.Puuid, dto.Puuid);
        Assert.Equal(player.GameName, dto.GameName);
        Assert.Equal(player.TagLine, dto.TagLine);
        Assert.Equal(player.Region, dto.Region);
        Assert.Equal(time, dto.CreatedAt);
        Assert.Equal(time, dto.LastUpdatedAt);
    }

    [Fact]
    public void Command_preserves_supplied_values_without_deciding_normalization()
    {
        var command = JsonSerializer.Deserialize<SearchPlayerCommand>(
            """{"gameName":" Example ","tagLine":"TAG","region":"opaque-region"}""", JsonSerializerOptions.Web);
        Assert.NotNull(command);
        Assert.Equal(" Example ", command.GameName);
        Assert.Equal("TAG", command.TagLine);
        Assert.Equal("opaque-region", command.Region);
    }
}
