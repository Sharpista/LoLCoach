using LoLCoach.Api.Domain;

namespace LoLCoach.Tests;

public class PlayerTests
{
    [Fact]
    public void Player_preserves_explicit_identity_and_account_fields()
    {
        var id = Guid.NewGuid();
        var created = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var player = new Player(id, "puuid-test", "Example", "TAG", "opaque-region", created, created);
        Assert.Equal(id, player.Id);
        Assert.Equal("puuid-test", player.Puuid);
        Assert.Equal("Example", player.GameName);
        Assert.Equal("TAG", player.TagLine);
        Assert.Equal("opaque-region", player.Region);
        Assert.Equal(created, player.CreatedAt);
        Assert.Equal(created, player.LastUpdatedAt);
    }
}
