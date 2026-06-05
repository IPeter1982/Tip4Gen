namespace Tip4Gen.Domain.Tournaments;

public class Tournament
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string ExternalLeagueId { get; private set; } = default!;
    public int Season { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>FIFA-decided winner; populated post-tournament by admin via /api/admin/long-tips/outcomes.</summary>
    public Guid? WinnerTeamId { get; private set; }

    /// <summary>FIFA-decided top scorer; FK to players. Populated post-tournament by admin.</summary>
    public Guid? TopScorerPlayerId { get; private set; }

    private Tournament() { }

    public Tournament(string name, string externalLeagueId, int season, DateTimeOffset startsAtUtc, DateTimeOffset? endsAtUtc = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        ExternalLeagueId = externalLeagueId;
        Season = season;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateSchedule(DateTimeOffset startsAtUtc, DateTimeOffset? endsAtUtc)
    {
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    public void Rename(string name) => Name = name;

    /// <summary>
    /// Set or clear the FIFA-decided outcomes that drive §9 leaderboard tiebreakers.
    /// Either field may be null (admin can record one before the other lands). Editable —
    /// re-calling overwrites both values atomically.
    /// </summary>
    public void RecordOutcomes(Guid? winnerTeamId, Guid? topScorerPlayerId)
    {
        WinnerTeamId = winnerTeamId;
        TopScorerPlayerId = topScorerPlayerId;
    }
}
