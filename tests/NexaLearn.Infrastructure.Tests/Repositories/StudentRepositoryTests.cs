using FluentAssertions;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.ValueObjects;
using NexaLearn.Infrastructure.Persistence.Repositories;
using NexaLearn.Infrastructure.Tests.Common;

namespace NexaLearn.Infrastructure.Tests.Repositories;

public class StudentRepositoryTests : IntegrationTestBase
{
    // --- Helpers ---

    private static Student CreateStudent(string email = "alejandro@example.com", string name = "Alejandro Martin")
    {
        return Student.Create(
            Guid.NewGuid(),
            Email.Create(email).Value,
            name
        ).Value;
    }

    // --- Tests ---

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsStudent()
    {
        var student = CreateStudent();

        await using var context = CreateDbContext();
        var repo = new StudentRepository(context);
        await repo.AddAsync(student, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new StudentRepository(readContext);
        var result = await readRepo.GetByIdAsync(student.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(student.Id);
        result.Name.Should().Be("Alejandro Martin");
        result.Email.Value.Should().Be("alejandro@example.com");
    }

    [Fact]
    public async Task AddAsync_ThenGetByEmail_ReturnsStudent()
    {
        var student = CreateStudent();

        await using var context = CreateDbContext();
        var repo = new StudentRepository(context);
        await repo.AddAsync(student, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new StudentRepository(readContext);
        var result = await readRepo.GetByEmailAsync(student.Email, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(student.Id);
    }

    [Fact]
    public async Task GetByEmail_CaseInsensitive_ReturnsStudent()
    {
        // Email.Create normaliza a minúsculas — guardar con input en mayúsculas
        // y buscar con otro casing produce el mismo resultado gracias al ValueConverter
        var student = CreateStudent("alejandro@example.com");

        await using var context = CreateDbContext();
        var repo = new StudentRepository(context);
        await repo.AddAsync(student, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        var searchEmail = Email.Create("ALEJANDRO@EXAMPLE.COM").Value; // normaliza a lowercase

        await using var readContext = CreateDbContext();
        var readRepo = new StudentRepository(readContext);
        var result = await readRepo.GetByEmailAsync(searchEmail, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(student.Id);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        await using var context = CreateDbContext();
        var repo = new StudentRepository(context);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }
}
