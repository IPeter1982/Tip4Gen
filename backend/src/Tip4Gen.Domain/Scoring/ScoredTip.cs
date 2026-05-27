namespace Tip4Gen.Domain.Scoring;

/// <summary>
/// Persisted scoring outcome for a single tip. One row per tip; re-scoring overwrites
/// the existing row (the scoring service deletes-then-reinserts within a transaction).
///
/// UserId is denormalized so leaderboard queries don't need to JOIN tips → matches.
/// </summary>
public class ScoredTip
{
    public Guid Id { get; private set; }
    public Guid TipId { get; private set; }
    public Guid MatchId { get; private set; }
    public Guid UserId { get; private set; }
    public ScoreCategory Category { get; private set; }
    public int BasePoints { get; private set; }
    public decimal Multiplier { get; private set; }
    public bool JokerApplied { get; private set; }
    public int FinalPoints { get; private set; }
    public DateTimeOffset ScoredAt { get; private set; }

    private ScoredTip() { }

    public ScoredTip(Guid tipId, Guid matchId, Guid userId, ScoringResult result)
    {
        Id = Guid.NewGuid();
        TipId = tipId;
        MatchId = matchId;
        UserId = userId;
        Category = result.Category;
        BasePoints = result.BasePoints;
        Multiplier = result.Multiplier;
        JokerApplied = result.JokerApplied;
        FinalPoints = result.FinalPoints;
        ScoredAt = DateTimeOffset.UtcNow;
    }
}
