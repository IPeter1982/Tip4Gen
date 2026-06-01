namespace Tip4Gen.Domain.Users;

public enum UserRejectionReason
{
    None = 0,
    DisplayNameBlank,
    DisplayNameTooLong,
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
}
