using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Workers;

public class TeamLockJobOptions
{
    public const string SectionName = "TeamLockJob";

    /// <summary>
    /// How often the job wakes to check whether tournament-start has passed.
    /// Every 5 minutes is plenty — the lock pass is a one-time event per tournament.
    /// </summary>
    [Range(1, 1440)]
    public int IntervalMinutes { get; set; } = 5;

    [Range(0, 300)]
    public int StartupDelaySeconds { get; set; } = 10;
}
