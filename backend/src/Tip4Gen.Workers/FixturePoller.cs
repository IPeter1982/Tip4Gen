using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Persistence;
using Tip4Gen.Infrastructure.Tournaments;

namespace Tip4Gen.Workers;

public class FixturePoller(
    IServiceScopeFactory scopeFactory,
    IOptions<FixturePollerOptions> options,
    ILogger<FixturePoller> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        var pollInterval = TimeSpan.FromMinutes(opts.IntervalMinutes);
        var activeWindow = TimeSpan.FromHours(opts.ActiveWindowHours);
        var lookahead = TimeSpan.FromMinutes(opts.LookaheadMinutes);

        logger.LogInformation(
            "FixturePoller starting: interval={Interval}, activeWindow={Active}, lookahead={Lookahead}",
            pollInterval, activeWindow, lookahead);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(opts.StartupDelaySeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(activeWindow, lookahead, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fixture poll tick failed");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task PollAsync(TimeSpan activeWindow, TimeSpan lookahead, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sync = scope.ServiceProvider.GetRequiredService<IFixtureSyncService>();

        var now = DateTimeOffset.UtcNow;
        var windowStart = now - activeWindow;
        var windowEnd = now + lookahead;

        var hasActiveWork = await db.Matches.AnyAsync(m =>
            m.Status == MatchStatus.Live
            || (m.Status == MatchStatus.Scheduled
                && m.KickoffUtc >= windowStart
                && m.KickoffUtc <= windowEnd),
            ct);

        if (!hasActiveWork)
        {
            logger.LogDebug("FixturePoller: no live or imminent matches — skipping API call");
            return;
        }

        var result = await sync.SyncAsync(includeTeamsRoster: false, ct);
        logger.LogInformation(
            "FixturePoller tick: matches +{Created}/~{Updated}, finalized {Finalized}",
            result.MatchesCreated, result.MatchesUpdated, result.FinalizedMatchIds.Count);
    }
}
