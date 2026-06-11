using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace InsuranceClaimSystem.Infrastructure.Data;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddAppDbContex(this IServiceCollection services, string connectionString)
    {
        // ADD THIS LINE — tells Npgsql to treat all DateTime as UTC globally
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}