using FluentAssertions;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Infrastructure.Persistence.Repositories;
using NexaLearn.Infrastructure.Tests.Common;

namespace NexaLearn.Infrastructure.Tests.Repositories;

public class EnrollmentRepositoryTests : IntegrationTestBase
{
    // --- Helpers ---

    private static Enrollment CreateEnrollment(Guid? studentId = null, Guid? courseId = null)
    {
        return Enrollment.Create(
            Guid.NewGuid(),
            studentId ?? Guid.NewGuid(),
            courseId ?? Guid.NewGuid(),
            courseIsPublished: true
        ).Value;
    }

    // --- Tests ---

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsEnrollment()
    {
        var enrollment = CreateEnrollment();

        await using var context = CreateDbContext();
        var repo = new EnrollmentRepository(context);
        await repo.AddAsync(enrollment, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new EnrollmentRepository(readContext);
        var result = await readRepo.GetByIdAsync(enrollment.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(enrollment.Id);
        result.StudentId.Should().Be(enrollment.StudentId);
        result.CourseId.Should().Be(enrollment.CourseId);
    }

    [Fact]
    public async Task AddAsync_ThenGetByStudentAndCourse_ReturnsEnrollment()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var enrollment = CreateEnrollment(studentId, courseId);

        await using var context = CreateDbContext();
        var repo = new EnrollmentRepository(context);
        await repo.AddAsync(enrollment, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new EnrollmentRepository(readContext);
        var result = await readRepo.GetByStudentAndCourseAsync(studentId, courseId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(enrollment.Id);
    }

    [Fact]
    public async Task GetByStudentAndCourse_WrongIds_ReturnsNull()
    {
        var enrollment = CreateEnrollment();

        await using var context = CreateDbContext();
        var repo = new EnrollmentRepository(context);
        await repo.AddAsync(enrollment, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new EnrollmentRepository(readContext);
        var result = await readRepo.GetByStudentAndCourseAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ThenGetByStudent_ReturnsAllEnrollments()
    {
        var studentId = Guid.NewGuid();
        var enrollment1 = CreateEnrollment(studentId);
        var enrollment2 = CreateEnrollment(studentId);
        var otherEnrollment = CreateEnrollment(); // otro estudiante

        await using var context = CreateDbContext();
        var repo = new EnrollmentRepository(context);
        await repo.AddAsync(enrollment1, CancellationToken.None);
        await repo.AddAsync(enrollment2, CancellationToken.None);
        await repo.AddAsync(otherEnrollment, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new EnrollmentRepository(readContext);
        var results = await readRepo.GetByStudentAsync(studentId, CancellationToken.None);

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(e => e.StudentId.Should().Be(studentId));
    }

    [Fact]
    public async Task CompletedLessonIds_PersistedAndRetrievedCorrectly()
    {
        var lessonId = Guid.NewGuid();
        var enrollment = CreateEnrollment();
        enrollment.CompleteLesson(lessonId, lessonBelongsToCourse: true);

        await using var context = CreateDbContext();
        var repo = new EnrollmentRepository(context);
        await repo.AddAsync(enrollment, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new EnrollmentRepository(readContext);
        var result = await readRepo.GetByIdAsync(enrollment.Id, CancellationToken.None);

        result!.CompletedLessonIds.Should().HaveCount(1);
        result.CompletedLessonIds.Should().Contain(lessonId);
        result.HasCompletedLesson(lessonId).Should().BeTrue();
    }
}
