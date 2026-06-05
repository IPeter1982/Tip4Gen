namespace Tip4Gen.Domain.Tipping;

public class LongTermTip
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public LongTermTipType Type { get; private set; }
    public Guid? TargetTeamId { get; private set; }
    public Guid? TargetPlayerId { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private LongTermTip() { }

    public static LongTermTip ForWinner(Guid userId, Guid teamId)
    {
        return new LongTermTip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = LongTermTipType.Winner,
            TargetTeamId = teamId,
            TargetPlayerId = null,
            SubmittedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static LongTermTip ForTopScorer(Guid userId, Guid playerId)
    {
        return new LongTermTip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = LongTermTipType.TopScorer,
            TargetTeamId = null,
            TargetPlayerId = playerId,
            SubmittedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateWinner(Guid teamId)
    {
        if (Type != LongTermTipType.Winner)
            throw new InvalidOperationException("Cannot update Winner on a non-Winner tip.");
        TargetTeamId = teamId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateTopScorer(Guid playerId)
    {
        if (Type != LongTermTipType.TopScorer)
            throw new InvalidOperationException("Cannot update TopScorer on a non-TopScorer tip.");
        TargetPlayerId = playerId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
