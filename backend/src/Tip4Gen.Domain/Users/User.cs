namespace Tip4Gen.Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Auth0Sub { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private User() { }

    public User(string auth0Sub, string displayName)
    {
        Id = Guid.NewGuid();
        Auth0Sub = auth0Sub;
        DisplayName = displayName;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Rename(string displayName) => DisplayName = displayName;
}
