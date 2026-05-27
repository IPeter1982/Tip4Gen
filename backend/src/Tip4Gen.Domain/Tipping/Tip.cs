namespace Tip4Gen.Domain.Tipping;

/// <summary>
/// One tip on one match. Two flavours:
///  • Human tip — UserId set, TeamMemberId null, IsAiFallback false, Reasoning null.
///  • AI tip — UserId null, TeamMemberId set (the AI slot on a Locked team), Reasoning
///    optional, IsAiFallback true iff the auto 1–1 default fired at T-1h.
///
/// Exactly one of (UserId, TeamMemberId) is set, enforced by a CHECK constraint in
/// the EF mapping. Per CLAUDE.md teams gotchas, AI tips key on the team member, not
/// a synthetic user, so they never appear on the individual leaderboard.
/// </summary>
public class Tip
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? TeamMemberId { get; private set; }
    public Guid MatchId { get; private set; }
    public int HomeGoals { get; private set; }
    public int AwayGoals { get; private set; }
    public bool Joker { get; private set; }
    public bool IsAiFallback { get; private set; }
    public string? Reasoning { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Tip() { }

    public Tip(Guid userId, Guid matchId, int homeGoals, int awayGoals, bool joker)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TeamMemberId = null;
        MatchId = matchId;
        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
        Joker = joker;
        IsAiFallback = false;
        Reasoning = null;
        SubmittedAt = DateTimeOffset.UtcNow;
        UpdatedAt = SubmittedAt;
    }

    public static Tip ForAi(
        Guid teamMemberId,
        Guid matchId,
        int homeGoals,
        int awayGoals,
        string? reasoning,
        bool isAiFallback) => new()
        {
            Id = Guid.NewGuid(),
            UserId = null,
            TeamMemberId = teamMemberId,
            MatchId = matchId,
            HomeGoals = homeGoals,
            AwayGoals = awayGoals,
            Joker = false,
            IsAiFallback = isAiFallback,
            Reasoning = string.IsNullOrWhiteSpace(reasoning) ? null : reasoning.Trim(),
            SubmittedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    public void Update(int homeGoals, int awayGoals, bool joker)
    {
        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
        Joker = joker;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
