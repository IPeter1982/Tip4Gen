using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Infrastructure.Teams;

namespace Tip4Gen.Api.Controllers;

public record AdminTeamRenameRequest(string Name, string? Reason);

public record AdminTeamDeleteResponse(Guid TeamId, string Name, int MembersRemoved);
public record AdminTeamMemberRemoveResponse(
    Guid TeamId,
    Guid RemovedMemberId,
    bool TeamCascadeDeleted,
    bool StatusRevertedToForming,
    TeamAdminView? Team);

[ApiController]
[Route("api/admin/teams")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class TeamsAdminController(
    CurrentUserService currentUser,
    ITeamLockService teamLock,
    ITeamsAdminService teamsAdmin) : ControllerBase
{
    /// <summary>
    /// Manual trigger for the tournament-start lock pass. Useful when the Workers
    /// process isn't running, or to force-flip Forming teams immediately.
    /// Idempotent — re-running after a successful pass returns zeros.
    /// </summary>
    [HttpPost("lock")]
    public async Task<IActionResult> Lock(CancellationToken ct)
    {
        var summary = await teamLock.LockAllAsync(ct);
        return Ok(summary);
    }

    /// <summary>Lists every team (all statuses) with full member roster for the admin overview.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var teams = await teamsAdmin.ListAsync(ct);
        return Ok(teams);
    }

    /// <summary>
    /// Rename a team. Bypasses the Forming-only mutability check that user-facing PATCH applies.
    /// </summary>
    [HttpPut("{teamId:guid}/name")]
    public async Task<IActionResult> Rename(Guid teamId, [FromBody] AdminTeamRenameRequest body, CancellationToken ct)
    {
        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await teamsAdmin.RenameAsync(
            new AdminRenameTeamCommand(admin.Id, teamId, body.Name, body.Reason), ct);

        return result switch
        {
            TeamAdminRenameResult.Success s => Ok(s.Team),

            TeamAdminRenameResult.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Team not found",
                detail: $"No team with id {teamId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "TeamNotFound" }),

            TeamAdminRenameResult.NameInvalid ni => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Invalid name",
                detail: ni.Message,
                extensions: new Dictionary<string, object?> { ["reason"] = "NameInvalid" }),

            _ => throw new InvalidOperationException($"Unhandled TeamAdminRenameResult: {result.GetType().Name}"),
        };
    }

    /// <summary>
    /// Delete a team. team_members + team_invites cascade via DB FK; AI tips/scored_tips
    /// cascade via team_members.id. Human tips/scored_tips persist (keyed by user_id).
    /// </summary>
    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> Delete(Guid teamId, [FromQuery] string? reason, CancellationToken ct)
    {
        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await teamsAdmin.DeleteAsync(
            new AdminDeleteTeamCommand(admin.Id, teamId, reason), ct);

        return result switch
        {
            TeamAdminDeleteResult.Success s => Ok(new AdminTeamDeleteResponse(s.TeamId, s.Name, s.MembersRemoved)),

            TeamAdminDeleteResult.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Team not found",
                detail: $"No team with id {teamId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "TeamNotFound" }),

            _ => throw new InvalidOperationException($"Unhandled TeamAdminDeleteResult: {result.GetType().Name}"),
        };
    }

    /// <summary>
    /// Remove a single member. If removing leaves no humans, the whole team cascade-deletes.
    /// If the team was Locked and now has fewer than MaxMembers, status reverts to Forming.
    /// </summary>
    [HttpDelete("{teamId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid teamId,
        Guid memberId,
        [FromQuery] string? reason,
        CancellationToken ct)
    {
        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await teamsAdmin.RemoveMemberAsync(
            new AdminRemoveMemberCommand(admin.Id, teamId, memberId, reason), ct);

        return result switch
        {
            TeamMemberRemoveResult.Success s => Ok(new AdminTeamMemberRemoveResponse(
                TeamId: s.Team.Id,
                RemovedMemberId: memberId,
                TeamCascadeDeleted: false,
                StatusRevertedToForming: s.StatusRevertedToForming,
                Team: s.Team)),

            TeamMemberRemoveResult.TeamCascadeDeleted cd => Ok(new AdminTeamMemberRemoveResponse(
                TeamId: cd.TeamId,
                RemovedMemberId: cd.RemovedMemberId,
                TeamCascadeDeleted: true,
                StatusRevertedToForming: false,
                Team: null)),

            TeamMemberRemoveResult.TeamNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Team not found",
                detail: $"No team with id {teamId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "TeamNotFound" }),

            TeamMemberRemoveResult.MemberNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Member not found",
                detail: $"No member with id {memberId} on team {teamId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "MemberNotFound" }),

            _ => throw new InvalidOperationException($"Unhandled TeamMemberRemoveResult: {result.GetType().Name}"),
        };
    }
}
