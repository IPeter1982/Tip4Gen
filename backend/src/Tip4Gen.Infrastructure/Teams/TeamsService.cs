using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Teams;

public record TeamMemberView(Guid Id, Guid? UserId, string DisplayName, string? AvatarVersion, bool IsAi, DateTimeOffset JoinedAt);

public record TeamView(
    Guid Id,
    string Name,
    TeamStatus Status,
    AiMode? AiMode,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TeamMemberView> Members);

public record InviteView(string Token, DateTimeOffset ExpiresAt);

public abstract record TeamCreateResult
{
    public sealed record Success(TeamView Team) : TeamCreateResult;
    public sealed record AlreadyInTeam : TeamCreateResult;
    public sealed record Rejected(TeamValidationResult Validation) : TeamCreateResult;
}

public abstract record TeamPatchResult
{
    public sealed record Success(TeamView Team) : TeamPatchResult;
    public sealed record NotFound : TeamPatchResult;
    public sealed record NotMember : TeamPatchResult;
    public sealed record Rejected(TeamValidationResult Validation) : TeamPatchResult;
}

public abstract record TeamLeaveResult
{
    public sealed record Success : TeamLeaveResult;
    public sealed record NotFound : TeamLeaveResult;
    public sealed record NotMember : TeamLeaveResult;
    public sealed record Rejected(TeamValidationResult Validation) : TeamLeaveResult;
}

public abstract record AddAiMemberResult
{
    public sealed record Success(TeamView Team) : AddAiMemberResult;
    public sealed record NotFound : AddAiMemberResult;
    public sealed record NotMember : AddAiMemberResult;
    public sealed record Rejected(TeamValidationResult Validation) : AddAiMemberResult;
}

public abstract record CreateInviteResult
{
    public sealed record Success(InviteView Invite) : CreateInviteResult;
    public sealed record NotFound : CreateInviteResult;
    public sealed record NotMember : CreateInviteResult;
    public sealed record Rejected(TeamValidationResult Validation) : CreateInviteResult;
}

public abstract record JoinResult
{
    public sealed record Success(TeamView Team) : JoinResult;
    public sealed record InviteNotFound : JoinResult;
    public sealed record InviteExpiredOrUsed : JoinResult;
    public sealed record AlreadyInTeam : JoinResult;
    public sealed record Rejected(TeamValidationResult Validation) : JoinResult;
}

public record PatchTeamCommand(Guid TeamId, string? Name, AiMode? AiMode, bool ClearAiMode);
public record AddAiMemberCommand(Guid TeamId, string DisplayName, AiMode Mode);

public interface ITeamsService
{
    Task<TeamCreateResult> CreateAsync(Guid creatorUserId, string name, CancellationToken ct);
    Task<TeamView?> GetMyTeamAsync(Guid userId, CancellationToken ct);
    Task<TeamPatchResult> PatchAsync(Guid actingUserId, PatchTeamCommand cmd, CancellationToken ct);
    Task<TeamLeaveResult> LeaveAsync(Guid actingUserId, Guid teamId, CancellationToken ct);
    Task<AddAiMemberResult> AddAiMemberAsync(Guid actingUserId, AddAiMemberCommand cmd, CancellationToken ct);
    Task<CreateInviteResult> CreateInviteAsync(Guid actingUserId, Guid teamId, CancellationToken ct);
    Task<JoinResult> JoinByTokenAsync(Guid actingUserId, string token, CancellationToken ct);
}

public class TeamsService(AppDbContext db, ILogger<TeamsService> logger) : ITeamsService
{
    /// <summary>Invites expire 7 days after creation.</summary>
    private static readonly TimeSpan InviteTtl = TimeSpan.FromDays(7);

    public async Task<TeamCreateResult> CreateAsync(Guid creatorUserId, string name, CancellationToken ct)
    {
        var nameValidation = TeamRulesValidator.ValidateName(name);
        if (!nameValidation.IsValid)
            return new TeamCreateResult.Rejected(nameValidation);

        var alreadyMember = await db.TeamMembers.AnyAsync(m => m.UserId == creatorUserId, ct);
        if (alreadyMember)
            return new TeamCreateResult.AlreadyInTeam();

        var mutability = await CheckGlobalMutabilityAsync(ct);
        if (!mutability.IsValid)
            return new TeamCreateResult.Rejected(mutability);

        var team = new Team(name);
        var member = TeamMember.ForHuman(team.Id, creatorUserId);
        db.Teams.Add(team);
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Team {TeamId} ({Name}) created by user {UserId}", team.Id, team.Name, creatorUserId);
        return new TeamCreateResult.Success(await ProjectAsync(team.Id, ct));
    }

