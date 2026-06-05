using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Infrastructure.Players;

public class WikipediaSquadsProviderOptions
{
    [Required] public string BaseUrl { get; set; } = "https://en.wikipedia.org";
    [Required] public string PageTitle { get; set; } = "2026_FIFA_World_Cup_squads";
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Sent on every request. Wikipedia's ToS requires an identifiable UA;
    /// anonymous browsers get throttled and may be banned without notice.
    /// </summary>
    [Required] public string UserAgent { get; set; } = "Tip4Gen/1.0 (contact: ispanpeter82@gmail.com)";
}
