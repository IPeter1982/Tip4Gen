using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Admin;
using Tip4Gen.Domain.Settings;
using Tip4Gen.Domain.Users;
using Tip4Gen.Infrastructure.Admin;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Settings;

public sealed record SetAiAvatarCommand(Guid AdminUserId, byte[] Bytes, string ContentType, string? Reason);

public abstract record AiAvatarResult
{
    public sealed record Success(string? Version) : AiAvatarResult;
    public sealed record Rejected(UserRejectionReason Reason, string Message) : AiAvatarResult;
}

public class AiAvatarAdminService(AppDbContext db, IAdminAuditWriter auditWriter, TimeProvider clock)
{
    public async Task<AiAvatarResult> SetAsync(SetAiAvatarCommand cmd, CancellationToken ct)
    {
        var validation = UserRulesValidator.ValidateAvatar(cmd.Bytes, cmd.ContentType);
        if (!validation.IsValid)
            return new AiAvatarResult.Rejected(validation.Reason, validation.Message!);

        var existing = await db.AiAvatarSettings.FirstOrDefaultAsync(ct);
        var before = existing is null ? null : new { existing.Version, existing.ContentType };

        var version = Convert.ToHexString(SHA256.HashData(cmd.Bytes))[..8].ToLowerInvariant();
        var now = clock.GetUtcNow();
        if (existing is null)
        {
            db.AiAvatarSettings.Add(AiAvatarSetting.Create(cmd.Bytes, cmd.ContentType, version, cmd.AdminUserId, now));
        }
        else
        {
            existing.Replace(cmd.Bytes, cmd.ContentType, version, cmd.AdminUserId, now);
        }

        var after = new { Version = version, ContentType = cmd.ContentType };
        await auditWriter.RecordAsync(
            adminUserId: cmd.AdminUserId,
            action: AdminAuditAction.AiAvatarSet,
            entityType: "AiAvatarSetting",
            entityId: null,
            before: before,
            after: after,
            reason: cmd.Reason,
            ct: ct);

        await db.SaveChangesAsync(ct);
        return new AiAvatarResult.Success(version);
    }

    public async Task<AiAvatarResult> ClearAsync(Guid adminUserId, string? reason, CancellationToken ct)
    {
        var existing = await db.AiAvatarSettings.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            // No-op: nothing to clear, no audit row written.
            return new AiAvatarResult.Success(null);
        }

        var before = new { existing.Version, existing.ContentType };
        db.AiAvatarSettings.Remove(existing);

        await auditWriter.RecordAsync(
            adminUserId: adminUserId,
            action: AdminAuditAction.AiAvatarDeleted,
            entityType: "AiAvatarSetting",
            entityId: null,
            before: before,
            after: null,
            reason: reason,
            ct: ct);

        await db.SaveChangesAsync(ct);
        return new AiAvatarResult.Success(null);
    }
}
