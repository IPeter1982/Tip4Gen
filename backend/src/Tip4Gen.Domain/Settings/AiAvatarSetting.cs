namespace Tip4Gen.Domain.Settings;

/// <summary>
/// Single global image rendered for every AI team member. Lives in a singleton table
/// (id = 1, enforced by a DB check constraint). Admin upload via /api/admin/ai-avatar
/// goes through the same byte/content-type validation as personal user avatars
/// (UserRulesValidator.ValidateAvatar).
/// </summary>
public class AiAvatarSetting
{
    public const int SingletonId = 1;

    public int Id { get; private set; } = SingletonId;
    public byte[] Avatar { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public string Version { get; private set; } = default!;
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedByUserId { get; private set; }

    private AiAvatarSetting() { }

    public static AiAvatarSetting Create(byte[] bytes, string contentType, string version, Guid updatedBy, DateTimeOffset nowUtc)
        => new()
        {
            Id = SingletonId,
            Avatar = bytes,
            ContentType = contentType,
            Version = version,
            UpdatedAtUtc = nowUtc,
            UpdatedByUserId = updatedBy,
        };

    public void Replace(byte[] bytes, string contentType, string version, Guid updatedBy, DateTimeOffset nowUtc)
    {
        Avatar = bytes;
        ContentType = contentType;
        Version = version;
        UpdatedAtUtc = nowUtc;
        UpdatedByUserId = updatedBy;
    }
}
