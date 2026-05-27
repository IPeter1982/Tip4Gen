namespace Tip4Gen.Domain.Teams;

/// <summary>
/// A team member. For human members, UserId points to users.id and AiDisplayName is null.
/// For the AI slot, UserId is null, IsAi is true, and AiDisplayName carries the persona
/// name shown in the UI. The Phase 6 AI tipper writes tips keyed on the team itself,
/// not a synthetic user row.
/// </summary>
public class TeamMember
{
    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid? UserId { get; private set; }
    public bool IsAi { get; private set; }
    public string? AiDisplayName { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private TeamMember() { }

    public static TeamMember ForHuman(Guid teamId, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        TeamId = teamId,
        UserId = userId,
        IsAi = false,
        AiDisplayName = null,
        JoinedAt = DateTimeOffset.UtcNow,
    };

    public static TeamMember ForAi(Guid teamId, string aiDisplayName)
    {
        if (string.IsNullOrWhiteSpace(aiDisplayName))
            throw new ArgumentException("AI members must have a display name.", nameof(aiDisplayName));
        return new()
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = null,
            IsAi = true,
            AiDisplayName = aiDisplayName.Trim(),
            JoinedAt = DateTimeOffset.UtcNow,
        };
    }
}
