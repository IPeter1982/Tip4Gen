namespace Tip4Gen.Domain.Ai;

/// <summary>
/// One AI tipping attempt for a (team_member, match) pair. The orchestrator writes
/// one row per IAiTipper call so the schedule policy can count attempts across job
/// restarts — without this the job would burn quota retrying matches it already
/// tipped after a process restart.
/// </summary>
public class AiTipAttempt
{
    public Guid Id { get; private set; }
    public Guid TeamMemberId { get; private set; }
    public Guid MatchId { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }

    private AiTipAttempt() { }

    public AiTipAttempt(Guid teamMemberId, Guid matchId, bool success, string? errorMessage)
    {
        Id = Guid.NewGuid();
        TeamMemberId = teamMemberId;
        MatchId = matchId;
        AttemptedAt = DateTimeOffset.UtcNow;
        Success = success;
        ErrorMessage = success ? null : (errorMessage ?? "unknown error");
    }
}
