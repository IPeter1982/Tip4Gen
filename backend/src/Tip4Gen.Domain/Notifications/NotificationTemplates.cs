using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Tip4Gen.Domain.Notifications;

/// <summary>
/// Pure Hungarian email renderer. Subject + HTML + plain-text body for each
/// <see cref="NotificationKind"/>. User-controlled strings (team names, display names)
/// pass through <see cref="Encoder"/> before interpolation so a future malicious admin
/// or claim value can't inject markup. The encoder is configured with all Unicode
/// ranges allowed so Hungarian letters (á, ő, ű, …) stay readable in the rendered
/// HTML — only the five dangerous chars (&lt;, &gt;, &amp;, &quot;, &apos;) get escaped.
/// </summary>
public static class NotificationTemplates
{
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    private static string E(string raw) => Encoder.Encode(raw);

    public static (string Subject, string Html, string Text) RenderTipReminder(
        NotificationKind kind,
        TipReminderContext ctx)
    {
        return kind switch
        {
            NotificationKind.TipReminder24h => RenderT24h(ctx),
            NotificationKind.TipReminder2h => RenderT2h(ctx),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported reminder kind."),
        };
    }

    private static (string, string, string) RenderT24h(TipReminderContext ctx)
    {
        var matchLabel = $"{ctx.HomeTeamName} – {ctx.AwayTeamName}";
        var subject = $"Holnap: {matchLabel} — tippelj!";

        var text = new StringBuilder()
            .AppendLine($"Szia {ctx.DisplayName}!")
            .AppendLine()
            .AppendLine($"Holnap mérkőzés: {matchLabel}")
            .AppendLine($"Kezdés: {ctx.KickoffBudapestText}")
            .AppendLine($"Tipp határidő: {ctx.DeadlineBudapestText}")
            .AppendLine()
            .AppendLine("Add le a tipped:")
            .AppendLine(ctx.TipUrl)
            .AppendLine()
            .AppendLine("Ezeket az emaileket a profil oldalon ki tudod kapcsolni:")
            .AppendLine(ctx.PreferencesUrl)
            .ToString();

        var html = RenderHtmlShell(
            heading: "Tippelj a holnapi meccsre",
            ctx: ctx,
            matchLabel: matchLabel,
            ctaLabel: "Tipp leadása",
            footerNote: $"Még egy nap van hátra. Tipp határidő: {E(ctx.DeadlineBudapestText)}.");

        return (subject, html, text);
    }

    private static (string, string, string) RenderT2h(TipReminderContext ctx)
    {
        var matchLabel = $"{ctx.HomeTeamName} – {ctx.AwayTeamName}";
        var subject = $"Utolsó hívás: {matchLabel}";

        var text = new StringBuilder()
            .AppendLine($"Szia {ctx.DisplayName}!")
            .AppendLine()
            .AppendLine($"Hamarosan kezdődik: {matchLabel}")
            .AppendLine($"Kezdés: {ctx.KickoffBudapestText}")
            .AppendLine($"Tipp határidő: {ctx.DeadlineBudapestText}")
            .AppendLine()
            .AppendLine("Még nem tippeltél — most jó alkalom:")
            .AppendLine(ctx.TipUrl)
            .AppendLine()
            .AppendLine("Ezeket az emaileket a profil oldalon ki tudod kapcsolni:")
            .AppendLine(ctx.PreferencesUrl)
            .ToString();

        var html = RenderHtmlShell(
            heading: "Utolsó hívás",
            ctx: ctx,
            matchLabel: matchLabel,
            ctaLabel: "Tipp leadása",
            footerNote: $"A tipp határideje hamarosan lejár: {E(ctx.DeadlineBudapestText)}.");

        return (subject, html, text);
    }

    /// <summary>
    /// Shared HTML scaffold — table-based layout for email-client compatibility.
    /// All caller-supplied strings are HTML-encoded.
    /// </summary>
    private static string RenderHtmlShell(
        string heading,
        TipReminderContext ctx,
        string matchLabel,
        string ctaLabel,
        string footerNote)
    {
        var displayName = E(ctx.DisplayName);
        var match = E(matchLabel);
        var kickoff = E(ctx.KickoffBudapestText);
        var tipUrl = E(ctx.TipUrl);
        var prefsUrl = E(ctx.PreferencesUrl);
        var siteUrl = E(ctx.SiteBaseUrl);

        return $"""
            <!doctype html>
            <html lang="hu">
            <head>
              <meta charset="utf-8">
              <title>{E(heading)}</title>
            </head>
            <body style="margin:0;padding:0;background:#f5f5f4;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#1c1917;">
              <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background:#f5f5f4;padding:24px 0;">
                <tr><td align="center">
                  <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="560" style="max-width:560px;background:#ffffff;border:2px solid #1c1917;">
                    <tr>
                      <td style="padding:20px 24px;border-bottom:2px solid #1c1917;">
                        <div style="font-family:monospace;font-size:11px;letter-spacing:0.2em;text-transform:uppercase;color:#ea580c;">Tip4Gen · Foci VB</div>
                        <h1 style="margin:8px 0 0;font-size:22px;letter-spacing:-0.02em;text-transform:uppercase;">{E(heading)}</h1>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:24px;">
                        <p style="margin:0 0 16px;font-size:15px;">Szia <strong>{displayName}</strong>!</p>
                        <p style="margin:0 0 8px;font-family:monospace;font-size:11px;letter-spacing:0.15em;text-transform:uppercase;color:#78716c;">Mérkőzés</p>
                        <p style="margin:0 0 16px;font-size:20px;font-weight:bold;">{match}</p>
                        <p style="margin:0 0 16px;font-family:monospace;font-size:13px;color:#44403c;">Kezdés: <strong>{kickoff}</strong></p>
                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin:24px 0;">
                          <tr><td>
                            <a href="{tipUrl}" style="display:inline-block;background:#1c1917;color:#ffffff;font-family:monospace;font-size:13px;font-weight:bold;letter-spacing:0.15em;text-transform:uppercase;padding:14px 22px;border:2px solid #1c1917;text-decoration:none;">{E(ctaLabel)}</a>
                          </td></tr>
                        </table>
                        <p style="margin:0;font-family:monospace;font-size:12px;color:#78716c;">{footerNote}</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:16px 24px;border-top:1px solid #e7e5e4;font-family:monospace;font-size:11px;color:#a8a29e;">
                        <p style="margin:0 0 6px;">Ezt az emailt automatikusan küldtük tipp-emlékeztetőként.</p>
                        <p style="margin:0;"><a href="{prefsUrl}" style="color:#78716c;text-decoration:underline;">Emlékeztetők kikapcsolása</a> · <a href="{siteUrl}" style="color:#78716c;text-decoration:underline;">Tip4Gen</a></p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
