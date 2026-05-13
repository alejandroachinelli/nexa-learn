using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Infrastructure.Persistence.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.Title).HasColumnName("title").HasMaxLength(200).IsRequired();

        builder.Property(l => l.Duration)
            .HasConversion(
                d => d.Minutes,
                v => Duration.Create(v).Value)
            .HasColumnName("duration_minutes")
            .IsRequired();
    }
}
