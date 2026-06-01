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

    // ===== Avatar =====

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void ValidateAvatar_accepts_supported_formats(string contentType)
    {
        var bytes = new byte[1024];
        Assert.True(UserRulesValidator.ValidateAvatar(bytes, contentType).IsValid);
    }

    [Fact]
    public void ValidateAvatar_rejects_missing_bytes()
    {
        Assert.Equal(UserRejectionReason.AvatarMissing, UserRulesValidator.ValidateAvatar(null, "image/jpeg").Reason);
        Assert.Equal(UserRejectionReason.AvatarMissing, UserRulesValidator.ValidateAvatar(Array.Empty<byte>(), "image/jpeg").Reason);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("text/plain")]
    [InlineData("application/octet-stream")]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateAvatar_rejects_unsupported_formats(string? contentType)
    {
        var bytes = new byte[1024];
        Assert.Equal(UserRejectionReason.AvatarUnsupportedFormat, UserRulesValidator.ValidateAvatar(bytes, contentType).Reason);
    }

    [Fact]
    public void ValidateAvatar_rejects_too_large()
    {
        var bytes = new byte[User.MaxAvatarBytes + 1];
        Assert.Equal(UserRejectionReason.AvatarTooLarge, UserRulesValidator.ValidateAvatar(bytes, "image/jpeg").Reason);
    }

    [Fact]
    public void ValidateAvatar_accepts_exactly_max_size()
    {
        var bytes = new byte[User.MaxAvatarBytes];
        Assert.True(UserRulesValidator.ValidateAvatar(bytes, "image/jpeg").IsValid);
    }
}
