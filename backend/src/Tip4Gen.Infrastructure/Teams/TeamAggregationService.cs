using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Teams;

public record MemberBreakdownView(
    Guid MemberId,
    Guid? UserId,
    string DisplayName,
    bool IsAi,
    int Points,
    bool Dropped);

public record TeamMatchBreakdownView(
    Guid TeamId,
    string TeamName,
    string? TeamAvatarVersion,
    Guid MatchId,
    int TotalPoints,
    IReadOnlyList<MemberBreakdownView> Members);

public abstract record TeamMatchBreakdownResult
{
    public sealed record Success(TeamMatchBreakdownView View) : TeamMatchBreakdownResult;
    public sealed record TeamNotFound : TeamMatchBreakdownResult;
    public sealed record MatchNotFound : TeamMatchBreakdownResult;
    public sealed record TeamNotLocked(TeamStatus Status) : TeamMatchBreakdownResult;
}

public interface ITeamAggregationService
{
    Task<TeamMatchBreakdownResult> GetMatchBreakdownAsync(Guid teamId, Guid matchId, CancellationToken ct);
}

public class TeamAggregationService(AppDbContext db) : ITeamAggregationService
{
    public async Task<TeamMatchBreakdownResult> GetMatchBreakdownAsync(Guid teamId, Guid matchId, CancellationToken ct)
    {
        var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team is null) return new TeamMatchBreakdownResult.TeamNotFound();

        // Only Locked teams are valid for aggregation per guide §7.
        if (team.Status != TeamStatus.Locked)
            return new TeamMatchBreakdownResult.TeamNotLocked(team.Status);

        var matchExists = await db.Matches.AnyAsync(m => m.Id == matchId, ct);
        if (!matchExists) return new TeamMatchBreakdownResult.MatchNotFound();

        // Members with their display name (joined to users for humans, fallback to ai_display_name).
        var memberRows = await (
            from m in db.TeamMembers.AsNoTracking().Where(m => m.TeamId == teamId)
            from u in db.Users.AsNoTracking().Where(u => u.Id == m.UserId).DefaultIfEmpty()
            orderby m.JoinedAt
            select new
            {
                m.Id,
                m.UserId,
                m.IsAi,
                m.AiDisplayName,
                HumanName = u != null ? u.DisplayName : null,
            }).ToListAsync(ct);

        // Pull all scored_tips for this match in one query. Tips come in two flavours:
        // human (UserId set) and AI (TeamMemberId set) — exactly one is non-null per row.
        var scoredRows = await db.ScoredTips.AsNoTracking()
            .Where(s => s.MatchId == matchId)
            .Select(s => new { s.UserId, s.TeamMemberId, s.FinalPoints })
            .ToListAsync(ct);

        var scoredByUser = scoredRows
            .Where(s => s.UserId.HasValue)
            .ToDictionary(s => s.UserId!.Value, s => s.FinalPoints);
        var scoredByMember = scoredRows
            .Where(s => s.TeamMemberId.HasValue)
            .ToDictionary(s => s.TeamMemberId!.Value, s => s.FinalPoints);

        var pointsByMember = memberRows.ToDictionary(
            r => r.Id,
            r => r.UserId is Guid uid
                ? (scoredByUser.TryGetValue(uid, out var hPts) ? hPts : 0)
                : (scoredByMember.TryGetValue(r.Id, out var aPts) ? aPts : 0));

        var inputs = memberRows
            .Select(r => new TeamAggregator.MemberPoints(r.Id, pointsByMember[r.Id]))
            .ToList();

        // Defensive: only Locked teams reach here; they must have MaxMembers entries.
        if (inputs.Count != Team.MaxMembers)
            return new TeamMatchBreakdownResult.TeamNotLocked(team.Status);

        var aggregate = TeamAggregator.ForMatch(inputs);
        var droppedByMember = aggregate.Members.ToDictionary(a => a.MemberId, a => a.Dropped);

        var view = new TeamMatchBreakdownView(
            team.Id,
            team.Name,
            team.AvatarVersion,
            matchId,
            aggregate.TotalPoints,
            memberRows.Select(r => new MemberBreakdownView(
                r.Id,
                r.UserId,
                r.IsAi ? r.AiDisplayName! : (r.HumanName ?? "?"),
                r.IsAi,
                pointsByMember[r.Id],
                droppedByMember[r.Id])).ToList());

        return new TeamMatchBreakdownResult.Success(view);
    }
}
