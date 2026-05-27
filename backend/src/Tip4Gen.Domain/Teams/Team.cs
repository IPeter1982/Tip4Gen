namespace Tip4Gen.Domain.Teams;

public class Team
{
    public const int MaxMembers = 4;
    public const int MaxAiMembers = 1;
    public const int MaxNameLength = 80;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public TeamStatus Status { get; private set; }
    public AiMode? AiMode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Team() { }

    public Team(string name)
    {
        Id = Guid.NewGuid();
        Rename(name);
        Status = TeamStatus.Forming;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Rename(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Team name cannot be blank.", nameof(name));
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"Team name exceeds {MaxNameLength} characters.", nameof(name));
        Name = trimmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetAiMode(AiMode? mode)
    {
        AiMode = mode;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Lock()
    {
        Status = TeamStatus.Locked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Disqualify()
    {
        Status = TeamStatus.Disqualified;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
