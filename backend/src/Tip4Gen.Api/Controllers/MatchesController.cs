using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Tipping;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Controllers;

public record TeamSummary(Guid Id, string Name, string? Code);

public record MyTip(
    int HomeGoals,
    int AwayGoals,
    bool Joker,
    DateTimeOffset SubmittedAt,
    DateTimeOffset UpdatedAt);

public record UpcomingMatchResponse(
    Guid Id,
    string Stage,
    string? GroupCode,
    string? RoundLabel,
    TeamSummary HomeTeam,
    TeamSummary AwayTeam,
    DateTimeOffset KickoffUtc,
    DateTimeOffset DeadlineUtc,
    string Status,
    int? HomeGoals,
    int? AwayGoals,
    MyTip? MyTip);

public record MatchTipsResponse(
    Guid MatchId,
    DateTimeOffset DeadlineUtc,
    bool DeadlinePassed,
    int TipCount,
    IReadOnlyList<MatchTipResponse> Tips);

public record MatchTipResponse(
    Guid UserId,
    string DisplayName,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? UpdatedAt,
    int? HomeGoals,
    int? AwayGoals,
    bool? Joker);

[ApiController]
[Route("api/matches")]
[Authorize]
public class MatchesController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? phase, CancellationToken ct)
    {
        var phaseFilter = (phase ?? "upcoming").ToLowerInvariant();
        if (phaseFilter is not ("upcoming" or "past" or "all"))
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid phase",
                detail: "phase must be one of: upcoming, past, all.");

        var user = await currentUser.GetOrCreateAsync(ct);

        var query = db.Matches.AsNoTracking();
        query = phaseFilter switch
        {
            "upcoming" => query.Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.Live),
            "past" => query.Where(m => m.Status == MatchStatus.Finished
                                       || m.Status == MatchStatus.Abandoned
                                       || m.Status == MatchStatus.Awarded
                                       || m.Status == MatchStatus.Cancelled),
            _ => query,
        };

        // Pull match + teams + this user's tip in a single round-trip.
        var rows = await (
            from m in query
            join h in db.NationalTeams.AsNoTracking() on m.HomeTeamId equals h.Id
            join a in db.NationalTeams.AsNoTracking() on m.AwayTeamId equals a.Id
            from t in db.Tips.AsNoTracking()
                .Where(t => t.MatchId == m.Id && t.UserId == user.Id).DefaultIfEmpty()
            orderby m.KickoffUtc
            select new
            {
                Match = m,
                Home = h,
                Away = a,
                MyTip = t,
            }).ToListAsync(ct);

        var response = rows.Select(r => new UpcomingMatchResponse(
            r.Match.Id,
            r.Match.Stage.ToString(),
            r.Match.GroupCode,
            r.Match.RoundLabel,
            new TeamSummary(r.Home.Id, r.Home.Name, r.Home.Code),
            new TeamSummary(r.Away.Id, r.Away.Name, r.Away.Code),
            r.Match.KickoffUtc,
            r.Match.KickoffUtc - TipRulesValidator.DeadlineBeforeKickoff,
            r.Match.Status.ToString(),
            r.Match.HomeGoals,
            r.Match.AwayGoals,
            r.MyTip is null
                ? null
                : new MyTip(r.MyTip.HomeGoals, r.MyTip.AwayGoals, r.MyTip.Joker, r.MyTip.SubmittedAt, r.MyTip.UpdatedAt)
        )).ToList();

        return Ok(response);
    }

    [HttpGet("{matchId:guid}/tips")]
    public async Task<IActionResult> Tips(Guid matchId, CancellationToken ct)
    {
        var match = await db.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId, ct);
        if (match is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Match not found",
                detail: $"No match with id {matchId}.");
        }

        var deadline = match.KickoffUtc - TipRulesValidator.DeadlineBeforeKickoff;
        var deadlinePassed = DateTimeOffset.UtcNow >= deadline;

        var rows = await (
            from t in db.Tips.AsNoTracking().Where(t => t.MatchId == matchId)
            join u in db.Users.AsNoTracking() on t.UserId equals u.Id
            orderby u.DisplayName
            select new { Tip = t, User = u }).ToListAsync(ct);

        var tips = rows.Select(r => deadlinePassed
            ? new MatchTipResponse(r.User.Id, r.User.DisplayName, r.Tip.SubmittedAt, r.Tip.UpdatedAt,
                r.Tip.HomeGoals, r.Tip.AwayGoals, r.Tip.Joker)
            : new MatchTipResponse(r.User.Id, r.User.DisplayName, r.Tip.SubmittedAt, null,
                null, null, null)).ToList();

        return Ok(new MatchTipsResponse(matchId, deadline, deadlinePassed, tips.Count, tips));
    }
}
