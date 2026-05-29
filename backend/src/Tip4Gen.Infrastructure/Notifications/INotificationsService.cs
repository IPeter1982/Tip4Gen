namespace Tip4Gen.Infrastructure.Notifications;

public interface INotificationsService
{
    Task<NotificationsRunSummary> RunOnceAsync(CancellationToken ct);
}

public sealed record NotificationsRunSummary(int MatchesInWindow, int EmailsSent, int EmailsFailed);
