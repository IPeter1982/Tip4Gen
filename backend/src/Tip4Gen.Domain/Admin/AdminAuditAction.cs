namespace Tip4Gen.Domain.Admin;

/// <summary>
/// Distinct admin actions that produce audit rows. Each maps to a single endpoint
/// in the admin controllers; new admin endpoints get a new enum value rather than
/// reusing an existing one — the action name is the primary lens for the audit log.
/// </summary>
public enum AdminAuditAction
{
    MatchSetResult = 0,
    MatchCancel = 1,
    MatchPostpone = 2,
    MatchRescore = 3,
    LongTipOutcomesSet = 4,
    AiAvatarSet = 5,
    AiAvatarDeleted = 6,
}
