using Microsoft.EntityFrameworkCore;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Infrastructure.Persistence.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly NexaLearnDbContext _context;

    public EnrollmentRepository(NexaLearnDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<Enrollment?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken ct)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId, ct);
    }

    public async Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        await _context.Enrollments.AddAsync(enrollment, ct);
    }

    public void Update(Enrollment enrollment)
    {
        _context.Enrollments.Update(enrollment);
    }
}
