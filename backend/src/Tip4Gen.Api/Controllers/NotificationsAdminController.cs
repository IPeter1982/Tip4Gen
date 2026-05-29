using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Notifications;
using Tip4Gen.Infrastructure.Notifications;

namespace Tip4Gen.Api.Controllers;

public record NotificationsPreviewRequest(
    string ToEmail,
    string DisplayName,
    NotificationKind Kind,
    string HomeTeam,
    string AwayTeam);

public record NotificationsPreviewResponse(
    string Outcome,
    string? ProviderMessageId,
    string? Error);

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class NotificationsAdminController(
    INotificationSender sender,
    IOptions<NotificationsOptions> notifOptions) : ControllerBase
{
    /// <summary>
    /// One-shot preview: render a tip-reminder template against synthetic inputs and
    /// dispatch through the configured sender. Useful for verifying the Resend ApiKey,
    /// from-address, and template formatting before the cron starts spamming real users.
    /// Bypasses the dedup ledger entirely (no notification_log row is written).
    /// </summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] NotificationsPreviewRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: "toEmail is required.");
        }

        var siteBase = notifOptions.Value.SiteBaseUrl.TrimEnd('/');
        var matchId = Guid.NewGuid();
        var kickoff = DateTimeOffset.UtcNow + TimeSpan.FromHours(24);
        var deadline = kickoff - TipReminderPolicy.TipDeadlineBeforeKickoff;
        var ctx = new TipReminderContext(
            DisplayName: string.IsNullOrWhiteSpace(request.DisplayName) ? "Játékos" : request.DisplayName,
            HomeTeamName: request.HomeTeam,
            AwayTeamName: request.AwayTeam,
            KickoffBudapestText: FormatBudapest(kickoff),
            DeadlineBudapestText: FormatBudapest(deadline),
            TipUrl: $"{siteBase}/matches/{matchId}/tip",
            SiteBaseUrl: siteBase,
            PreferencesUrl: $"{siteBase}/me");

        var (subject, html, text) = NotificationTemplates.RenderTipReminder(request.Kind, ctx);
        var email = new NotificationEmail(request.ToEmail, ctx.DisplayName, subject, html, text);

        var result = await sender.SendAsync(email, ct);
        return Ok(result switch
        {
            NotificationSendResult.Success s => new NotificationsPreviewResponse("Success", s.ProviderMessageId, null),
            NotificationSendResult.Disabled => new NotificationsPreviewResponse("Disabled", null, "Resend:ApiKey is not configured"),
            NotificationSendResult.RateLimited r => new NotificationsPreviewResponse("RateLimited", null, r.Error),
            NotificationSendResult.Failed f => new NotificationsPreviewResponse("Failed", null, f.Error),
            _ => new NotificationsPreviewResponse(result.GetType().Name, null, "unknown result"),
        });
    }

    private static string FormatBudapest(DateTimeOffset utc)
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest"); }
        catch (TimeZoneNotFoundException) { tz = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); }
        var local = TimeZoneInfo.ConvertTime(utc, tz);
        return local.ToString("yyyy.MM.dd. HH:mm");
    }
}
