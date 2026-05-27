using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Ai;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Domain.Tipping;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Ai;

public sealed record AiTippingRunSummary(int AttemptsMade, int TipsWritten, int FallbacksWritten, int Skipped);

public interface IAiTippingService
{
    /// <summary>
    /// Walks every (AI team_member, upcoming Scheduled match) pair, applies the schedule
    /// policy, and persists the resulting tip or fallback row. Idempotent — repeat calls
    /// in the same window are safe (the policy returns Skip once a tip exists).
    /// </summary>
    Task<AiTippingRunSummary> RunOnceAsync(CancellationToken ct);
}

/// <summary>
/// Orchestrates the AI tipping flow per guide §7 and the Phase 6 spec. On each tick:
///  • find Locked teams' AI members
///  • for every Scheduled match in the next ~3h, decide what to do:
///      AttemptAi → call OpenAiTipper, record attempt, write tip on success
///      WriteFallback → write the deterministic 1–1 with is_ai_fallback = true
///      Skip / DeadlinePassed → do nothing
///
/// Uniqueness on (team_member_id, match_id) on the tips table is the last line of defence
/// against double-insertion if two job instances ever overlap.
/// </summary>
public class AiTippingService(
    AppDbContext db,
    IAiTipper tipper,
    ILogger<AiTippingService> logger,
    TimeProvider clock) : IAiTippingService
{
    public const int FallbackHomeGoals = 1;
    public const int FallbackAwayGoals = 1;
    public const string FallbackReasoning = "AI nem válaszolt időben.";

    public async Task<AiTippingRunSummary> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var horizonEnd = now + AiTipSchedulePolicy.AttemptWindow + TimeSpan.FromMinutes(15);

        // Pull AI members on Locked teams. The team's AiMode drives prompt tone — if it
        // happens to be null on a Locked team (shouldn't, by lock policy) we default to Balanced.
        var aiMembers = await (
            from m in db.TeamMembers.AsNoTracking().Where(x => x.IsAi)
            join t in db.Teams.AsNoTracking() on m.TeamId equals t.Id
            where t.Status == TeamStatus.Locked
            select new
            {
                MemberId = m.Id,
                t.AiMode,
            }).ToListAsync(ct);

        if (aiMembers.Count == 0)
            return new AiTippingRunSummary(0, 0, 0, 0);

        // Matches in the schedule window. We cap the upper bound at kickoff + small buffer
        // so the DB doesn't drag in irrelevant rows; the policy still rejects post-kickoff.
        var matches = await (
            from match in db.Matches.AsNoTracking()
            where match.Status == MatchStatus.Scheduled
                && match.KickoffUtc > now
                && match.KickoffUtc <= horizonEnd
            join home in db.NationalTeams.AsNoTracking() on match.HomeTeamId equals home.Id
            join away in db.NationalTeams.AsNoTracking() on match.AwayTeamId equals away.Id
            select new
            {
                MatchId = match.Id,
                match.KickoffUtc,
                match.Stage,
                HomeName = home.Name,
                AwayName = away.Name,
            }).ToListAsync(ct);

        if (matches.Count == 0)
            return new AiTippingRunSummary(0, 0, 0, 0);

        var memberIds = aiMembers.Select(a => a.MemberId).ToList();
        var matchIds = matches.Select(m => m.MatchId).ToList();

        // Pre-fetch existing tips and prior attempt counts for the full cross-product.
        var existingTipKeys = await db.Tips.AsNoTracking()
            .Where(t => t.TeamMemberId != null
                && memberIds.Contains(t.TeamMemberId!.Value)
                && matchIds.Contains(t.MatchId))
            .Select(t => new { MemberId = t.TeamMemberId!.Value, t.MatchId })
            .ToListAsync(ct);
        var tipExists = existingTipKeys.Select(k => (k.MemberId, k.MatchId)).ToHashSet();

        var attemptCounts = (await db.AiTipAttempts.AsNoTracking()
            .Where(a => memberIds.Contains(a.TeamMemberId) && matchIds.Contains(a.MatchId))
            .GroupBy(a => new { a.TeamMemberId, a.MatchId })
            .Select(g => new { g.Key.TeamMemberId, g.Key.MatchId, Count = g.Count() })
            .ToListAsync(ct))
            .ToDictionary(x => (x.TeamMemberId, x.MatchId), x => x.Count);

        int attempts = 0, written = 0, fallbacks = 0, skipped = 0;

        foreach (var member in aiMembers)
        {
            foreach (var match in matches)
            {
                var key = (member.MemberId, match.MatchId);
                var prior = attemptCounts.GetValueOrDefault(key, 0);
                var decision = AiTipSchedulePolicy.Decide(
                    now: now,
                    kickoffUtc: match.KickoffUtc,
                    tipExists: tipExists.Contains(key),
                    previousAttempts: prior);

                switch (decision)
                {
                    case AiTipDecision.AttemptAi:
                        attempts++;
                        var mode = member.AiMode ?? AiMode.Balanced;
                        var result = await tipper.GenerateAsync(
                            match.HomeName, match.AwayName, match.Stage, mode, ct);

                        if (result is AiTipResult.Success ok)
                        {
                            db.Tips.Add(Tip.ForAi(
                                teamMemberId: member.MemberId,
                                matchId: match.MatchId,
                                homeGoals: ok.Response.HomeGoals,
                                awayGoals: ok.Response.AwayGoals,
                                reasoning: ok.Response.Reasoning,
                                isAiFallback: false));
                            db.AiTipAttempts.Add(new AiTipAttempt(
                                teamMemberId: member.MemberId,
                                matchId: match.MatchId,
                                success: true,
                                errorMessage: null));
                            tipExists.Add(key);
                            written++;
                        }
                        else
                        {
                            db.AiTipAttempts.Add(new AiTipAttempt(
                                teamMemberId: member.MemberId,
                                matchId: match.MatchId,
                                success: false,
                                errorMessage: DescribeFailure(result)));
                            attemptCounts[key] = prior + 1;
                        }
                        break;

                    case AiTipDecision.WriteFallback:
                        db.Tips.Add(Tip.ForAi(
                            teamMemberId: member.MemberId,
                            matchId: match.MatchId,
                            homeGoals: FallbackHomeGoals,
                            awayGoals: FallbackAwayGoals,
                            reasoning: FallbackReasoning,
                            isAiFallback: true));
                        tipExists.Add(key);
                        fallbacks++;
                        break;

                    case AiTipDecision.Skip:
                    case AiTipDecision.DeadlinePassed:
                    default:
                        skipped++;
                        break;
                }
            }
        }

        if (db.ChangeTracker.HasChanges())
        {
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Unique (team_member_id, match_id) on tips guards against a double-write
                // from overlapping ticks. Log and move on — the next tick will Skip.
                logger.LogWarning(ex, "AiTippingService SaveChanges failed; will retry next tick");
            }
        }

        logger.LogInformation(
            "AI tipping run: attempts={Attempts}, written={Written}, fallbacks={Fallbacks}, skipped={Skipped}",
            attempts, written, fallbacks, skipped);

        return new AiTippingRunSummary(attempts, written, fallbacks, skipped);
    }

    private static string DescribeFailure(AiTipResult result) => result switch
    {
        AiTipResult.Disabled => "disabled (no API key)",
        AiTipResult.Timeout => "timeout",
        AiTipResult.ProviderError pe => $"provider error: {pe.Error}",
        AiTipResult.InvalidResponse ir => $"invalid response: {ir.Error}",
        _ => result.GetType().Name,
    };
}
