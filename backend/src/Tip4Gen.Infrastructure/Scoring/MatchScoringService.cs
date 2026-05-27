using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Scoring;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Scoring;

public abstract record MatchScoringResult
{
    public sealed record Success(Guid MatchId, int TipsScored, int TotalPoints) : MatchScoringResult;

    /// <summary>Match does not exist.</summary>
    public sealed record MatchNotFound(Guid MatchId) : MatchScoringResult;

    /// <summary>
    /// Match exists but is not in a state where scoring is meaningful (e.g. Scheduled, Live,
    /// Postponed, Cancelled, Abandoned). Awarded + Finished are the only scorable states.
    /// </summary>
    public sealed record NotScorable(Guid MatchId, MatchStatus Status) : MatchScoringResult;
}

public interface IMatchScoringService
{
    /// <summary>
    /// Scores every tip on the match. Idempotent: deletes the existing scored_tips rows
    /// for the match within the same SaveChanges, then re-inserts. Safe to call from
    /// the MatchFinalized event handler AND from the admin re-score endpoint.
    /// </summary>
    Task<MatchScoringResult> ScoreMatchAsync(Guid matchId, CancellationToken ct);
}

public class MatchScoringService(AppDbContext db, ILogger<MatchScoringService> logger) : IMatchScoringService
{
    public async Task<MatchScoringResult> ScoreMatchAsync(Guid matchId, CancellationToken ct)
    {
        var match = await db.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId, ct);
        if (match is null)
            return new MatchScoringResult.MatchNotFound(matchId);

        // Only Finished/Awarded matches with a recorded score are scorable.
        // Abandoned/Cancelled get cleared by admin actions (Phase 8) — not here.
        var hasScore = match.HomeGoals is int && match.AwayGoals is int;
        var isScorableStatus = match.Status is MatchStatus.Finished or MatchStatus.Awarded;
        if (!hasScore || !isScorableStatus)
            return new MatchScoringResult.NotScorable(matchId, match.Status);

        var result = new MatchResult(match.HomeGoals!.Value, match.AwayGoals!.Value);

        // Clear previous scoring for this match (cheap; tip_id has a unique index).
        var existing = await db.ScoredTips.Where(s => s.MatchId == matchId).ToListAsync(ct);
        if (existing.Count > 0)
            db.ScoredTips.RemoveRange(existing);

        var tips = await db.Tips.Where(t => t.MatchId == matchId).ToListAsync(ct);
        var total = 0;
        foreach (var tip in tips)
        {
            var scoring = MatchScorer.Score(tip.HomeGoals, tip.AwayGoals, result, match.Stage, tip.Joker);
            db.ScoredTips.Add(new ScoredTip(tip.Id, match.Id, tip.UserId, tip.TeamMemberId, scoring));
            total += scoring.FinalPoints;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Scored match {MatchId} ({Stage} {Home}-{Away}): {Count} tips, total {Total} points (re-score: {ReScored} rows cleared)",
            matchId, match.Stage, result.HomeGoals, result.AwayGoals, tips.Count, total, existing.Count);

        return new MatchScoringResult.Success(matchId, tips.Count, total);
    }
}
