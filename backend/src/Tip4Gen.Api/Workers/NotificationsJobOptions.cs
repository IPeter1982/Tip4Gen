using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Api.Workers;

public class NotificationsJobOptions
{
    public const string SectionName = "NotificationsJob";

    /// <summary>
    /// How often to walk upcoming matches and dispatch due reminders. 10 minutes is
    /// well inside the policy's 2-hour windows (T-25..T-23 and T-3..T-1) so we never
    /// miss a window even with one missed tick.
    /// </summary>
    [Range(1, 60)]
    public int IntervalMinutes { get; set; } = 10;

    [Range(0, 600)]
    public int StartupDelaySeconds { get; set; } = 30;
}
