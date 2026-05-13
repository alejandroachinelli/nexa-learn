using NexaLearn.Application.Common.Interfaces;

namespace NexaLearn.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly NexaLearnDbContext _context;

    public UnitOfWork(NexaLearnDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct) =>
        _context.SaveChangesAsync(ct);
}
