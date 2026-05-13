using Microsoft.EntityFrameworkCore;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.Interfaces;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Infrastructure.Persistence.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly NexaLearnDbContext _context;

    public StudentRepository(NexaLearnDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Student?> GetByEmailAsync(Email email, CancellationToken ct)
    {
        // El ValueConverter configurado en StudentConfiguration traduce automáticamente
        // la comparación Email → string para que EF Core genere el WHERE correcto
        return await _context.Students
            .FirstOrDefaultAsync(s => s.Email == email, ct);
    }

    public async Task AddAsync(Student student, CancellationToken ct)
    {
        await _context.Students.AddAsync(student, ct);
    }
}
