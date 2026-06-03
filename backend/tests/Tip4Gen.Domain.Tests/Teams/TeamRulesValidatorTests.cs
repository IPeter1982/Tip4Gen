using Tip4Gen.Domain.Teams;

namespace Tip4Gen.Domain.Tests.Teams;

public class TeamRulesValidatorTests
{
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
    public void ValidateMutable_passes_for_forming_team()
    {
        // Forming = mutable regardless of when (joining/creating is allowed after start).
        Assert.True(TeamRulesValidator.ValidateMutable(TeamStatus.Forming).IsValid);
    }

    [Fact]
    public void ValidateMutable_rejects_when_team_locked()
    {
        var r = TeamRulesValidator.ValidateMutable(TeamStatus.Locked);
        Assert.False(r.IsValid);
        Assert.Equal(TeamRejectionReason.TeamLocked, r.Reason);
    }

    [Fact]
    public void ValidateMutable_rejects_when_team_disqualified()
    {
        var r = TeamRulesValidator.ValidateMutable(TeamStatus.Disqualified);
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
        var r = TeamRulesValidator.ValidateAddMember(3, 0, isAi: false);
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
        var r = TeamRulesValidator.ValidateAddMember(3, 0, isAi: true);
        Assert.False(r.IsValid);
        Assert.Equal(TeamRejectionReason.TeamFull, r.Reason);
    }
}
