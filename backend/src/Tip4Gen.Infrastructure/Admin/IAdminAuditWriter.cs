using Tip4Gen.Domain.Admin;

namespace Tip4Gen.Infrastructure.Admin;

public interface IAdminAuditWriter
{
    /// <summary>
    /// Stage an <see cref="AdminAudit"/> row on the current DbContext. The caller is
    /// responsible for calling SaveChangesAsync so the audit lands in the same
    /// transaction as the mutation it describes (CLAUDE.md: "every /api/admin/* write
    /// must record a row in admin_audit in the same transaction").
    ///
    /// `before` / `after` are serialized to JSON via System.Text.Json with the project's
    /// web defaults (camelCase, enum-as-string globally). Pass anonymous objects or
    /// records of just the fields that changed; don't dump full aggregates.
    /// </summary>
    Task RecordAsync(
        Guid adminUserId,
        AdminAuditAction action,
        string entityType,
        Guid? entityId,
        object? before,
        object? after,
        string? reason,
        CancellationToken ct);
}
