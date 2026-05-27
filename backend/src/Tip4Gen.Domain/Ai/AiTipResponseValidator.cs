namespace Tip4Gen.Domain.Ai;

public abstract record AiTipValidationResult
{
    public sealed record Valid(AiTipResponse Response) : AiTipValidationResult;
    public sealed record Invalid(string Error) : AiTipValidationResult;

    public static AiTipValidationResult Ok(AiTipResponse response) => new Valid(response);
    public static AiTipValidationResult Fail(string error) => new Invalid(error);
}

/// <summary>
/// Validates the raw response from an AI tipper before it's persisted. Goals must be
/// integers in 0–15 (same as guide §3 caps); reasoning is required and capped at 500
/// chars so a misbehaving model can't blow up the DB or the UI.
/// </summary>
public static class AiTipResponseValidator
{
    public const int MaxGoals = 15;
    public const int MaxReasoningLength = 500;

    public static AiTipValidationResult Validate(int? homeGoals, int? awayGoals, string? reasoning)
    {
        if (homeGoals is not int h || h < 0 || h > MaxGoals)
            return AiTipValidationResult.Fail("home_goals must be an integer in 0–15");
        if (awayGoals is not int a || a < 0 || a > MaxGoals)
            return AiTipValidationResult.Fail("away_goals must be an integer in 0–15");
        var trimmed = (reasoning ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return AiTipValidationResult.Fail("reasoning is required");
        if (trimmed.Length > MaxReasoningLength)
            return AiTipValidationResult.Fail($"reasoning exceeds {MaxReasoningLength} characters");
        return AiTipValidationResult.Ok(new AiTipResponse(h, a, trimmed));
    }
}
