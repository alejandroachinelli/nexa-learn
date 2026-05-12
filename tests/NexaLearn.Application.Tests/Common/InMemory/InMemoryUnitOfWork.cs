using NexaLearn.Application.Common.Interfaces;

namespace NexaLearn.Application.Tests.Common.InMemory;

public class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
}
