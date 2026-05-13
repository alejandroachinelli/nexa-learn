using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaLearn.Domain.Aggregates.Courses;

namespace NexaLearn.Infrastructure.Persistence.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("modules");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Ignore(m => m.HasLessons);

        builder.HasMany(m => m.Lessons)
            .WithOne()
            .HasForeignKey("module_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Lessons)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
