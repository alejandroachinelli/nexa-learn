using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaLearn.Domain.Aggregates.Enrollments;

namespace NexaLearn.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(e => e.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(e => e.EnrolledAt).HasColumnName("enrolled_at").IsRequired();

        // CompletedLessonIds se persiste como uuid[] en PostgreSQL usando el backing field.
        // EF Core accede directamente al campo privado por nombre.
        builder.Property<List<Guid>>("_completedLessonIds")
            .HasField("_completedLessonIds")
            .HasColumnName("completed_lesson_ids")
            .HasColumnType("uuid[]")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Índice único a nivel de base de datos para prevenir inscripciones duplicadas
        builder.HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique()
            .HasDatabaseName("ix_enrollments_student_course");
    }
}
