namespace Tip4Gen.Domain.Notifications;

/// <summary>
/// One delivery attempt for a (user, kind, match) triplet. Successful rows are the
/// dedup ledger — the policy refuses to re-send a kind that already has a successful
/// log row. Failed rows are kept for diagnostics; the next tick may retry.
/// </summary>
public class NotificationLog
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public Guid? MatchId { get; private set; }
    public DateTimeOffset SentAt { get; private set; }
    public bool Success { get; private set; }
    public string? Error { get; private set; }

    private NotificationLog() { }

    public NotificationLog(
        Guid userId,
        NotificationKind kind,
        Guid? matchId,
        bool success,
        string? error)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Kind = kind;
        MatchId = matchId;
        SentAt = DateTimeOffset.UtcNow;
        Success = success;
        Error = success ? null : (error ?? "unknown error");
    }
}
