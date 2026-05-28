namespace Tip4Gen.Domain.Admin;

/// <summary>
/// One audit row per admin write. Persisted inside the same transaction as the
/// mutation it describes — without that guarantee a crash between mutation and
/// audit would leave us unable to explain how the world changed.
///
/// BeforeJson/AfterJson are opaque to the audit table on purpose: each action
/// records the shape that's relevant to it (e.g. Match snapshots {status, goals}
/// for SetResult; the long-tip outcomes serialize {winnerTeamId, topScorerName}).
/// Use the structured Action + EntityType + EntityId to filter, not the JSON.
/// </summary>
public class AdminAudit
{
    public const string EntityTypeMatch = "Match";
    public const string EntityTypeTournament = "Tournament";

    public Guid Id { get; private set; }
    public Guid AdminUserId { get; private set; }
    public AdminAuditAction Action { get; private set; }
    public string EntityType { get; private set; } = default!;
    public Guid? EntityId { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private AdminAudit() { }

    public AdminAudit(
        Guid adminUserId,
        AdminAuditAction action,
        string entityType,
        Guid? entityId,
        string? beforeJson,
        string? afterJson,
        string? reason)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("EntityType is required.", nameof(entityType));

        Id = Guid.NewGuid();
        AdminUserId = adminUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        OccurredAt = DateTimeOffset.UtcNow;
    }
}