    public async Task<TeamView?> GetMyTeamAsync(Guid userId, CancellationToken ct)
    {
        var membership = await db.TeamMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (membership is null) return null;
        return await ProjectAsync(membership.TeamId, ct);
    }

    public async Task<TeamPatchResult> PatchAsync(Guid actingUserId, PatchTeamCommand cmd, CancellationToken ct)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == cmd.TeamId, ct);
        if (team is null) return new TeamPatchResult.NotFound();

        var isMember = await db.TeamMembers.AnyAsync(m => m.TeamId == team.Id && m.UserId == actingUserId, ct);
        if (!isMember) return new TeamPatchResult.NotMember();

        var mutability = await CheckMutabilityAsync(team, ct);
        if (!mutability.IsValid) return new TeamPatchResult.Rejected(mutability);

        if (cmd.Name is not null)
        {
            var nameValidation = TeamRulesValidator.ValidateName(cmd.Name);
            if (!nameValidation.IsValid) return new TeamPatchResult.Rejected(nameValidation);
            team.Rename(cmd.Name);
        }

        if (cmd.ClearAiMode)
            team.SetAiMode(null);
        else if (cmd.AiMode.HasValue)
            team.SetAiMode(cmd.AiMode.Value);

        await db.SaveChangesAsync(ct);
        return new TeamPatchResult.Success(await ProjectAsync(team.Id, ct));
    }

    public async Task<TeamLeaveResult> LeaveAsync(Guid actingUserId, Guid teamId, CancellationToken ct)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team is null) return new TeamLeaveResult.NotFound();

        var membership = await db.TeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == actingUserId, ct);
        if (membership is null) return new TeamLeaveResult.NotMember();

        var mutability = await CheckMutabilityAsync(team, ct);
        if (!mutability.IsValid) return new TeamLeaveResult.Rejected(mutability);

        db.TeamMembers.Remove(membership);

        // If this was the last human, also remove any AI slot and the team itself.
        // Otherwise an empty (or AI-only) team would linger.
        var remainingHumans = await db.TeamMembers
            .CountAsync(m => m.TeamId == teamId && m.Id != membership.Id && !m.IsAi, ct);
        if (remainingHumans == 0)
        {
            var stragglers = await db.TeamMembers
                .Where(m => m.TeamId == teamId && m.Id != membership.Id)
                .ToListAsync(ct);
            db.TeamMembers.RemoveRange(stragglers);
            db.Teams.Remove(team);
            logger.LogInformation("Team {TeamId} removed after last human left", teamId);
        }

        await db.SaveChangesAsync(ct);
        return new TeamLeaveResult.Success();
    }

    public async Task<AddAiMemberResult> AddAiMemberAsync(Guid actingUserId, AddAiMemberCommand cmd, CancellationToken ct)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == cmd.TeamId, ct);
        if (team is null) return new AddAiMemberResult.NotFound();

        var isMember = await db.TeamMembers.AnyAsync(m => m.TeamId == team.Id && m.UserId == actingUserId, ct);
        if (!isMember) return new AddAiMemberResult.NotMember();

        var mutability = await CheckMutabilityAsync(team, ct);
        if (!mutability.IsValid) return new AddAiMemberResult.Rejected(mutability);

        var nameValidation = TeamRulesValidator.ValidateName(cmd.DisplayName);
        if (!nameValidation.IsValid) return new AddAiMemberResult.Rejected(nameValidation);

        var memberCounts = await db.TeamMembers
            .Where(m => m.TeamId == team.Id)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Ai = g.Count(m => m.IsAi) })
            .FirstOrDefaultAsync(ct) ?? new { Total = 0, Ai = 0 };

        var capacity = TeamRulesValidator.ValidateAddMember(memberCounts.Total, memberCounts.Ai, isAi: true);
        if (!capacity.IsValid) return new AddAiMemberResult.Rejected(capacity);

        db.TeamMembers.Add(TeamMember.ForAi(team.Id, cmd.DisplayName));
        team.SetAiMode(cmd.Mode);
        await db.SaveChangesAsync(ct);

        return new AddAiMemberResult.Success(await ProjectAsync(team.Id, ct));
    }

    public async Task<CreateInviteResult> CreateInviteAsync(Guid actingUserId, Guid teamId, CancellationToken ct)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team is null) return new CreateInviteResult.NotFound();

        var isMember = await db.TeamMembers.AnyAsync(m => m.TeamId == team.Id && m.UserId == actingUserId, ct);
        if (!isMember) return new CreateInviteResult.NotMember();

        var mutability = await CheckMutabilityAsync(team, ct);
        if (!mutability.IsValid) return new CreateInviteResult.Rejected(mutability);

        var token = GenerateToken();
        var invite = new TeamInvite(team.Id, token, actingUserId, DateTimeOffset.UtcNow.Add(InviteTtl));
        db.TeamInvites.Add(invite);
        await db.SaveChangesAsync(ct);

        return new CreateInviteResult.Success(new InviteView(invite.Token, invite.ExpiresAt));
    }

    public async Task<JoinResult> JoinByTokenAsync(Guid actingUserId, string token, CancellationToken ct)
    {
        var invite = await db.TeamInvites.FirstOrDefaultAsync(i => i.Token == token, ct);
        if (invite is null) return new JoinResult.InviteNotFound();
        if (!invite.IsActive(DateTimeOffset.UtcNow)) return new JoinResult.InviteExpiredOrUsed();

        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == invite.TeamId, ct);
        if (team is null) return new JoinResult.InviteNotFound();

        var mutability = await CheckMutabilityAsync(team, ct);
        if (!mutability.IsValid) return new JoinResult.Rejected(mutability);

        var alreadyMember = await db.TeamMembers.AnyAsync(m => m.UserId == actingUserId, ct);
        if (alreadyMember) return new JoinResult.AlreadyInTeam();

        var memberCounts = await db.TeamMembers
            .Where(m => m.TeamId == team.Id)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Ai = g.Count(m => m.IsAi) })
            .FirstOrDefaultAsync(ct) ?? new { Total = 0, Ai = 0 };

        var capacity = TeamRulesValidator.ValidateAddMember(memberCounts.Total, memberCounts.Ai, isAi: false);
        if (!capacity.IsValid) return new JoinResult.Rejected(capacity);

        db.TeamMembers.Add(TeamMember.ForHuman(team.Id, actingUserId));
        invite.Redeem(actingUserId);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} joined team {TeamId} via invite {Token}", actingUserId, team.Id, token);
        return new JoinResult.Success(await ProjectAsync(team.Id, ct));
    }

    private async Task<TeamValidationResult> CheckMutabilityAsync(Team team, CancellationToken ct)
    {
        var tournamentStart = await db.Tournaments
            .OrderBy(t => t.StartsAtUtc)
            .Select(t => (DateTimeOffset?)t.StartsAtUtc)
            .FirstOrDefaultAsync(ct);
        return TeamRulesValidator.ValidateMutable(DateTimeOffset.UtcNow, tournamentStart, team.Status);
    }

    private async Task<TeamValidationResult> CheckGlobalMutabilityAsync(CancellationToken ct)
    {
        // For creating a new team there's no existing status to gate; only the
        // tournament-start lock matters. Pass Forming so the status branch is a no-op.
        return await CheckMutabilityAsync(new Team("placeholder"), ct);
    }

    private static string GenerateToken()
    {
        // 24 bytes → 32 base64url chars, well under the 64-char column cap.
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private async Task<TeamView> ProjectAsync(Guid teamId, CancellationToken ct)
    {
        var team = await db.Teams.AsNoTracking().FirstAsync(t => t.Id == teamId, ct);
        var rows = await (
            from m in db.TeamMembers.AsNoTracking().Where(m => m.TeamId == teamId)
            from u in db.Users.AsNoTracking().Where(u => u.Id == m.UserId).DefaultIfEmpty()
            orderby m.JoinedAt
            select new
            {
                m,
                UserName = u != null ? u.DisplayName : null,
                UserAvatarVersion = u != null ? u.AvatarVersion : null,
            }).ToListAsync(ct);

        var members = rows.Select(r => new TeamMemberView(
            Id: r.m.Id,
            UserId: r.m.UserId,
            DisplayName: r.m.IsAi ? r.m.AiDisplayName! : (r.UserName ?? "?"),
            AvatarVersion: r.m.IsAi ? null : r.UserAvatarVersion,
            IsAi: r.m.IsAi,
            JoinedAt: r.m.JoinedAt)).ToList();

        return new TeamView(team.Id, team.Name, team.Status, team.AiMode, team.CreatedAt, members);
    }
}
