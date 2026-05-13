using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.Title)
            .HasConversion(
                t => t.Value,
                v => CourseTitle.Create(v).Value)
            .HasColumnName("title")
            .HasMaxLength(CourseTitle.MaxLength)
            .IsRequired();

        // Money se mapea como owned entity: dos columnas en la misma tabla
        builder.OwnsOne(c => c.Price, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("price")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            money.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(c => c.IsPublished).HasColumnName("is_published");

        // Module y Lesson se configuran como owned many-to-one en tablas separadas.
        // OwnsMany es correcto porque Module no tiene identidad fuera de Course.
        builder.OwnsMany(c => c.Modules, moduleBuilder =>
        {
            moduleBuilder.ToTable("modules");
            moduleBuilder.WithOwner().HasForeignKey("course_id");
            moduleBuilder.HasKey(m => m.Id);
            moduleBuilder.Property(m => m.Id).HasColumnName("id");
            moduleBuilder.Property(m => m.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            moduleBuilder.Ignore(m => m.HasLessons);

            moduleBuilder.OwnsMany(m => m.Lessons, lessonBuilder =>
            {
                lessonBuilder.ToTable("lessons");
                lessonBuilder.WithOwner().HasForeignKey("module_id");
                lessonBuilder.HasKey(l => l.Id);
                lessonBuilder.Property(l => l.Id).HasColumnName("id");
                lessonBuilder.Property(l => l.Title).HasColumnName("title").HasMaxLength(200).IsRequired();

                // Duration se guarda como int (minutos) — guardamos el dato primitivo, no el VO
                lessonBuilder.Property(l => l.Duration)
                    .HasConversion(
                        d => d.Minutes,
                        v => Duration.Create(v).Value)
                    .HasColumnName("duration_minutes")
                    .IsRequired();

                lessonBuilder.Navigation(l => l.Duration)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            moduleBuilder.Navigation(m => m.Lessons)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(c => c.Modules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
