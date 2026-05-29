using Microsoft.Extensions.Options;
using Tip4Gen.Infrastructure.Notifications;

namespace Tip4Gen.Api.Workers;

/// <summary>
/// Drives <see cref="INotificationsService.RunOnceAsync"/> on a 10-minute cadence. The
/// service does the policy decisions + sending; the job is just a heartbeat. When
/// <c>Resend:ApiKey</c> is unset the service is effectively a no-op (sender returns
/// Disabled) — we still log so we know the loop is alive.
/// </summary>
public class NotificationsJob(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationsJobOptions> options,
    ILogger<NotificationsJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        var interval = TimeSpan.FromMinutes(opts.IntervalMinutes);

        logger.LogInformation("NotificationsJob starting: interval={Interval}", interval);

        try { await Task.Delay(TimeSpan.FromSeconds(opts.StartupDelaySeconds), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogError(ex, "NotificationsJob tick failed");
            }

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationsService>();
        var summary = await service.RunOnceAsync(ct);
        if (summary.EmailsSent > 0 || summary.EmailsFailed > 0)
        {
            logger.LogInformation(
                "NotificationsJob tick: matches={Matches}, sent={Sent}, failed={Failed}",
                summary.MatchesInWindow, summary.EmailsSent, summary.EmailsFailed);
        }
    }
}
