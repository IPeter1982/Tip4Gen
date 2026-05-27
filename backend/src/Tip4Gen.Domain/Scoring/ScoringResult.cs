namespace Tip4Gen.Domain.Scoring;

/// <summary>
/// Outcome of scoring a single tip. Final = round(BasePoints * Multiplier) * (JokerApplied ? 2 : 1)
/// — joker doubles after the multiplier per guide §6.
/// </summary>
public readonly record struct ScoringResult(
    ScoreCategory Category,
    int BasePoints,
    decimal Multiplier,
    bool JokerApplied,
    int FinalPoints);
