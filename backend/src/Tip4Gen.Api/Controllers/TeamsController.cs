using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Infrastructure.Teams;

namespace Tip4Gen.Api.Controllers;

public record CreateTeamRequest(string Name);
public record PatchTeamRequest(string? Name, AiMode? AiMode, bool ClearAiMode = false);
public record AddAiMemberRequest(string DisplayName, AiMode Mode);

[ApiController]
[Route("api/teams")]
[Authorize]
public class TeamsController(
    CurrentUserService currentUser,
    ITeamsService teams,
    ITeamAggregationService aggregation) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var result = await teams.CreateAsync(user.Id, request.Name, ct);
        return result switch
        {
            TeamCreateResult.Success s => CreatedAtAction(nameof(GetMine), null, s.Team),
            TeamCreateResult.AlreadyInTeam => Conflict(
                "Már tagja vagy egy másik csapatnak.",
                reason: "AlreadyInTeam"),
            TeamCreateResult.Rejected r => Rejected(r.Validation),
            _ => throw new InvalidOperationException($"Unhandled TeamCreateResult: {result.GetType().Name}"),
        };
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var team = await teams.GetMyTeamAsync(user.Id, ct);
        return team is null ? NoContent() : Ok(team);
    }

    [HttpPatch("{teamId:guid}")]
    public async Task<IActionResult> Patch(Guid teamId, [FromBody] PatchTeamRequest request, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var cmd = new PatchTeamCommand(teamId, request.Name, request.AiMode, request.ClearAiMode);
        var result = await teams.PatchAsync(user.Id, cmd, ct);
        return result switch
        {
            TeamPatchResult.Success s => Ok(s.Team),
            TeamPatchResult.NotFound => NotFound(),
            TeamPatchResult.NotMember => Forbid(),
            TeamPatchResult.Rejected r => Rejected(r.Validation),
            _ => throw new InvalidOperationException($"Unhandled TeamPatchResult: {result.GetType().Name}"),
        };
    }

    [HttpPost("{teamId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid teamId, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var result = await teams.LeaveAsync(user.Id, teamId, ct);
        return result switch
        {
            TeamLeaveResult.Success => NoContent(),
            TeamLeaveResult.NotFound => NotFound(),
            TeamLeaveResult.NotMember => Forbid(),
            TeamLeaveResult.Rejected r => Rejected(r.Validation),
            _ => throw new InvalidOperationException($"Unhandled TeamLeaveResult: {result.GetType().Name}"),
        };
    }

    [HttpPost("{teamId:guid}/ai-member")]
    public async Task<IActionResult> AddAi(Guid teamId, [FromBody] AddAiMemberRequest request, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var cmd = new AddAiMemberCommand(teamId, request.DisplayName, request.Mode);
        var result = await teams.AddAiMemberAsync(user.Id, cmd, ct);
        return result switch
        {
            AddAiMemberResult.Success s => Ok(s.Team),
            AddAiMemberResult.NotFound => NotFound(),
            AddAiMemberResult.NotMember => Forbid(),
            AddAiMemberResult.Rejected r => Rejected(r.Validation),
            _ => throw new InvalidOperationException($"Unhandled AddAiMemberResult: {result.GetType().Name}"),
        };
    }

    [HttpPost("{teamId:guid}/invites")]
    public async Task<IActionResult> CreateInvite(Guid teamId, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var result = await teams.CreateInviteAsync(user.Id, teamId, ct);
        return result switch
        {
            CreateInviteResult.Success s => Ok(s.Invite),
            CreateInviteResult.NotFound => NotFound(),
            CreateInviteResult.NotMember => Forbid(),
            CreateInviteResult.Rejected r => Rejected(r.Validation),
            _ => throw new InvalidOperationException($"Unhandled CreateInviteResult: {result.GetType().Name}"),
        };
    }

    [HttpGet("{teamId:guid}/matches/{matchId:guid}/breakdown")]
    public async Task<IActionResult> MatchBreakdown(Guid teamId, Guid matchId, CancellationToken ct)
    {
        var result = await aggregation.GetMatchBreakdownAsync(teamId, matchId, ct);
        return result switch
        {
            TeamMatchBreakdownResult.Success s => Ok(s.View),
            TeamMatchBreakdownResult.TeamNotFound => NotFound(),
            TeamMatchBreakdownResult.MatchNotFound => NotFound(),
            TeamMatchBreakdownResult.TeamNotLocked tnl => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Team not locked",
                detail: $"Team is in status {tnl.Status}; breakdowns are only available for Locked teams.",
                extensions: new Dictionary<string, object?>
                {
                    ["reason"] = "TeamNotLocked",
                    ["status"] = tnl.Status.ToString(),
                }),
            _ => throw new InvalidOperationException($"Unhandled TeamMatchBreakdownResult: {result.GetType().Name}"),
        };
    }

    [HttpPost("join/{token}")]
    public async Task<IActionResult> Join(string token, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var result = await teams.JoinByTokenAsync(user.Id, token, ct);
        return result switch
        {
            JoinResult.Success s => Ok(s.Team),
            JoinResult.InviteNotFound => NotFound(
                Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Invite not found",
                    detail: "A meghívó nem található.").Value),
            JoinResult.InviteExpiredOrUsed => Conflict(
                "A meghívó lejárt vagy már felhasznált.",
                reason: "InviteExpiredOrUsed"),
            JoinResult.AlreadyInTeam => Conflict(
                "Már tagja vagy egy másik csapatnak.",
                reason: "AlreadyInTeam"),
            JoinResult.Rejected r => Rejected(r.Validation),
            _ => throw new InvalidOperationException($"Unhandled JoinResult: {result.GetType().Name}"),
        };
    }

    private IActionResult Rejected(TeamValidationResult validation) =>
        Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: ReasonTitle(validation.Reason),
            detail: validation.Message,
            extensions: new Dictionary<string, object?>
            {
                ["reason"] = validation.Reason.ToString(),
            });

    private ObjectResult Conflict(string detail, string reason) =>
        (ObjectResult)Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: reason,
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["reason"] = reason,
            });

    private static string ReasonTitle(TeamRejectionReason reason) => reason switch
    {
        TeamRejectionReason.NameBlank => "Team name required",
        TeamRejectionReason.NameTooLong => "Team name too long",
        TeamRejectionReason.TournamentStarted => "Tournament started",
        TeamRejectionReason.TeamLocked => "Team locked",
        TeamRejectionReason.TeamFull => "Team full",
        TeamRejectionReason.AiSlotTaken => "AI slot taken",
        TeamRejectionReason.UserAlreadyInTeam => "Already in a team",
        _ => "Team request rejected",
    };
}
