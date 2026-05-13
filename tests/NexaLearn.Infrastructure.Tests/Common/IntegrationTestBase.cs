using Microsoft.EntityFrameworkCore;
using NexaLearn.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace NexaLearn.Infrastructure.Tests.Common;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.StopAsync();
        await _postgres.DisposeAsync();
    }

    protected NexaLearnDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NexaLearnDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new NexaLearnDbContext(options);
    }

    protected UnitOfWork CreateUnitOfWork(NexaLearnDbContext context) =>
        new UnitOfWork(context);
}
