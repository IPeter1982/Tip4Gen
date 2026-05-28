using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Admin;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Persistence;
using Tip4Gen.Infrastructure.Scoring;

namespace Tip4Gen.Infrastructure.Admin;

public abstract record MatchAdminResult
{
    public sealed record Success(Guid MatchId, int TipsScored, int TotalPoints) : MatchAdminResult;
    public sealed record MatchNotFound : MatchAdminResult;
    public sealed record InvalidScore(int Home, int Away) : MatchAdminResult;

    /// <summary>The new status requested isn't a result-recording status (only Finished/Awarded).</summary>
    public sealed record InvalidStatusRequested(MatchStatus Requested) : MatchAdminResult;
}

public abstract record MatchCancelResult
{
    public sealed record Success(Guid MatchId, int ScoredTipsCleared, int JokersRefunded) : MatchCancelResult;
    public sealed record MatchNotFound : MatchCancelResult;
    public sealed record AlreadyCancelled : MatchCancelResult;
}

public record CancelMatchCommand(Guid AdminUserId, Guid MatchId, string? Reason);

public abstract record MatchPostponeResult
{
    public sealed record Success(Guid MatchId, DateTimeOffset NewKickoffUtc, DateTimeOffset NewDeadlineUtc) : MatchPostponeResult;
    public sealed record MatchNotFound : MatchPostponeResult;

    /// <summary>The new kickoff is in the past or so close that the resulting deadline (-1h) is already gone.</summary>
    public sealed record KickoffNotFarEnough(DateTimeOffset Requested, DateTimeOffset MinAllowed) : MatchPostponeResult;

    /// <summary>Match status doesn't allow postponement (only Scheduled and Postponed are postponable).</summary>
    public sealed record InvalidStatus(MatchStatus Current) : MatchPostponeResult;
}

public record PostponeMatchCommand(Guid AdminUserId, Guid MatchId, DateTimeOffset NewKickoffUtc, string? Reason);

public record SetResultCommand(
    Guid AdminUserId,
    Guid MatchId,
    int HomeGoals,
    int AwayGoals,
    MatchStatus NewStatus,
    string? Reason);

public interface IMatchAdminService
{
    /// <summary>
    /// Set or correct a final score. NewStatus must be Finished (regular outcome) or
    /// Awarded (FIFA-decided, per guide §11). Audit row is staged inside the same
    /// transaction as the mutation; re-scoring runs immediately after.
    /// </summary>
    Task<MatchAdminResult> SetResultAsync(SetResultCommand cmd, CancellationToken ct);

    /// <summary>
    /// Cancel a match per guide §11: clears the recorded score, sets status to
    /// Cancelled, deletes scored_tips so the leaderboard sees zero contribution, and
    /// refunds the joker on each tip that played one. Tip rows themselves remain as
    /// a historical record. Idempotent: re-calling on an already-cancelled match
    /// returns AlreadyCancelled (no further side effects).
    /// </summary>
    Task<MatchCancelResult> CancelAsync(CancelMatchCommand cmd, CancellationToken ct);

    /// <summary>
    /// Postpone a match per guide §11: shifts kickoff (and thus the implicit -1h
    /// deadline), sets status to Postponed. Existing tips carry over and remain
    /// editable up to the new deadline — TipRulesValidator's deadline check is
    /// driven by kickoff_utc alone. Jokers are NOT refunded (intentional per §11).
    /// </summary>
    Task<MatchPostponeResult> PostponeAsync(PostponeMatchCommand cmd, CancellationToken ct);
}

