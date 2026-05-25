using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Football;
using Tip4Gen.Infrastructure.Football;
using Tip4Gen.Infrastructure.Persistence;
using Tip4Gen.Infrastructure.Tournaments;

namespace Tip4Gen.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AppDb")
            ?? throw new InvalidOperationException("ConnectionStrings:AppDb is not configured");

        services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(connectionString));

        services.AddOptions<ApiFootballOptions>()
            .Bind(configuration.GetSection("FootballApi"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IFootballDataProvider, ApiFootballProvider>((sp, http) =>
            {
                var opts = sp.GetRequiredService<IOptions<ApiFootballOptions>>().Value;
                http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
                http.DefaultRequestHeaders.Add("x-apisports-key", opts.ApiKey);
            })
            .AddStandardResilienceHandler();

        services.AddScoped<IFixtureSyncService, FixtureSyncService>();

        return services;
    }
}
