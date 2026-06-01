namespace Tip4Gen.Domain.Users;

public enum UserRejectionReason
{
    None = 0,
    DisplayNameBlank,
    DisplayNameTooLong,
    AvatarMissing,
    AvatarUnsupportedFormat,
    AvatarTooLarge,
}

public readonly record struct UserValidationResult(bool IsValid, UserRejectionReason Reason, string? Message)
{
    public static UserValidationResult Ok() => new(true, UserRejectionReason.None, null);
    public static UserValidationResult Fail(UserRejectionReason reason, string message) => new(false, reason, message);
}

public static class UserRulesValidator
{
    public static UserValidationResult ValidateDisplayName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return UserValidationResult.Fail(
                UserRejectionReason.DisplayNameBlank,
                "A megjelenített név nem lehet üres.");
        if (trimmed.Length > User.MaxDisplayNameLength)
            return UserValidationResult.Fail(
                UserRejectionReason.DisplayNameTooLong,
                $"A megjelenített név maximum {User.MaxDisplayNameLength} karakter lehet.");
        return UserValidationResult.Ok();
    }

    public static UserValidationResult ValidateAvatar(byte[]? bytes, string? contentType)
    {
        if (bytes is null || bytes.Length == 0)
            return UserValidationResult.Fail(
                UserRejectionReason.AvatarMissing,
                "Nincs kép kiválasztva.");
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
            return UserValidationResult.Fail(
                UserRejectionReason.AvatarUnsupportedFormat,
                "Csak JPEG, PNG vagy WebP képek tölthetők fel.");
        if (bytes.Length > User.MaxAvatarBytes)
            return UserValidationResult.Fail(
                UserRejectionReason.AvatarTooLarge,
                $"A kép maximum {User.MaxAvatarBytes / 1024} KB lehet.");
        return UserValidationResult.Ok();
    }
}
