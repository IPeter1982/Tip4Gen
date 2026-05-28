using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Ai;
using Tip4Gen.Domain.Football;
using Tip4Gen.Domain.Tournaments.Events;
using Tip4Gen.Infrastructure.Admin;
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
            ?? BuildConnectionStringFromDatabaseUrl(Environment.GetEnvironmentVariable("DATABASE_URL"))
            ?? throw new InvalidOperationException(
                "Neither ConnectionStrings:AppDb nor DATABASE_URL is configured");

        services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(connectionString));

        // Explicit registration so a previous AddSingleton from the host (or its absence)
        // is overridden uniformly. AiTippingService injects TimeProvider.
        services.AddSingleton(TimeProvider.System);

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
        services.AddScoped<IAdminAuditWriter, AdminAuditWriter>();
        services.AddScoped<IMatchAdminService, MatchAdminService>();
        services.AddScoped<ILongTipOutcomesService, LongTipOutcomesService>();

        return services;
    }

    /// <summary>
    /// Railway Postgres exposes DATABASE_URL in URI form
    /// (postgres://user:pass@host:port/dbname). Npgsql wants keyword/value form,
    /// so we translate at startup. Returns null when the input is null/empty so
    /// the caller can fall through to its own missing-config exception.
    /// </summary>
    internal static string? BuildConnectionStringFromDatabaseUrl(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
            return null;

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.IsDefaultPort ? 5432 : uri.Port;

        return $"Host={uri.Host};Port={port};Username={username};Password={password};Database={database};SSL Mode=Require;Trust Server Certificate=true";
    }
}
