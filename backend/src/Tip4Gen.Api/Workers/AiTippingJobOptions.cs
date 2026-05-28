using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Api.Workers;

public class AiTippingJobOptions
{
    public const string SectionName = "AiTippingJob";

    /// <summary>
    /// How often the job wakes to walk Locked teams' AI members × upcoming matches.
    /// 5 minutes gives the schedule policy fine-enough resolution for the 30-min
    /// attempt + retry + fallback windows without burning OpenAI quota.
    /// </summary>
    [Range(1, 60)]
    public int IntervalMinutes { get; set; } = 5;

    [Range(0, 300)]
    public int StartupDelaySeconds { get; set; } = 15;
}
