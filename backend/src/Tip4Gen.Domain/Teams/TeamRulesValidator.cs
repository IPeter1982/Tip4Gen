namespace Tip4Gen.Domain.Teams;

public enum TeamRejectionReason
{
    None = 0,
    NameBlank,
    NameTooLong,
    TournamentStarted,
    TeamLocked,
    TeamFull,
    AiSlotTaken,
    UserAlreadyInTeam,
}

public readonly record struct TeamValidationResult(bool IsValid, TeamRejectionReason Reason, string? Message)
{
    public static TeamValidationResult Ok() => new(true, TeamRejectionReason.None, null);
    public static TeamValidationResult Fail(TeamRejectionReason reason, string message) => new(false, reason, message);
}

/// <summary>
/// Pure rule checks for team mutations. Each method takes the relevant slice of state
/// and returns a result with a Hungarian message. DB queries happen in the service —
/// validators here only encode the rule precedence.
/// </summary>
public static class TeamRulesValidator
{
    public static TeamValidationResult ValidateName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return TeamValidationResult.Fail(TeamRejectionReason.NameBlank, "A csapatnévnek nem szabad üresnek lennie.");
        if (trimmed.Length > Team.MaxNameLength)
            return TeamValidationResult.Fail(
                TeamRejectionReason.NameTooLong,
                $"A csapatnév maximum {Team.MaxNameLength} karakter lehet.");
        return TeamValidationResult.Ok();
    }

    /// <summary>
    /// Can the team be mutated *at all* right now? Rule precedence: tournament-start lock
    /// beats team-status lock so the error message points at the real cause.
    /// </summary>
    public static TeamValidationResult ValidateMutable(
        DateTimeOffset now,
        DateTimeOffset? tournamentStartUtc,
        TeamStatus teamStatus)
    {
        if (tournamentStartUtc is { } start && now >= start)
            return TeamValidationResult.Fail(
                TeamRejectionReason.TournamentStarted,
                "A torna már elkezdődött; a csapat-beállítások véglegesek.");
        if (teamStatus != TeamStatus.Forming)
            return TeamValidationResult.Fail(
                TeamRejectionReason.TeamLocked,
                "A csapat lezárult; ekkor már nem módosítható.");
        return TeamValidationResult.Ok();
    }

    /// <summary>
    /// Capacity check for adding a member (human or AI) to a team. Caller has already
    /// confirmed the team is mutable.
    /// </summary>
    public static TeamValidationResult ValidateAddMember(int currentMemberCount, int currentAiCount, bool isAi)
    {
        if (currentMemberCount >= Team.MaxMembers)
            return TeamValidationResult.Fail(
                TeamRejectionReason.TeamFull,
                $"A csapat már elérte a {Team.MaxMembers} fős maximumot.");
        if (isAi && currentAiCount >= Team.MaxAiMembers)
            return TeamValidationResult.Fail(
                TeamRejectionReason.AiSlotTaken,
                "A csapatban már van AI tag.");
        return TeamValidationResult.Ok();
    }
}
