namespace Tip4Gen.Domain.Tournaments;

public class Match
{
    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public Stage Stage { get; private set; }
    public string? GroupCode { get; private set; }
    public string? RoundLabel { get; private set; }
    public Guid HomeTeamId { get; private set; }
    public Guid AwayTeamId { get; private set; }
    public DateTimeOffset KickoffUtc { get; private set; }
    public MatchStatus Status { get; private set; }
    public int? HomeGoals { get; private set; }
    public int? AwayGoals { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Match() { }

    public Match(
        Guid tournamentId,
        string externalId,
        Stage stage,
        string? groupCode,
        string? roundLabel,
        Guid homeTeamId,
        Guid awayTeamId,
        DateTimeOffset kickoffUtc)
    {
        Id = Guid.NewGuid();
        TournamentId = tournamentId;
        ExternalId = externalId;
        Stage = stage;
        GroupCode = groupCode;
        RoundLabel = roundLabel;
        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        KickoffUtc = kickoffUtc;
        Status = MatchStatus.Scheduled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reschedule(DateTimeOffset kickoffUtc)
    {
        KickoffUtc = kickoffUtc;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(MatchStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetFinalScore(int home, int away)
    {
        HomeGoals = home;
        AwayGoals = away;
        Status = MatchStatus.Finished;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Admin-recorded FIFA outcome for an abandoned/incomplete match (guide §11).
    /// Behaves like <see cref="SetFinalScore"/> but lands the status on Awarded so
    /// downstream views can distinguish FIFA-decided results from regular Finished
    /// matches. Scoring treats Awarded the same as Finished.
    /// </summary>
    public void AwardResult(int home, int away)
    {
        HomeGoals = home;
        AwayGoals = away;
        Status = MatchStatus.Awarded;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearScore()
    {
        HomeGoals = null;
        AwayGoals = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
