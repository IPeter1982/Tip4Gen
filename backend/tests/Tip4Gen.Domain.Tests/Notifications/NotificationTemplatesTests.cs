using Tip4Gen.Domain.Notifications;

namespace Tip4Gen.Domain.Tests.Notifications;

public class NotificationTemplatesTests
{
    private static TipReminderContext SampleContext() => new(
        DisplayName: "Tóth Anna",
        HomeTeamName: "Magyarország",
        AwayTeamName: "Németország",
        KickoffBudapestText: "2026.06.12. 20:00",
        DeadlineBudapestText: "2026.06.12. 19:00",
        TipUrl: "https://tip4gen.example/matches/abc/tip",
        SiteBaseUrl: "https://tip4gen.example",
        PreferencesUrl: "https://tip4gen.example/me");

    [Fact]
    public void T24h_subject_says_holnap()
    {
        var (subject, _, _) = NotificationTemplates.RenderTipReminder(
            NotificationKind.TipReminder24h, SampleContext());
        Assert.Contains("Holnap", subject);
        Assert.Contains("Magyarország", subject);
        Assert.Contains("Németország", subject);
    }

    [Fact]
    public void T2h_subject_says_utolso()
    {
        var (subject, _, _) = NotificationTemplates.RenderTipReminder(
            NotificationKind.TipReminder2h, SampleContext());
        Assert.Contains("Utolsó", subject);
        Assert.Contains("Magyarország", subject);
    }

    [Fact]
    public void Text_body_includes_tip_url_and_unsubscribe_url()
    {
        var (_, _, text) = NotificationTemplates.RenderTipReminder(
            NotificationKind.TipReminder24h, SampleContext());
        Assert.Contains("https://tip4gen.example/matches/abc/tip", text);
        Assert.Contains("https://tip4gen.example/me", text);
    }

    [Fact]
    public void Html_body_encodes_unsafe_input()
    {
        var ctx = SampleContext() with { DisplayName = "<script>alert(1)</script>" };
        var (_, html, _) = NotificationTemplates.RenderTipReminder(
            NotificationKind.TipReminder24h, ctx);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Html_body_contains_cta_link()
    {
        var (_, html, _) = NotificationTemplates.RenderTipReminder(
            NotificationKind.TipReminder24h, SampleContext());
        Assert.Contains("https://tip4gen.example/matches/abc/tip", html);
        Assert.Contains("Tipp leadása", html);
    }

    [Fact]
    public void Html_body_contains_deadline_in_footer()
    {
        var (_, html, _) = NotificationTemplates.RenderTipReminder(
            NotificationKind.TipReminder2h, SampleContext());
        Assert.Contains("2026.06.12. 19:00", html);
    }

    [Fact]
    public void Unsupported_kind_throws()
    {
        // Defensive: any new NotificationKind without a template should explode loudly
        // rather than silently send a blank email.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NotificationTemplates.RenderTipReminder((NotificationKind)999, SampleContext()));
    }
}
