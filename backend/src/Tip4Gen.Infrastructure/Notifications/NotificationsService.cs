using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Notifications;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Notifications;

/// <summary>
/// Orchestrates one pass of the tip-reminder cron. For each user with prefs enabled
/// + an email on file, walks the upcoming-match window and asks
/// <see cref="TipReminderPolicy"/> whether to send a T-24h or T-2h reminder, then
/// dispatches via <see cref="INotificationSender"/> and logs the outcome.
///
/// All inputs (users, prefs, tips, sent log) are loaded in a small fixed number of
/// queries per tick; the join happens in memory at WC-scale (200 users × ≤12 matches
/// in the 26h window).
/// </summary>
public class NotificationsService(
    AppDbContext db,
    INotificationSender sender,
    IOptions<NotificationsOptions> options,
    TimeProvider clock,
    ILogger<NotificationsService> logger) : INotificationsService
{
    private static readonly TimeZoneInfo Budapest = ResolveBudapest();

    private static TimeZoneInfo ResolveBudapest()
    {
        // Linux/macOS use IANA; Windows uses the legacy id. .NET 9 normalises but be defensive.
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); }
    }

    public async Task<NotificationsRunSummary> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var windowEnd = now + TipReminderPolicy.T24hWindowStart; // ~25h ahead
        var windowStart = now - TipReminderPolicy.TipDeadlineBeforeKickoff; // include matches whose deadline hasn't fully passed

        var matches = await db.Matches.AsNoTracking()
            .Where(m => m.Status == MatchStatus.Scheduled
                && m.KickoffUtc > windowStart
                && m.KickoffUtc <= windowEnd)
            .Select(m => new MatchInfo(m.Id, m.HomeTeamId, m.AwayTeamId, m.KickoffUtc))
            .ToListAsync(ct);
        if (matches.Count == 0)
            return new NotificationsRunSummary(0, 0, 0);

        var matchIds = matches.Select(m => m.Id).ToList();
        var teamIds = matches.SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId }).Distinct().ToList();
        var teamNames = await db.NationalTeams.AsNoTracking()
            .Where(t => teamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var users = await db.Users.AsNoTracking()
            .Where(u => u.Email != null)
            .Select(u => new UserInfo(u.Id, u.Email!, u.DisplayName))
            .ToListAsync(ct);
        if (users.Count == 0)
            return new NotificationsRunSummary(matches.Count, 0, 0);

        var userIds = users.Select(u => u.Id).ToList();
        var prefsByUser = await db.UserPreferences.AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.EmailRemindersEnabled, ct);

        var tippedPairs = await db.Tips.AsNoTracking()
            .Where(t => t.UserId != null
                && matchIds.Contains(t.MatchId)
                && userIds.Contains(t.UserId!.Value))
            .Select(t => new { UserId = t.UserId!.Value, t.MatchId })
            .ToListAsync(ct);
        var tippedSet = tippedPairs.Select(p => (p.UserId, p.MatchId)).ToHashSet();

        var logRows = await db.NotificationLogs.AsNoTracking()
            .Where(l => l.Success
                && l.MatchId != null
                && matchIds.Contains(l.MatchId!.Value)
                && userIds.Contains(l.UserId))
            .Select(l => new { l.UserId, l.Kind, MatchId = l.MatchId!.Value })
            .ToListAsync(ct);
        var sentSet = logRows.Select(r => (r.UserId, r.Kind, r.MatchId)).ToHashSet();

        var siteBase = options.Value.SiteBaseUrl.TrimEnd('/');
        var prefsUrl = siteBase + "/me";

        int sent = 0, failed = 0;
        foreach (var match in matches)
        {
            if (ct.IsCancellationRequested) break;
            foreach (var user in users)
            {
                if (ct.IsCancellationRequested) break;
                var prefsEnabled = prefsByUser.TryGetValue(user.Id, out var p) ? p : true;
                var hasTip = tippedSet.Contains((user.Id, match.Id));
                var t24Sent = sentSet.Contains((user.Id, NotificationKind.TipReminder24h, match.Id));
                var t2Sent = sentSet.Contains((user.Id, NotificationKind.TipReminder2h, match.Id));

                var decision = TipReminderPolicy.Decide(now, match.KickoffUtc, prefsEnabled, hasTip, t24Sent, t2Sent);
                if (decision == ReminderDecision.None) continue;

                var kind = decision == ReminderDecision.SendT24h
                    ? NotificationKind.TipReminder24h
                    : NotificationKind.TipReminder2h;

                var home = teamNames.GetValueOrDefault(match.HomeTeamId, "?");
                var away = teamNames.GetValueOrDefault(match.AwayTeamId, "?");
                var ctx = BuildContext(user, home, away, match.KickoffUtc, siteBase, prefsUrl, match.Id);
                var (subject, html, text) = NotificationTemplates.RenderTipReminder(kind, ctx);
                var emailMsg = new NotificationEmail(user.Email, user.DisplayName, subject, html, text);

                var result = await sender.SendAsync(emailMsg, ct);
                switch (result)
                {
                    case NotificationSendResult.Disabled:
                        // No log row — keep the dedup ledger meaningful.
                        continue;
                    case NotificationSendResult.Success success:
                        db.NotificationLogs.Add(new NotificationLog(user.Id, kind, match.Id, true, null));
                        // Update in-memory set so a same-tick second pass doesn't re-pick.
                        sentSet.Add((user.Id, kind, match.Id));
                        sent++;
                        logger.LogInformation("Notification sent: {Kind} → {Email} (match {MatchId}, message {ProviderId})",
                            kind, user.Email, match.Id, success.ProviderMessageId);
                        break;
                    case NotificationSendResult.RateLimited rate:
                        db.NotificationLogs.Add(new NotificationLog(user.Id, kind, match.Id, false, rate.Error));
                        failed++;
                        logger.LogWarning("Notification rate-limited: {Kind} → {Email}. Aborting this tick.", kind, user.Email);
                        await SaveIfDirty(ct);
                        return new NotificationsRunSummary(matches.Count, sent, failed);
                    case NotificationSendResult.Failed failedResult:
                        db.NotificationLogs.Add(new NotificationLog(user.Id, kind, match.Id, false, failedResult.Error));
                        failed++;
                        break;
                }
            }

            await SaveIfDirty(ct);
        }

        return new NotificationsRunSummary(matches.Count, sent, failed);
    }

    private async Task SaveIfDirty(CancellationToken ct)
    {
        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }

    private static TipReminderContext BuildContext(
        UserInfo user,
        string homeTeamName,
        string awayTeamName,
        DateTimeOffset kickoffUtc,
        string siteBase,
        string prefsUrl,
        Guid matchId)
    {
        var deadlineUtc = kickoffUtc - TipReminderPolicy.TipDeadlineBeforeKickoff;
        return new TipReminderContext(
            DisplayName: user.DisplayName,
            HomeTeamName: homeTeamName,
            AwayTeamName: awayTeamName,
            KickoffBudapestText: FormatBudapest(kickoffUtc),
            DeadlineBudapestText: FormatBudapest(deadlineUtc),
            TipUrl: $"{siteBase}/matches/{matchId}/tip",
            SiteBaseUrl: siteBase,
            PreferencesUrl: prefsUrl);
    }

    private static string FormatBudapest(DateTimeOffset utc)
    {
        var local = TimeZoneInfo.ConvertTime(utc, Budapest);
        return local.ToString("yyyy.MM.dd. HH:mm", CultureInfo.GetCultureInfo("hu-HU"));
    }

    private readonly record struct MatchInfo(Guid Id, Guid HomeTeamId, Guid AwayTeamId, DateTimeOffset KickoffUtc);
    private readonly record struct UserInfo(Guid Id, string Email, string DisplayName);
}
