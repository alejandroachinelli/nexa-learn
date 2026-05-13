using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexaLearn.Application.Common.Interfaces;
using NexaLearn.Domain.Interfaces;
using NexaLearn.Infrastructure.Persistence;
using NexaLearn.Infrastructure.Persistence.Repositories;

namespace NexaLearn.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NexaLearnDbContext>(options =>
            options.UseNpgsql(configuration["ConnectionStrings:Default"]));

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
