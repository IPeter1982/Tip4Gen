using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Ai;
using Tip4Gen.Domain.Football;
using Tip4Gen.Domain.Tournaments.Events;
using Tip4Gen.Infrastructure.Ai;
using Tip4Gen.Infrastructure.Football;
using Tip4Gen.Infrastructure.Leaderboard;
using Tip4Gen.Infrastructure.Persistence;
using Tip4Gen.Infrastructure.Scoring;
using Tip4Gen.Infrastructure.Teams;
using Tip4Gen.Infrastructure.Tipping;
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

        // OpenAI — ApiKey is intentionally optional. When unset, OpenAiTipper short-circuits
        // to AiTipResult.Disabled and the schedule policy falls through to the 1–1 default
        // at T-1h. Drop the key with `dotnet user-secrets set OpenAi:ApiKey sk-…` to enable.
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection("OpenAi"))
            .ValidateDataAnnotations();

        services.AddHttpClient<IAiTipper, OpenAiTipper>((sp, http) =>
            {
                var opts = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
                http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
                http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
                if (!string.IsNullOrWhiteSpace(opts.ApiKey))
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);
            })
            .AddStandardResilienceHandler();

        services.AddScoped<IFixtureSyncService, FixtureSyncService>();
        services.AddScoped<ITipsService, TipsService>();
        services.AddScoped<ILongTermTipsService, LongTermTipsService>();
        services.AddScoped<IMatchScoringService, MatchScoringService>();
        services.AddScoped<IMatchFinalizedHandler, MatchFinalizedScoringHandler>();
        services.AddScoped<ITeamsService, TeamsService>();
        services.AddScoped<ITeamLockService, TeamLockService>();
        services.AddScoped<ITeamAggregationService, TeamAggregationService>();
        services.AddScoped<IIndividualLeaderboardService, IndividualLeaderboardService>();
        services.AddScoped<ITeamLeaderboardService, TeamLeaderboardService>();
        services.AddScoped<IAiTippingService, AiTippingService>();

        return services;
    }
}
