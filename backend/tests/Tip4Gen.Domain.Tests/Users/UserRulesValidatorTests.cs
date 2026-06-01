using Tip4Gen.Domain.Users;

namespace Tip4Gen.Domain.Tests.Users;

public class UserRulesValidatorTests
{
    [Fact]
    public void ValidateDisplayName_accepts_a_normal_name()
    {
        var r = UserRulesValidator.ValidateDisplayName("Péter");
        Assert.True(r.IsValid);
        Assert.Equal(UserRejectionReason.None, r.Reason);
    }

    [Fact]
    public void ValidateDisplayName_rejects_blank()
    {
        Assert.Equal(UserRejectionReason.DisplayNameBlank, UserRulesValidator.ValidateDisplayName(null).Reason);
        Assert.Equal(UserRejectionReason.DisplayNameBlank, UserRulesValidator.ValidateDisplayName("").Reason);
        Assert.Equal(UserRejectionReason.DisplayNameBlank, UserRulesValidator.ValidateDisplayName("   ").Reason);
    }

    [Fact]
    public void ValidateDisplayName_rejects_too_long()
    {
        var longName = new string('x', User.MaxDisplayNameLength + 1);
        Assert.Equal(UserRejectionReason.DisplayNameTooLong, UserRulesValidator.ValidateDisplayName(longName).Reason);
    }

    [Fact]
    public void ValidateDisplayName_accepts_exactly_max_length()
    {
        var maxName = new string('x', User.MaxDisplayNameLength);
        Assert.True(UserRulesValidator.ValidateDisplayName(maxName).IsValid);
    }

    [Fact]
    public void ValidateDisplayName_trims_before_length_check()
    {
        // Surrounding whitespace doesn't count toward the limit.
        var padded = "  " + new string('x', User.MaxDisplayNameLength) + "  ";
        Assert.True(UserRulesValidator.ValidateDisplayName(padded).IsValid);
    }
}
