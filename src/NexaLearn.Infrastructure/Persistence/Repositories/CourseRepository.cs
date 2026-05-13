using Microsoft.EntityFrameworkCore;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Infrastructure.Persistence.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly NexaLearnDbContext _context;

    public CourseRepository(NexaLearnDbContext context)
    {
        _context = context;
    }

    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Courses
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct)
    {
        return await _context.Courses
            .Where(c => c.IsPublished)
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(Course course, CancellationToken ct)
    {
        await _context.Courses.AddAsync(course, ct);
    }

    public void Update(Course course)
    {
        _context.Courses.Update(course);
    }
}
