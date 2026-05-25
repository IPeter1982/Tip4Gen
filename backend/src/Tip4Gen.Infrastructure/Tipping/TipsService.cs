using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Tipping;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Tipping;

public abstract record TipUpsertResult
{
    public sealed record Success(Tip Tip, DateTimeOffset DeadlineUtc, bool Created) : TipUpsertResult;
    public sealed record MatchNotFound : TipUpsertResult;
    public sealed record Rejected(TipValidationResult Validation) : TipUpsertResult;
}

public record TipUpsertCommand(
    Guid UserId,
    Guid MatchId,
    int HomeGoals,
    int AwayGoals,
    bool Joker);

public interface ITipsService
{
    Task<TipUpsertResult> UpsertAsync(TipUpsertCommand cmd, CancellationToken ct);
}

public class TipsService(AppDbContext db, ILogger<TipsService> logger) : ITipsService
{
    public async Task<TipUpsertResult> UpsertAsync(TipUpsertCommand cmd, CancellationToken ct)
    {
        var match = await db.Matches.FirstOrDefaultAsync(m => m.Id == cmd.MatchId, ct);
        if (match is null)
            return new TipUpsertResult.MatchNotFound();

        var existing = await db.Tips
            .FirstOrDefaultAsync(t => t.UserId == cmd.UserId && t.MatchId == cmd.MatchId, ct);

        // Count this user's joker usage on OTHER matches; excluding the current
        // match makes update flows correct (changing your joker on this match
        // doesn't double-count against the cap).
        var otherJokerCount = await db.Tips
            .CountAsync(t => t.UserId == cmd.UserId && t.MatchId != cmd.MatchId && t.Joker, ct);

        var now = DateTimeOffset.UtcNow;
        var validation = TipRulesValidator.Validate(
            now: now,
            matchKickoffUtc: match.KickoffUtc,
            matchStage: match.Stage,
            homeGoals: cmd.HomeGoals,
            awayGoals: cmd.AwayGoals,
            usingJoker: cmd.Joker,
            otherJokerCountForUser: otherJokerCount);

        if (!validation.IsValid)
            return new TipUpsertResult.Rejected(validation);

        bool created;
        Tip tip;
        if (existing is null)
        {
            tip = new Tip(cmd.UserId, cmd.MatchId, cmd.HomeGoals, cmd.AwayGoals, cmd.Joker);
            db.Tips.Add(tip);
            created = true;
        }
        else
        {
            existing.Update(cmd.HomeGoals, cmd.AwayGoals, cmd.Joker);
            tip = existing;
            created = false;
        }

        await db.SaveChangesAsync(ct);

        var deadline = match.KickoffUtc - TipRulesValidator.DeadlineBeforeKickoff;
        logger.LogInformation(
            "Tip {Action} for user {UserId} on match {MatchId}: {Home}-{Away}, joker={Joker}",
            created ? "created" : "updated", cmd.UserId, cmd.MatchId, cmd.HomeGoals, cmd.AwayGoals, cmd.Joker);

        return new TipUpsertResult.Success(tip, deadline, created);
    }
}
