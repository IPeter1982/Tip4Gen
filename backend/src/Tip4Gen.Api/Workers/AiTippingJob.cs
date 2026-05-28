using Microsoft.Extensions.Options;
using Tip4Gen.Infrastructure.Ai;

namespace Tip4Gen.Api.Workers;

/// <summary>
/// Drives <see cref="IAiTippingService.RunOnceAsync"/> on a coarse cadence. The service
/// itself filters down to the right (member, match) pairs and uses the schedule policy
/// to decide what to do — the job is just a heartbeat. Active-window gating lives in
/// the service (it short-circuits when there are no AI members or no upcoming matches).
/// </summary>
public class AiTippingJob(
    IServiceScopeFactory scopeFactory,
    IOptions<AiTippingJobOptions> options,
    ILogger<AiTippingJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        var interval = TimeSpan.FromMinutes(opts.IntervalMinutes);

        logger.LogInformation("AiTippingJob starting: interval={Interval}", interval);

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
                logger.LogError(ex, "AiTippingJob tick failed");
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
        var service = scope.ServiceProvider.GetRequiredService<IAiTippingService>();
        var summary = await service.RunOnceAsync(ct);
        if (summary.AttemptsMade > 0 || summary.TipsWritten > 0 || summary.FallbacksWritten > 0)
        {
            logger.LogInformation(
                "AiTippingJob tick: attempts={Attempts}, written={Written}, fallbacks={Fallbacks}, skipped={Skipped}",
                summary.AttemptsMade, summary.TipsWritten, summary.FallbacksWritten, summary.Skipped);
        }
    }
}
