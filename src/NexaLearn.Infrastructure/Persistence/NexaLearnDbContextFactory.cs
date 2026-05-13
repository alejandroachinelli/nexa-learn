using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NexaLearn.Infrastructure.Persistence;

public class NexaLearnDbContextFactory : IDesignTimeDbContextFactory<NexaLearnDbContext>
{
    public NexaLearnDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NexaLearnDbContext>()
            .UseNpgsql("Host=localhost;Database=nexalearn;Username=nexalearn;Password=nexalearn_dev")
            .Options;

        return new NexaLearnDbContext(options);
    }
}
