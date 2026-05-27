using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tip4Gen.Infrastructure.Teams;

namespace Tip4Gen.Workers;

/// <summary>
/// Periodically calls <see cref="ITeamLockService.LockAllAsync"/>. The service is
/// itself idempotent and cheap when there are no Forming teams, so a coarse cadence
/// is fine — we just need to *eventually* lock teams once tournament-start passes.
/// </summary>
public class TeamLockJob(
    IServiceScopeFactory scopeFactory,
    IOptions<TeamLockJobOptions> options,
    ILogger<TeamLockJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        var interval = TimeSpan.FromMinutes(opts.IntervalMinutes);

        logger.LogInformation("TeamLockJob starting: interval={Interval}", interval);

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
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TeamLockJob tick failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var lockService = scope.ServiceProvider.GetRequiredService<ITeamLockService>();
        var summary = await lockService.LockAllAsync(ct);
        if (summary.Locked > 0 || summary.Disqualified > 0)
        {
            logger.LogInformation(
                "TeamLockJob tick: locked={Locked}, disqualified={Disqualified}, skipped={Skipped}",
                summary.Locked, summary.Disqualified, summary.Skipped);
        }
    }
}
