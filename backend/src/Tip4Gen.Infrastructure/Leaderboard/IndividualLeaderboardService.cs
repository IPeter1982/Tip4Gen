using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Leaderboard;
using Tip4Gen.Domain.Scoring;
using Tip4Gen.Domain.Tipping;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Leaderboard;

public sealed record IndividualLeaderboardRow(
    int Rank,
    Guid UserId,
    string DisplayName,
    string? AvatarVersion,
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
/// Long-term tip outcomes (winner / top scorer) are read from the active tournament's
/// recorded fields (Phase 8 admin entry). If either is null, the ranker treats the
/// corresponding §9 tiebreaker as neutral.
/// </summary>
public class IndividualLeaderboardService(AppDbContext db) : IIndividualLeaderboardService
{
    public async Task<IReadOnlyList<IndividualLeaderboardRow>> GetAsync(Guid? currentUserId, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.DisplayName, u.AvatarVersion })
            .ToListAsync(ct);
        var avatarVersionByUser = users.ToDictionary(u => u.Id, u => u.AvatarVersion);

        // Outcomes from the active tournament; nullable until admin enters them.
        var outcomes = await db.Tournaments.AsNoTracking()
            .OrderByDescending(t => t.StartsAtUtc)
            .Select(t => new { t.WinnerTeamId, t.TopScorerPlayerId })
            .FirstOrDefaultAsync(ct);

        // Each user's long-term tips, so we can compare against outcomes per user.
        var longTips = await db.LongTermTips.AsNoTracking()
            .Select(l => new LongTipRow(l.UserId, l.Type, l.TargetTeamId, l.TargetPlayerId))
            .ToListAsync(ct);
        var longTipsByUser = longTips.GroupBy(l => l.UserId).ToDictionary(g => g.Key, g => g.ToList());

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

            longTipsByUser.TryGetValue(u.Id, out var userLongTips);
            var (winnerCorrect, topScorerCorrect) = ComputeLongTipCorrectness(outcomes?.WinnerTeamId, outcomes?.TopScorerPlayerId, userLongTips);

            return new LeaderboardEntry(
                UserId: u.Id,
                DisplayName: u.DisplayName,
                TotalPoints: total,
                ExactCount: exact,
                WinnerCorrect: winnerCorrect,
                TopScorerCorrect: topScorerCorrect,
                LongestStreak: streak);
        });

        var ranked = LeaderboardRanker.Rank(entries);

        return ranked.Select(r => new IndividualLeaderboardRow(
            Rank: r.Rank,
            UserId: r.Entry.UserId,
            DisplayName: r.Entry.DisplayName,
            AvatarVersion: avatarVersionByUser.GetValueOrDefault(r.Entry.UserId),
            TotalPoints: r.Entry.TotalPoints,
            ExactCount: r.Entry.ExactCount,
            WinnerCorrect: r.Entry.WinnerCorrect,
            TopScorerCorrect: r.Entry.TopScorerCorrect,
            LongestStreak: r.Entry.LongestStreak,
            IsMe: currentUserId.HasValue && r.Entry.UserId == currentUserId.Value)).ToList();
    }

    /// <summary>
    /// Compare a user's long-term tips against the recorded outcomes. Null outcome
    /// → null correctness (ranker treats as neutral). Outcome recorded but user
    /// never tipped → false (they were wrong by default).
    /// Top-scorer match is now a strict FK equality since players come from the
    /// curated <c>players</c> table — typo tolerance moved to the SPA dropdown.
    /// </summary>
    private static (bool? WinnerCorrect, bool? TopScorerCorrect) ComputeLongTipCorrectness(
        Guid? actualWinnerTeamId,
        Guid? actualTopScorerPlayerId,
        IReadOnlyList<LongTipRow>? userLongTips)
    {
        bool? winnerCorrect = null;
        if (actualWinnerTeamId is Guid winnerId)
        {
            var winnerTip = userLongTips?.FirstOrDefault(l => l.Type == LongTermTipType.Winner);
            winnerCorrect = winnerTip is not null && winnerTip.TargetTeamId == winnerId;
        }

        bool? topScorerCorrect = null;
        if (actualTopScorerPlayerId is Guid scorerId)
        {
            var topTip = userLongTips?.FirstOrDefault(l => l.Type == LongTermTipType.TopScorer);
            topScorerCorrect = topTip?.TargetPlayerId == scorerId;
        }

        return (winnerCorrect, topScorerCorrect);
    }

    private sealed record LongTipRow(Guid UserId, LongTermTipType Type, Guid? TargetTeamId, Guid? TargetPlayerId);
}
