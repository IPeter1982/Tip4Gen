using Tip4Gen.Domain.Users;

namespace Tip4Gen.Domain.Teams;

public class Team
{
    public const int MaxMembers = 3;
    public const int MaxAiMembers = 1;
    public const int MaxNameLength = 80;
    public const int MaxAvatarBytes = User.MaxAvatarBytes;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public TeamStatus Status { get; private set; }
    public AiMode? AiMode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public byte[]? Avatar { get; private set; }
    public string? AvatarContentType { get; private set; }
    public string? AvatarVersion { get; private set; }

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

    public void SetAvatar(byte[] bytes, string contentType, string version)
    {
        Avatar = bytes;
        AvatarContentType = contentType;
        AvatarVersion = version;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearAvatar()
    {
        Avatar = null;
        AvatarContentType = null;
        AvatarVersion = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
