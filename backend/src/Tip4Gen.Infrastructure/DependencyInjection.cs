using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AppDb")
            ?? throw new InvalidOperationException("ConnectionStrings:AppDb is not configured");

        services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(connectionString));

        return services;
    }
}
