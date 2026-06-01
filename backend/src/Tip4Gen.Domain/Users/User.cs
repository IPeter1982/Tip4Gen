namespace Tip4Gen.Domain.Users;

public class User
{
    public const int MaxDisplayNameLength = 120;

    public Guid Id { get; private set; }
    public string Auth0Sub { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    /// <summary>
    /// Optional — populated from the Auth0 <c>email</c> claim on login. Required for the
    /// notifications worker (Phase 9) which runs without a JWT and can't re-fetch the
    /// claim. Stays nullable so sub-only Auth0 connections still work.
    /// </summary>
    public string? Email { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private User() { }

    public User(string auth0Sub, string displayName, string? email = null)
    {
        Id = Guid.NewGuid();
        Auth0Sub = auth0Sub;
        DisplayName = displayName;
        Email = NormalizeEmail(email);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Rename(string displayName) => DisplayName = displayName;

    public void SetEmail(string? email)
    {
        var normalized = NormalizeEmail(email);
        if (Email == normalized) return;
        Email = normalized;
    }

    private static string? NormalizeEmail(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? null : raw.Trim().ToLowerInvariant();
}
