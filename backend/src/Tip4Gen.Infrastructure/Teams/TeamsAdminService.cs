using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Admin;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Infrastructure.Admin;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Teams;

public sealed record TeamAdminMemberView(
    Guid Id,
    Guid? UserId,
    string DisplayName,
    string? AvatarVersion,
    bool IsAi,
    DateTimeOffset JoinedAt);

public sealed record TeamAdminView(
    Guid Id,
    string Name,
    TeamStatus Status,
    AiMode? AiMode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? AvatarVersion,
    int MemberCount,
    int HumanMemberCount,
    int AiMemberCount,
    IReadOnlyList<TeamAdminMemberView> Members);

public abstract record TeamAdminRenameResult
{
    public sealed record Success(TeamAdminView Team) : TeamAdminRenameResult;
    public sealed record NotFound : TeamAdminRenameResult;
    public sealed record NameInvalid(string Message) : TeamAdminRenameResult;
}

public abstract record TeamAdminDeleteResult
{
    public sealed record Success(Guid TeamId, string Name, int MembersRemoved) : TeamAdminDeleteResult;
    public sealed record NotFound : TeamAdminDeleteResult;
}

public abstract record TeamMemberRemoveResult
{
    /// <summary>Member removed; team continues. StatusRevertedToForming is true when a Locked team dropped below MaxMembers and was auto-unlocked.</summary>
    public sealed record Success(TeamAdminView Team, bool StatusRevertedToForming) : TeamMemberRemoveResult;

    /// <summary>Member removed was the last human; the whole team (and any remaining AI slot) was cascade-deleted.</summary>
    public sealed record TeamCascadeDeleted(Guid TeamId, string TeamName, Guid RemovedMemberId, int OtherMembersRemoved) : TeamMemberRemoveResult;

    public sealed record TeamNotFound : TeamMemberRemoveResult;
    public sealed record MemberNotFound : TeamMemberRemoveResult;
}

public record AdminRenameTeamCommand(Guid AdminUserId, Guid TeamId, string NewName, string? Reason);
public record AdminDeleteTeamCommand(Guid AdminUserId, Guid TeamId, string? Reason);
public record AdminRemoveMemberCommand(Guid AdminUserId, Guid TeamId, Guid MemberId, string? Reason);

public interface ITeamsAdminService
{
    /// <summary>Lists every team (all statuses) with its full member roster, for the admin overview.</summary>
    Task<IReadOnlyList<TeamAdminView>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Renames a team. Bypasses the Forming-only mutability check that user-facing PATCH applies —
    /// admin can rename Locked / Disqualified teams. Name format is still validated.
    /// </summary>
    Task<TeamAdminRenameResult> RenameAsync(AdminRenameTeamCommand cmd, CancellationToken ct);

    /// <summary>
    /// Deletes a team. team_members and team_invites cascade via DB FK; AI tips/scored_tips
    /// also cascade via team_members.id. Human tips/scored_tips persist (keyed by user_id).
    /// </summary>
    Task<TeamAdminDeleteResult> DeleteAsync(AdminDeleteTeamCommand cmd, CancellationToken ct);

    /// <summary>
    /// Removes a single member from a team. If removing leaves no humans, the whole team (and any
    /// AI slot) cascade-deletes (mirrors TeamsService.LeaveAsync). Otherwise, if the team was Locked
    /// and now has fewer than Team.MaxMembers, status reverts to Forming so the existing direct-join
    /// path can refill it and TeamLockJob can re-lock.
    /// </summary>
    Task<TeamMemberRemoveResult> RemoveMemberAsync(AdminRemoveMemberCommand cmd, CancellationToken ct);
}

