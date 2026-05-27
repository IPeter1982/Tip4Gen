using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Leaderboard;

public sealed record TeamLeaderboardMember(Guid MemberId, string DisplayName, bool IsAi, int Points);

public sealed record TeamLeaderboardRow(
    int Rank,
    Guid TeamId,
    string TeamName,
    int TotalPoints,
    IReadOnlyList<TeamLeaderboardMember> Members,
    bool IsMyTeam);

public interface ITeamLeaderboardService
{
    Task<IReadOnlyList<TeamLeaderboardRow>> GetAsync(Guid? currentUserId, CancellationToken ct);
}

/// <summary>
/// Team leaderboard, per guide §8: each match's contribution is the best 3 of 4 member
/// scores; the sum across matches is the team total. Only Locked teams compete in the
/// team rankings — Disqualified teams' members still appear on the individual board
/// (per §7) but the team itself is excluded.
///
/// Tiebreakers: §9 spells out individual tiebreakers (exact count, long-tip correctness,
/// streak). The guide doesn't define team-level tiebreakers, so we go with the safest
/// reading: tied total points → shared placement. If a stricter rule emerges, the ranker
/// can be lifted into Domain and the per-team aggregate signals added.
/// </summary>
public class TeamLeaderboardService(AppDbContext db) : ITeamLeaderboardService
{
    public async Task<IReadOnlyList<TeamLeaderboardRow>> GetAsync(Guid? currentUserId, CancellationToken ct)
    {
        var teams = await db.Teams.AsNoTracking()
            .Where(t => t.Status == TeamStatus.Locked)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);

        if (teams.Count == 0) return [];

        var teamIds = teams.Select(t => t.Id).ToList();

        var memberRows = await (
            from m in db.TeamMembers.AsNoTracking().Where(m => teamIds.Contains(m.TeamId))
            from u in db.Users.AsNoTracking().Where(u => u.Id == m.UserId).DefaultIfEmpty()
            orderby m.TeamId, m.JoinedAt
            select new
            {
                m.Id,
                m.TeamId,
                m.UserId,
                m.IsAi,
                m.AiDisplayName,
                HumanName = u != null ? u.DisplayName : null,
            }).ToListAsync(ct);

        var membersByTeam = memberRows.GroupBy(r => r.TeamId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var userIds = memberRows.Where(r => r.UserId.HasValue).Select(r => r.UserId!.Value).Distinct().ToList();
        var aiMemberIds = memberRows.Where(r => r.IsAi).Select(r => r.Id).Distinct().ToList();

        // Pull scored tips for any member of these teams in one round-trip. Each row has
        // exactly one of (UserId, TeamMemberId) set — we build two parallel lookups so
        // the match-aggregation loop below can resolve either flavour by member type.
        var scoredRows = (userIds.Count == 0 && aiMemberIds.Count == 0)
            ? []
            : await db.ScoredTips.AsNoTracking()
                .Where(s => (s.UserId != null && userIds.Contains(s.UserId.Value))
                         || (s.TeamMemberId != null && aiMemberIds.Contains(s.TeamMemberId.Value)))
                .Select(s => new { s.UserId, s.TeamMemberId, s.MatchId, s.FinalPoints })
                .ToListAsync(ct);

        var userMatchPoints = scoredRows
            .Where(s => s.UserId.HasValue)
            .ToDictionary(s => (UserId: s.UserId!.Value, s.MatchId), s => s.FinalPoints);
        var memberMatchPoints = scoredRows
            .Where(s => s.TeamMemberId.HasValue)
            .ToDictionary(s => (MemberId: s.TeamMemberId!.Value, s.MatchId), s => s.FinalPoints);

        var totalByMember = memberRows.ToDictionary(r => r.Id, _ => 0);
        var teamTotals = new Dictionary<Guid, int>();

        foreach (var team in teams)
        {
            var members = membersByTeam.GetValueOrDefault(team.Id) ?? [];
            if (members.Count != Team.MaxMembers)
            {
                // A Locked team must have 4 members per TeamLockPolicy. If it doesn't,
                // skip it from team scoring (the individual board still counts the members).
                teamTotals[team.Id] = 0;
                continue;
            }

            // Find every match where any member (human or AI) of this team has a
            // scored tip — that's the set of matches the team competes on.
            var humanMatchIds = members
                .Where(m => m.UserId.HasValue)
                .SelectMany(m => scoredRows.Where(s => s.UserId == m.UserId!.Value).Select(s => s.MatchId));
            var aiMatchIds = members
                .Where(m => m.IsAi)
                .SelectMany(m => scoredRows.Where(s => s.TeamMemberId == m.Id).Select(s => s.MatchId));
            var matchIds = humanMatchIds.Concat(aiMatchIds).Distinct().ToList();

            int teamTotal = 0;
            foreach (var matchId in matchIds)
            {
                var input = members.Select(m =>
                {
                    int pts = 0;
                    if (m.UserId.HasValue && userMatchPoints.TryGetValue((m.UserId.Value, matchId), out var hPts))
                        pts = hPts;
                    else if (m.IsAi && memberMatchPoints.TryGetValue((m.Id, matchId), out var aPts))
                        pts = aPts;
                    return new TeamAggregator.MemberPoints(m.Id, pts);
                }).ToList();

                var aggregate = TeamAggregator.ForMatch(input);
                teamTotal += aggregate.TotalPoints;

                foreach (var memberAgg in aggregate.Members)
                {
                    if (!memberAgg.Dropped)
                        totalByMember[memberAgg.MemberId] += memberAgg.Points;
                }
            }

            teamTotals[team.Id] = teamTotal;
        }

        // Determine which team the current user is in (if any) so we can flag isMyTeam.
        var myTeamId = currentUserId.HasValue
            ? memberRows.FirstOrDefault(r => r.UserId == currentUserId.Value)?.TeamId
            : null;

        // Rank: descending by total, shared placement on ties, secondary sort by name.
        var ordered = teams
            .OrderByDescending(t => teamTotals[t.Id])
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        var rows = new List<TeamLeaderboardRow>(ordered.Count);
        int rank = 0;
        int? prevTotal = null;
        for (int i = 0; i < ordered.Count; i++)
        {
            var team = ordered[i];
            var total = teamTotals[team.Id];
            if (prevTotal is null || prevTotal != total)
                rank = i + 1;

            var memberViews = (membersByTeam.GetValueOrDefault(team.Id) ?? [])
                .Select(m => new TeamLeaderboardMember(
                    m.Id,
                    m.IsAi ? m.AiDisplayName! : (m.HumanName ?? "?"),
                    m.IsAi,
                    totalByMember.GetValueOrDefault(m.Id)))
                .ToList();

            rows.Add(new TeamLeaderboardRow(
                Rank: rank,
                TeamId: team.Id,
                TeamName: team.Name,
                TotalPoints: total,
                Members: memberViews,
                IsMyTeam: myTeamId == team.Id));

            prevTotal = total;
        }
        return rows;
    }
}
