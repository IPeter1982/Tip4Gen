using Tip4Gen.Domain.Teams;

namespace Tip4Gen.Domain.Tests.Teams;

public class TeamRulesValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 06, 10, 12, 00, 00, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureStart = Now.AddDays(2);
    private static readonly DateTimeOffset PastStart = Now.AddDays(-1);

    // ===== Name =====

    [Fact]
    public void ValidateName_accepts_a_normal_name()
    {
        var r = TeamRulesValidator.ValidateName("Lila Fecskék");
        Assert.True(r.IsValid);
        Assert.Equal(TeamRejectionReason.None, r.Reason);
    }

    [Fact]
    public void ValidateName_rejects_blank()
    {
        Assert.Equal(TeamRejectionReason.NameBlank, TeamRulesValidator.ValidateName(null).Reason);
        Assert.Equal(TeamRejectionReason.NameBlank, TeamRulesValidator.ValidateName("").Reason);
        Assert.Equal(TeamRejectionReason.NameBlank, TeamRulesValidator.ValidateName("   ").Reason);
    }

    [Fact]
    public void ValidateName_rejects_too_long()
    {
        var longName = new string('x', Team.MaxNameLength + 1);
        Assert.Equal(TeamRejectionReason.NameTooLong, TeamRulesValidator.ValidateName(longName).Reason);
    }

    [Fact]
    public void ValidateName_accepts_exactly_max_length()
    {
        var maxName = new string('x', Team.MaxNameLength);
        Assert.True(TeamRulesValidator.ValidateName(maxName).IsValid);
    }

    // ===== Mutability =====

    [Fact]
    public void ValidateMutable_passes_before_tournament_start_when_forming()
    {
        var r = TeamRulesValidator.ValidateMutable(Now, FutureStart, TeamStatus.Forming);
        Assert.True(r.IsValid);
    }

    [Fact]
    public void ValidateMutable_passes_when_no_tournament_yet()
    {
        var r = TeamRulesValidator.ValidateMutable(Now, null, TeamStatus.Forming);
        Assert.True(r.IsValid);
    }

    [Fact]
    public void ValidateMutable_rejects_after_tournament_start_even_when_forming()
    {
        // Tournament-start lock wins over team-status check so the error explains itself.
        var r = TeamRulesValidator.ValidateMutable(Now, PastStart, TeamStatus.Forming);
        Assert.False(r.IsValid);
        Assert.Equal(TeamRejectionReason.TournamentStarted, r.Reason);
    }

    [Fact]
    public void ValidateMutable_rejects_when_team_locked()
    {
        var r = TeamRulesValidator.ValidateMutable(Now, FutureStart, TeamStatus.Locked);
        Assert.False(r.IsValid);
        Assert.Equal(TeamRejectionReason.TeamLocked, r.Reason);
    }

    [Fact]
    public void ValidateMutable_rejects_when_team_disqualified()
    {
        var r = TeamRulesValidator.ValidateMutable(Now, FutureStart, TeamStatus.Disqualified);
        Assert.False(r.IsValid);
        Assert.Equal(TeamRejectionReason.TeamLocked, r.Reason);
    }

    // ===== Capacity =====

    [Fact]
    public void ValidateAddMember_accepts_adding_to_empty_team()
    {
        Assert.True(TeamRulesValidator.ValidateAddMember(0, 0, isAi: false).IsValid);
        Assert.True(TeamRulesValidator.ValidateAddMember(0, 0, isAi: true).IsValid);
    }

    [Fact]
    public void ValidateAddMember_rejects_when_team_full()
    {
        var r = TeamRulesValidator.ValidateAddMember(4, 0, isAi: false);
        Assert.False(r.IsValid);
        Assert.Equal(TeamRejectionReason.TeamFull, r.Reason);
    }

    [Fact]
    public void ValidateAddMember_rejects_second_ai()
    {
        var r = TeamRulesValidator.ValidateAddMember(2, 1, isAi: true);
        Assert.False(r.IsValid);
        Assert.Equal(TeamRejectionReason.AiSlotTaken, r.Reason);
    }

    [Fact]
    public void ValidateAddMember_allows_first_ai_when_slot_open()
    {
        var r = TeamRulesValidator.ValidateAddMember(2, 0, isAi: true);
        Assert.True(r.IsValid);
    }

    [Fact]
    public void ValidateAddMember_capacity_check_runs_before_ai_check()
    {
        // Team is already full; trying to add an AI still surfaces TeamFull (the right diagnosis).
        var r = TeamRulesValidator.ValidateAddMember(4, 0, isAi: true);
        Assert.False(r.IsValid);
        Assert.Equal(TeamRejectionReason.TeamFull, r.Reason);
    }
}
