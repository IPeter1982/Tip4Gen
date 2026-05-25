namespace Tip4Gen.Domain.Tipping;

public class Tip
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid MatchId { get; private set; }
    public int HomeGoals { get; private set; }
    public int AwayGoals { get; private set; }
    public bool Joker { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Tip() { }

    public Tip(Guid userId, Guid matchId, int homeGoals, int awayGoals, bool joker)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        MatchId = matchId;
        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
        Joker = joker;
        SubmittedAt = DateTimeOffset.UtcNow;
        UpdatedAt = SubmittedAt;
    }

    public void Update(int homeGoals, int awayGoals, bool joker)
    {
        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
        Joker = joker;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
