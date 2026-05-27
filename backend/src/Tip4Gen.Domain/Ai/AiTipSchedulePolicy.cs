namespace Tip4Gen.Domain.Ai;

public enum AiTipDecision
{
    /// <summary>Do nothing this tick (either tip already exists, too early, or attempt quota spent for this window).</summary>
    Skip = 0,

    /// <summary>Call the AI provider; persist on success or record the attempt on failure.</summary>
    AttemptAi = 1,

    /// <summary>Write the deterministic 1–1 fallback tip with is_ai_fallback = true.</summary>
    WriteFallback = 2,

    /// <summary>Kickoff has already passed; abandon — no AI tip is worth recording.</summary>
    DeadlinePassed = 3,
}

/// <summary>
/// Decides what the AI tipping job should do for a single (team_member, match) pair on
/// a given tick. Pure — the job is responsible for gathering inputs and persisting the
/// chosen outcome.
///
/// Windows (per Phase 6 spec):
/// • [T-2h, T-90min) — first attempt
/// • [T-90min, T-1h) — second attempt if the first failed
/// • [T-1h, T)        — fallback to 1–1
/// • [T, ∞)           — abandon
/// </summary>
public static class AiTipSchedulePolicy
{
    public static readonly TimeSpan AttemptWindow = TimeSpan.FromHours(2);
    public static readonly TimeSpan RetryWindow = TimeSpan.FromMinutes(90);
    public static readonly TimeSpan FallbackWindow = TimeSpan.FromHours(1);

    public const int MaxAttempts = 2;

    public static AiTipDecision Decide(
        DateTimeOffset now,
        DateTimeOffset kickoffUtc,
        bool tipExists,
        int previousAttempts)
    {
        if (tipExists) return AiTipDecision.Skip;
        if (now >= kickoffUtc) return AiTipDecision.DeadlinePassed;

        var fallbackStart = kickoffUtc - FallbackWindow;
        if (now >= fallbackStart) return AiTipDecision.WriteFallback;

        var retryStart = kickoffUtc - RetryWindow;
        if (now >= retryStart)
            return previousAttempts < MaxAttempts ? AiTipDecision.AttemptAi : AiTipDecision.Skip;

        var attemptStart = kickoffUtc - AttemptWindow;
        if (now >= attemptStart)
            return previousAttempts < 1 ? AiTipDecision.AttemptAi : AiTipDecision.Skip;

        return AiTipDecision.Skip;
    }
}