public class TeamsAdminService(
    AppDbContext db,
    IAdminAuditWriter auditWriter,
    ILogger<TeamsAdminService> logger) : ITeamsAdminService
{
    public async Task<IReadOnlyList<TeamAdminView>> ListAsync(CancellationToken ct)
    {
        var teams = await db.Teams.AsNoTracking()
            .OrderBy(t => t.Status)
            .ThenBy(t => t.Name)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Status,
                t.AiMode,
                t.CreatedAt,
                t.UpdatedAt,
                t.AvatarVersion,
            })
            .ToListAsync(ct);
        if (teams.Count == 0) return Array.Empty<TeamAdminView>();

        var teamIds = teams.Select(t => t.Id).ToList();
        var rows = await (
            from m in db.TeamMembers.AsNoTracking().Where(m => teamIds.Contains(m.TeamId))
            from u in db.Users.AsNoTracking().Where(u => u.Id == m.UserId).DefaultIfEmpty()
            orderby m.JoinedAt
            select new
            {
                m,
                UserName = u != null ? u.DisplayName : null,
                UserAvatarVersion = u != null ? u.AvatarVersion : null,
            }).ToListAsync(ct);

        var membersByTeam = rows
            .GroupBy(r => r.m.TeamId)
            .ToDictionary(g => g.Key, g => g.Select(r => new TeamAdminMemberView(
                Id: r.m.Id,
                UserId: r.m.UserId,
                DisplayName: r.m.IsAi ? r.m.AiDisplayName! : (r.UserName ?? "?"),
                AvatarVersion: r.m.IsAi ? null : r.UserAvatarVersion,
                IsAi: r.m.IsAi,
                JoinedAt: r.m.JoinedAt)).ToList());

        return teams.Select(t =>
        {
            var members = membersByTeam.TryGetValue(t.Id, out var ms)
                ? ms
                : new List<TeamAdminMemberView>();
            var aiCount = members.Count(m => m.IsAi);
            return new TeamAdminView(
                Id: t.Id,
                Name: t.Name,
                Status: t.Status,
                AiMode: t.AiMode,
                CreatedAt: t.CreatedAt,
                UpdatedAt: t.UpdatedAt,
                AvatarVersion: t.AvatarVersion,
                MemberCount: members.Count,
                HumanMemberCount: members.Count - aiCount,
                AiMemberCount: aiCount,
                Members: members);
        }).ToList();
    }

    public async Task<TeamAdminRenameResult> RenameAsync(AdminRenameTeamCommand cmd, CancellationToken ct)
    {
        var validation = TeamRulesValidator.ValidateName(cmd.NewName);
        if (!validation.IsValid)
            return new TeamAdminRenameResult.NameInvalid(validation.Message ?? "Érvénytelen csapatnév.");

        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == cmd.TeamId, ct);
        if (team is null)
            return new TeamAdminRenameResult.NotFound();

        var before = new { name = team.Name };

        team.Rename(cmd.NewName);

        var after = new { name = team.Name };

        await auditWriter.RecordAsync(
            adminUserId: cmd.AdminUserId,
            action: AdminAuditAction.TeamRenamed,
            entityType: AdminAudit.EntityTypeTeam,
            entityId: cmd.TeamId,
            before: before,
            after: after,
            reason: cmd.Reason,
            ct: ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminUserId} renamed team {TeamId}: {Before} → {After}",
            cmd.AdminUserId, cmd.TeamId, before.name, after.name);

        return new TeamAdminRenameResult.Success(await ProjectAsync(team.Id, ct));
    }

    public async Task<TeamAdminDeleteResult> DeleteAsync(AdminDeleteTeamCommand cmd, CancellationToken ct)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == cmd.TeamId, ct);
        if (team is null)
            return new TeamAdminDeleteResult.NotFound();

        var members = await db.TeamMembers.AsNoTracking()
            .Where(m => m.TeamId == cmd.TeamId)
            .Select(m => new { m.Id, m.UserId, m.IsAi, m.AiDisplayName })
            .ToListAsync(ct);

        var before = new
        {
            name = team.Name,
            status = team.Status,
            memberCount = members.Count,
            humanMembers = members.Count(m => !m.IsAi),
            aiMembers = members.Count(m => m.IsAi),
        };

        db.Teams.Remove(team);

        await auditWriter.RecordAsync(
            adminUserId: cmd.AdminUserId,
            action: AdminAuditAction.TeamDeleted,
            entityType: AdminAudit.EntityTypeTeam,
            entityId: cmd.TeamId,
            before: before,
            after: null,
            reason: cmd.Reason,
            ct: ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminUserId} deleted team {TeamId} ({Name}); {MemberCount} members cascaded",
            cmd.AdminUserId, cmd.TeamId, team.Name, members.Count);

        return new TeamAdminDeleteResult.Success(cmd.TeamId, team.Name, members.Count);
    }

    public async Task<TeamMemberRemoveResult> RemoveMemberAsync(AdminRemoveMemberCommand cmd, CancellationToken ct)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == cmd.TeamId, ct);
        if (team is null)
            return new TeamMemberRemoveResult.TeamNotFound();

        var member = await db.TeamMembers
            .FirstOrDefaultAsync(m => m.Id == cmd.MemberId && m.TeamId == cmd.TeamId, ct);
        if (member is null)
            return new TeamMemberRemoveResult.MemberNotFound();

        var memberDisplayName = member.IsAi
            ? member.AiDisplayName ?? "?"
            : (await db.Users.AsNoTracking()
                .Where(u => u.Id == member.UserId)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync(ct)) ?? "?";

        var teamStatusBefore = team.Status;
        var teamNameBefore = team.Name;

        // Decide cascade: if removing this member leaves no humans, the whole team goes
        // (mirrors TeamsService.LeaveAsync at the last-human path).
        var remainingHumans = await db.TeamMembers
            .CountAsync(m => m.TeamId == cmd.TeamId && m.Id != member.Id && !m.IsAi, ct);

        if (!member.IsAi && remainingHumans == 0)
        {
            var stragglers = await db.TeamMembers
                .Where(m => m.TeamId == cmd.TeamId && m.Id != member.Id)
                .ToListAsync(ct);

            db.TeamMembers.Remove(member);
            db.TeamMembers.RemoveRange(stragglers);
            db.Teams.Remove(team);

            var before = new
            {
                memberId = member.Id,
                displayName = memberDisplayName,
                isAi = member.IsAi,
                teamName = teamNameBefore,
                teamStatus = teamStatusBefore,
            };
            var after = new
            {
                remainingMembers = 0,
                statusAfter = (TeamStatus?)null,
                teamCascadeDeleted = true,
            };

            await auditWriter.RecordAsync(
                adminUserId: cmd.AdminUserId,
                action: AdminAuditAction.TeamMemberRemoved,
                entityType: AdminAudit.EntityTypeTeam,
                entityId: cmd.TeamId,
                before: before,
                after: after,
                reason: cmd.Reason,
                ct: ct);

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Admin {AdminUserId} removed last human {MemberId} from team {TeamId}; team and {OtherCount} other members cascade-deleted",
                cmd.AdminUserId, member.Id, cmd.TeamId, stragglers.Count);

            return new TeamMemberRemoveResult.TeamCascadeDeleted(
                cmd.TeamId, teamNameBefore, member.Id, stragglers.Count);
        }

        // Remove the single member, then decide whether to auto-revert the team to Forming.
        db.TeamMembers.Remove(member);

        var remainingTotal = await db.TeamMembers
            .CountAsync(m => m.TeamId == cmd.TeamId && m.Id != member.Id, ct);

        var revertedToForming = false;
        if (team.Status == TeamStatus.Locked && remainingTotal < Team.MaxMembers)
        {
            team.Unlock();
            revertedToForming = true;
        }

        var beforeNorm = new
        {
            memberId = member.Id,
            displayName = memberDisplayName,
            isAi = member.IsAi,
            teamName = teamNameBefore,
            teamStatus = teamStatusBefore,
        };
        var afterNorm = new
        {
            remainingMembers = remainingTotal,
            statusAfter = (TeamStatus?)team.Status,
            teamCascadeDeleted = false,
        };

        await auditWriter.RecordAsync(
            adminUserId: cmd.AdminUserId,
            action: AdminAuditAction.TeamMemberRemoved,
            entityType: AdminAudit.EntityTypeTeam,
            entityId: cmd.TeamId,
            before: beforeNorm,
            after: afterNorm,
            reason: cmd.Reason,
            ct: ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminUserId} removed member {MemberId} ({IsAi}) from team {TeamId}; remaining {Remaining}, reverted={Reverted}",
            cmd.AdminUserId, member.Id, member.IsAi ? "AI" : "human", cmd.TeamId, remainingTotal, revertedToForming);

        return new TeamMemberRemoveResult.Success(await ProjectAsync(cmd.TeamId, ct), revertedToForming);
    }

    private async Task<TeamAdminView> ProjectAsync(Guid teamId, CancellationToken ct)
    {
        // After SaveChanges; build a fresh projection so the caller sees authoritative state
        // (including any status flip from Unlock).
        var team = await db.Teams.AsNoTracking()
            .Where(t => t.Id == teamId)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Status,
                t.AiMode,
                t.CreatedAt,
                t.UpdatedAt,
                t.AvatarVersion,
            })
            .FirstAsync(ct);

        var members = await (
            from m in db.TeamMembers.AsNoTracking().Where(m => m.TeamId == teamId)
            from u in db.Users.AsNoTracking().Where(u => u.Id == m.UserId).DefaultIfEmpty()
            orderby m.JoinedAt
            select new TeamAdminMemberView(
                m.Id,
                m.UserId,
                m.IsAi ? m.AiDisplayName! : (u != null ? u.DisplayName : "?"),
                m.IsAi ? null : (u != null ? u.AvatarVersion : null),
                m.IsAi,
                m.JoinedAt)).ToListAsync(ct);

        var aiCount = members.Count(m => m.IsAi);
        return new TeamAdminView(
            Id: team.Id,
            Name: team.Name,
            Status: team.Status,
            AiMode: team.AiMode,
            CreatedAt: team.CreatedAt,
            UpdatedAt: team.UpdatedAt,
            AvatarVersion: team.AvatarVersion,
            MemberCount: members.Count,
            HumanMemberCount: members.Count - aiCount,
            AiMemberCount: aiCount,
            Members: members);
    }
}
