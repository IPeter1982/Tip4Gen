using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Leaderboard;
using Tip4Gen.Domain.Scoring;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Leaderboard;

public sealed record IndividualLeaderboardRow(
    int Rank,
    Guid UserId,
    string DisplayName,
    int TotalPoints,
    int ExactCount,
    bool? WinnerCorrect,
    bool? TopScorerCorrect,
    int LongestStreak,
    bool IsMe);

public interface IIndividualLeaderboardService
{
    Task<IReadOnlyList<IndividualLeaderboardRow>> GetAsync(Guid? currentUserId, CancellationToken ct);
}

/// <summary>
/// Direct-query leaderboard. At 200 users × 64 matches = ~12,800 rows of scored_tips,
/// a single SELECT per data type then in-memory grouping is fast enough; we deliberately
/// skip the materialized-view optimization the plan flags as [CUT-OK].
///
/// Long-term tip outcomes (winner / top scorer) aren't recorded yet — Phase 8 will add
/// the admin entry endpoint. Until then we pass nulls and the §9 tiebreakers for those
/// rules act as neutral (handled inside LeaderboardRanker).
/// </summary>
public class IndividualLeaderboardService(AppDbContext db) : IIndividualLeaderboardService
{
    public async Task<IReadOnlyList<IndividualLeaderboardRow>> GetAsync(Guid? currentUserId, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(ct);

        // Only human scored tips count toward the individual board — AI tips key on
        // team_member_id and are excluded here. (§7: AI is team-aggregated only.)
        var scoredRows = await (
            from s in db.ScoredTips.AsNoTracking()
            join m in db.Matches.AsNoTracking() on s.MatchId equals m.Id
            where s.UserId != null
            orderby s.UserId, m.KickoffUtc
            select new { UserId = s.UserId!.Value, s.FinalPoints, s.Category, m.KickoffUtc }).ToListAsync(ct);

        var byUser = scoredRows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.KickoffUtc).ToList());

        var entries = users.Select(u =>
        {
            byUser.TryGetValue(u.Id, out var rows);
            rows ??= [];

            var total = rows.Sum(r => r.FinalPoints);
            var exact = rows.Count(r => r.Category == ScoreCategory.Exact);
            var streak = StreakCalculator.LongestStreak(rows.Select(r => r.FinalPoints));

            return new LeaderboardEntry(
                UserId: u.Id,
                DisplayName: u.DisplayName,
                TotalPoints: total,
                ExactCount: exact,
                WinnerCorrect: null,
                TopScorerCorrect: null,
                LongestStreak: streak);
        });

        var ranked = LeaderboardRanker.Rank(entries);

        return ranked.Select(r => new IndividualLeaderboardRow(
            Rank: r.Rank,
            UserId: r.Entry.UserId,
            DisplayName: r.Entry.DisplayName,
            TotalPoints: r.Entry.TotalPoints,
            ExactCount: r.Entry.ExactCount,
            WinnerCorrect: r.Entry.WinnerCorrect,
            TopScorerCorrect: r.Entry.TopScorerCorrect,
            LongestStreak: r.Entry.LongestStreak,
            IsMe: currentUserId.HasValue && r.Entry.UserId == currentUserId.Value)).ToList();
    }
}
