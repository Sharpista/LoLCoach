using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LoLCoach.Api.Infrastructure;

public sealed class PlayerDbContextFactory : IDesignTimeDbContextFactory<PlayerDbContext>
{
    public PlayerDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__LoLCoach")
            ?? throw new InvalidOperationException("Configure ConnectionStrings__LoLCoach before using EF tools.");
        return new PlayerDbContext(new DbContextOptionsBuilder<PlayerDbContext>().UseNpgsql(connection).Options);
    }
}
