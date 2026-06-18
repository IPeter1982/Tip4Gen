using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Api.Avatars;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Infrastructure.Teams;

namespace Tip4Gen.Api.Controllers;

public record CreateTeamRequest(string Name);
public record PatchTeamRequest(string? Name);
public record SetTeamAiModeRequest(AiMode Mode);
public record AddAiMemberRequest(string DisplayName, AiMode Mode);
public record SetTeamAvatarRequest(string DataUrl);

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

    [HttpGet]
    public async Task<IActionResult> ListAll(CancellationToken ct)
    {
        var list = await teams.ListAllAsync(ct);
        return Ok(list);
    }

    [HttpPatch("{teamId:guid}")]
    public async Task<IActionResult> Patch(Guid teamId, [FromBody] PatchTeamRequest request, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var cmd = new PatchTeamCommand(teamId, request.Name);
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

    /// <summary>
    /// Update the AI tipping mode for a team. Separate from <see cref="Patch"/> because
    /// this endpoint is allowed even after the team locks / the tournament starts —
    /// only Disqualified teams are rejected.
    /// </summary>
    [HttpPut("{teamId:guid}/ai-mode")]
    public async Task<IActionResult> SetAiMode(Guid teamId, [FromBody] SetTeamAiModeRequest request, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var result = await teams.SetAiModeAsync(user.Id, teamId, request.Mode, ct);
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

    [HttpPut("{teamId:guid}/avatar")]
    public async Task<IActionResult> SetAvatar(Guid teamId, [FromBody] SetTeamAvatarRequest request, CancellationToken ct)
    {
        if (!DataUrlParser.TryParse(request.DataUrl, out var contentType, out var bytes) || bytes is null || contentType is null)
            return Rejected(TeamValidationResult.Fail(
                TeamRejectionReason.AvatarInvalidDataUrl,
                "Érvénytelen kép-adat."));

        var user = await currentUser.GetOrCreateAsync(ct);
        var result = await teams.SetAvatarAsync(user.Id, teamId, bytes, contentType, ct);
        return result switch
        {
            TeamPatchResult.Success s => Ok(s.Team),
            TeamPatchResult.NotFound => NotFound(),
            TeamPatchResult.NotMember => Forbid(),
            TeamPatchResult.Rejected r => Rejected(r.Validation),
            _ => throw new InvalidOperationException($"Unhandled TeamPatchResult: {result.GetType().Name}"),
        };
    }

    [HttpDelete("{teamId:guid}/avatar")]
    public async Task<IActionResult> DeleteAvatar(Guid teamId, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var result = await teams.ClearAvatarAsync(user.Id, teamId, ct);
        return result switch
        {
            TeamPatchResult.Success s => Ok(s.Team),
            TeamPatchResult.NotFound => NotFound(),
            TeamPatchResult.NotMember => Forbid(),
            TeamPatchResult.Rejected r => Rejected(r.Validation),
            _ => throw new InvalidOperationException($"Unhandled TeamPatchResult: {result.GetType().Name}"),
        };
    }

    /// <summary>
    /// Public avatar binary. Anonymous because &lt;img src&gt; can't carry Bearer tokens.
    /// Cache-Control: immutable for a day; URL changes when the version (?v=) changes.
    /// </summary>
    [HttpGet("{teamId:guid}/avatar")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvatar(Guid teamId, CancellationToken ct)
    {
        var avatar = await teams.GetAvatarBytesAsync(teamId, ct);
        if (avatar is null) return NotFound();
        Response.Headers.CacheControl = "public, max-age=86400, immutable";
        Response.Headers.ETag = $"\"{avatar.Version}\"";
        return File(avatar.Bytes, avatar.ContentType);
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

    [HttpPost("{teamId:guid}/join")]
    public async Task<IActionResult> JoinDirect(Guid teamId, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var result = await teams.JoinDirectlyAsync(user.Id, teamId, ct);
        return result switch
        {
            JoinDirectResult.Success s => Ok(s.Team),
            JoinDirectResult.NotFound => NotFoundProblem(
                "A csapat nem található.",
                reason: "TeamNotFound"),
            JoinDirectResult.AlreadyInTeam => Conflict(
                "Már tagja vagy egy másik csapatnak.",
                reason: "AlreadyInTeam"),
            JoinDirectResult.Rejected r => Rejected(r.Validation),
            _ => throw new InvalidOperationException($"Unhandled JoinDirectResult: {result.GetType().Name}"),
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

    private ObjectResult NotFoundProblem(string detail, string reason) =>
        (ObjectResult)Problem(
            statusCode: StatusCodes.Status404NotFound,
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
        TeamRejectionReason.AvatarMissing => "Avatar missing",
        TeamRejectionReason.AvatarUnsupportedFormat => "Avatar format unsupported",
        TeamRejectionReason.AvatarTooLarge => "Avatar too large",
        TeamRejectionReason.AvatarInvalidDataUrl => "Avatar data URL invalid",
        _ => "Team request rejected",
    };
}
