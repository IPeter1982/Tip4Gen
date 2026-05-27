namespace Tip4Gen.Domain.Teams;

/// <summary>
/// Single-use join token for a team. Tokens are opaque strings; service generates them
/// from a CSPRNG. UsedByUserId is set on first redemption so the same link cannot be
/// reused after consumption.
/// </summary>
public class TeamInvite
{
    public const int TokenLength = 32;

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public string Token { get; private set; } = default!;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public Guid? UsedByUserId { get; private set; }

    private TeamInvite() { }

    public TeamInvite(Guid teamId, string token, Guid createdByUserId, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be blank.", nameof(token));

        Id = Guid.NewGuid();
        TeamId = teamId;
        Token = token;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsActive(DateTimeOffset now) => UsedAt is null && now < ExpiresAt;

    public void Redeem(Guid userId)
    {
        UsedAt = DateTimeOffset.UtcNow;
        UsedByUserId = userId;
    }
}
