using Tip4Gen.Domain.Tipping;

namespace Tip4Gen.Domain.Tests.Tipping;

public class LongTermTipRulesValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 06, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureStart = Now.AddDays(1);
    private static readonly Guid TeamA = Guid.NewGuid();

    [Fact]
    public void Both_fields_provided_before_lock_is_accepted()
    {
        var result = LongTermTipRulesValidator.Validate(Now, FutureStart, TeamA, "Messi");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Only_winner_is_accepted()
    {
        var result = LongTermTipRulesValidator.Validate(Now, FutureStart, TeamA, null);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Only_top_scorer_is_accepted()
    {
        var result = LongTermTipRulesValidator.Validate(Now, FutureStart, null, "Mbappé");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Neither_field_is_rejected()
    {
        var result = LongTermTipRulesValidator.Validate(Now, FutureStart, null, null);
        Assert.False(result.IsValid);
        Assert.Equal(LongTermTipRejectionReason.NothingProvided, result.Reason);
    }

    [Fact]
    public void Exactly_at_tournament_start_is_locked()
    {
        var result = LongTermTipRulesValidator.Validate(FutureStart, FutureStart, TeamA, "Messi");
        Assert.False(result.IsValid);
        Assert.Equal(LongTermTipRejectionReason.Locked, result.Reason);
    }

    [Fact]
    public void After_tournament_start_is_locked()
    {
        var result = LongTermTipRulesValidator.Validate(FutureStart.AddMinutes(1), FutureStart, TeamA, "Messi");
        Assert.False(result.IsValid);
        Assert.Equal(LongTermTipRejectionReason.Locked, result.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Blank_top_scorer_name_is_rejected_when_provided(string blank)
    {
        var result = LongTermTipRulesValidator.Validate(Now, FutureStart, null, blank);
        Assert.False(result.IsValid);
        Assert.Equal(LongTermTipRejectionReason.PlayerNameBlank, result.Reason);
    }

    [Fact]
    public void Overly_long_top_scorer_name_is_rejected()
    {
        var name = new string('a', LongTermTipRulesValidator.MaxPlayerNameLength + 1);
        var result = LongTermTipRulesValidator.Validate(Now, FutureStart, null, name);
        Assert.False(result.IsValid);
        Assert.Equal(LongTermTipRejectionReason.PlayerNameTooLong, result.Reason);
    }

    [Fact]
    public void Lock_check_runs_before_field_checks()
    {
        // After lock with no fields → still rejects as Locked, not NothingProvided.
        var result = LongTermTipRulesValidator.Validate(FutureStart.AddHours(1), FutureStart, null, null);
        Assert.False(result.IsValid);
        Assert.Equal(LongTermTipRejectionReason.Locked, result.Reason);
    }
}