public class MatchAdminService(
    AppDbContext db,
    IAdminAuditWriter auditWriter,
    IMatchScoringService scoring,
    ILogger<MatchAdminService> logger) : IMatchAdminService
{
    public async Task<MatchAdminResult> SetResultAsync(SetResultCommand cmd, CancellationToken ct)
    {
        if (cmd.HomeGoals < 0 || cmd.HomeGoals > 15 || cmd.AwayGoals < 0 || cmd.AwayGoals > 15)
            return new MatchAdminResult.InvalidScore(cmd.HomeGoals, cmd.AwayGoals);

        if (cmd.NewStatus is not (MatchStatus.Finished or MatchStatus.Awarded))
            return new MatchAdminResult.InvalidStatusRequested(cmd.NewStatus);

        var match = await db.Matches.FirstOrDefaultAsync(m => m.Id == cmd.MatchId, ct);
        if (match is null)
            return new MatchAdminResult.MatchNotFound();

        var before = new
        {
            status = match.Status,
            homeGoals = match.HomeGoals,
            awayGoals = match.AwayGoals,
        };

        if (cmd.NewStatus == MatchStatus.Awarded)
            match.AwardResult(cmd.HomeGoals, cmd.AwayGoals);
        else
            match.SetFinalScore(cmd.HomeGoals, cmd.AwayGoals);

        var after = new
        {
            status = match.Status,
            homeGoals = match.HomeGoals,
            awayGoals = match.AwayGoals,
        };

        await auditWriter.RecordAsync(
            adminUserId: cmd.AdminUserId,
            action: AdminAuditAction.MatchSetResult,
            entityType: AdminAudit.EntityTypeMatch,
            entityId: cmd.MatchId,
            before: before,
            after: after,
            reason: cmd.Reason,
            ct: ct);

        await db.SaveChangesAsync(ct);

        // Re-score after the result lands. ScoreMatchAsync is idempotent (delete +
        // re-insert in its own SaveChanges) so a stale set of scored_tips from a
        // prior result won't pollute the new totals.
        var scoringResult = await scoring.ScoreMatchAsync(cmd.MatchId, ct);
        var (tipsScored, totalPoints) = scoringResult switch
        {
            MatchScoringResult.Success s => (s.TipsScored, s.TotalPoints),
            // If scoring somehow can't run (shouldn't, since we just set the score),
            // log and return zeros — the admin will see them and can hit /rescore manually.
            _ => (0, 0),
        };

        if (scoringResult is not MatchScoringResult.Success)
        {
            logger.LogWarning(
                "Result set on match {MatchId} but scoring returned {Outcome} — admin may need to /rescore",
                cmd.MatchId, scoringResult.GetType().Name);
        }

        logger.LogInformation(
            "Admin {AdminUserId} set match {MatchId} → {Home}-{Away} ({Status}); scored {TipsScored} tips, {TotalPoints} points",
            cmd.AdminUserId, cmd.MatchId, cmd.HomeGoals, cmd.AwayGoals, cmd.NewStatus, tipsScored, totalPoints);

        return new MatchAdminResult.Success(cmd.MatchId, tipsScored, totalPoints);
    }

    public async Task<MatchCancelResult> CancelAsync(CancelMatchCommand cmd, CancellationToken ct)
    {
        var match = await db.Matches.FirstOrDefaultAsync(m => m.Id == cmd.MatchId, ct);
        if (match is null)
            return new MatchCancelResult.MatchNotFound();

        if (match.Status == MatchStatus.Cancelled)
            return new MatchCancelResult.AlreadyCancelled();

        var before = new
        {
            status = match.Status,
            homeGoals = match.HomeGoals,
            awayGoals = match.AwayGoals,
        };

        match.ClearScore();
        match.UpdateStatus(MatchStatus.Cancelled);

        // Wipe scored_tips for the match so the leaderboard sees no contribution.
        // ExecuteDeleteAsync runs as a single DELETE statement, but it bypasses the
        // change tracker — the audit row still SaveChanges below so the transaction
        // boundary stays clean.
        var scoredCleared = await db.ScoredTips
            .Where(s => s.MatchId == cmd.MatchId)
            .ExecuteDeleteAsync(ct);

        // Refund jokers per §11: flip joker → false on every tip for this match that
        // played one. Tip rows themselves remain. ExecuteUpdate returns row count so
        // we can report it in the audit + the response.
        var jokersRefunded = await db.Tips
            .Where(t => t.MatchId == cmd.MatchId && t.Joker)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Joker, false), ct);

        var after = new
        {
            status = match.Status,
            homeGoals = match.HomeGoals,
            awayGoals = match.AwayGoals,
            scoredTipsCleared = scoredCleared,
            jokersRefunded,
        };

        await auditWriter.RecordAsync(
            adminUserId: cmd.AdminUserId,
            action: AdminAuditAction.MatchCancel,
            entityType: AdminAudit.EntityTypeMatch,
            entityId: cmd.MatchId,
            before: before,
            after: after,
            reason: cmd.Reason,
            ct: ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminUserId} cancelled match {MatchId}: cleared {ScoredCleared} scored tips, refunded {JokersRefunded} jokers",
            cmd.AdminUserId, cmd.MatchId, scoredCleared, jokersRefunded);

        return new MatchCancelResult.Success(cmd.MatchId, scoredCleared, jokersRefunded);
    }

    public async Task<MatchPostponeResult> PostponeAsync(PostponeMatchCommand cmd, CancellationToken ct)
    {
        var match = await db.Matches.FirstOrDefaultAsync(m => m.Id == cmd.MatchId, ct);
        if (match is null)
            return new MatchPostponeResult.MatchNotFound();

        if (match.Status is not (MatchStatus.Scheduled or MatchStatus.Postponed))
            return new MatchPostponeResult.InvalidStatus(match.Status);

        // The new deadline is kickoff - 1h. Require the new kickoff to be at least
        // 1h + a small buffer ahead of now so the deadline isn't already in the past.
        var now = DateTimeOffset.UtcNow;
        var minKickoff = now + TimeSpan.FromHours(1) + TimeSpan.FromMinutes(5);
        if (cmd.NewKickoffUtc < minKickoff)
            return new MatchPostponeResult.KickoffNotFarEnough(cmd.NewKickoffUtc, minKickoff);

        var before = new
        {
            kickoffUtc = match.KickoffUtc,
            status = match.Status,
        };

        match.Reschedule(cmd.NewKickoffUtc);
        match.UpdateStatus(MatchStatus.Postponed);

        var after = new
        {
            kickoffUtc = match.KickoffUtc,
            status = match.Status,
        };

        await auditWriter.RecordAsync(
            adminUserId: cmd.AdminUserId,
            action: AdminAuditAction.MatchPostpone,
            entityType: AdminAudit.EntityTypeMatch,
            entityId: cmd.MatchId,
            before: before,
            after: after,
            reason: cmd.Reason,
            ct: ct);

        await db.SaveChangesAsync(ct);

        var newDeadline = cmd.NewKickoffUtc - TimeSpan.FromHours(1);
        logger.LogInformation(
            "Admin {AdminUserId} postponed match {MatchId} → {NewKickoff} (deadline {NewDeadline})",
            cmd.AdminUserId, cmd.MatchId, cmd.NewKickoffUtc, newDeadline);

        return new MatchPostponeResult.Success(cmd.MatchId, cmd.NewKickoffUtc, newDeadline);
    }
}
