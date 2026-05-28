using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Api.Workers;

public class FixturePollerOptions
{
    public const string SectionName = "FixturePoller";

    [Range(1, 1440)]
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Match is considered "active" for polling purposes if it kicked off within the
    /// last ActiveWindowHours or kicks off within LookaheadMinutes. Outside that window
    /// the poller skips the tick — saves provider quota on idle days.
    /// </summary>
    [Range(1, 24)]
    public int ActiveWindowHours { get; set; } = 4;

    [Range(0, 240)]
    public int LookaheadMinutes { get; set; } = 60;

    [Range(0, 300)]
    public int StartupDelaySeconds { get; set; } = 5;
}
