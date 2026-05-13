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

        // Money se mapea como owned entity: dos columnas en la misma tabla.
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

        // UsePropertyAccessMode(Field) después de OwnsOne: Course usa constructor sin
        // parámetros para EF Core y Price es { get; } — EF Core debe usar el backing field.
        builder.Navigation(c => c.Price)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(c => c.IsPublished).HasColumnName("is_published");

        // HasMany (no OwnsMany): Module y Lesson son entidades regulares con FK explícita.
        // OwnsMany + private backing fields tiene limitaciones en EF Core change tracking
        // cuando se agregan nuevas entidades a una colección ya trackeada.
        builder.HasMany(c => c.Modules)
            .WithOne()
            .HasForeignKey("course_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Modules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
