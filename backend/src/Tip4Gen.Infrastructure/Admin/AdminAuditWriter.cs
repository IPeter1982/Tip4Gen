using System.Text.Json;
using System.Text.Json.Serialization;
using Tip4Gen.Domain.Admin;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Admin;

public class AdminAuditWriter(AppDbContext db) : IAdminAuditWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public Task RecordAsync(
        Guid adminUserId,
        AdminAuditAction action,
        string entityType,
        Guid? entityId,
        object? before,
        object? after,
        string? reason,
        CancellationToken ct)
    {
        var audit = new AdminAudit(
            adminUserId: adminUserId,
            action: action,
            entityType: entityType,
            entityId: entityId,
            beforeJson: before is null ? null : JsonSerializer.Serialize(before, JsonOpts),
            afterJson: after is null ? null : JsonSerializer.Serialize(after, JsonOpts),
            reason: reason);

        db.AdminAudits.Add(audit);
        // Intentionally no SaveChanges: caller's transaction owns persistence.
        return Task.CompletedTask;
    }
}
