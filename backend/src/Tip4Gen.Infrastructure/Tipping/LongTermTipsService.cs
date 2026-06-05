using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Tipping;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Tipping;

public record LongTermTipSnapshot(
    Guid? WinnerTeamId,
    string? WinnerTeamName,
    Guid? TopScorerPlayerId,
    string? TopScorerPlayerName,
    string? TopScorerTeamCode,
    DateTimeOffset? WinnerSubmittedAt,
    DateTimeOffset? TopScorerSubmittedAt,
    DateTimeOffset LockUtc,
    bool Locked);

public abstract record LongTermTipUpsertResult
{
    public sealed record Success(LongTermTipSnapshot Snapshot) : LongTermTipUpsertResult;
    public sealed record TournamentNotConfigured : LongTermTipUpsertResult;
    public sealed record TeamNotFound(Guid TeamId) : LongTermTipUpsertResult;
    public sealed record PlayerNotFound(Guid PlayerId) : LongTermTipUpsertResult;
    public sealed record Rejected(LongTermTipValidationResult Validation) : LongTermTipUpsertResult;
}

public record LongTermTipUpsertCommand(Guid UserId, Guid? WinnerTeamId, Guid? TopScorerPlayerId);

public interface ILongTermTipsService
{
    Task<LongTermTipUpsertResult> UpsertAsync(LongTermTipUpsertCommand cmd, CancellationToken ct);
    Task<LongTermTipSnapshot?> GetForUserAsync(Guid userId, CancellationToken ct);
}

public class LongTermTipsService(AppDbContext db, ILogger<LongTermTipsService> logger) : ILongTermTipsService
{
    public async Task<LongTermTipUpsertResult> UpsertAsync(LongTermTipUpsertCommand cmd, CancellationToken ct)
    {
        var tournament = await db.Tournaments
            .OrderByDescending(t => t.StartsAtUtc)
            .FirstOrDefaultAsync(ct);
        if (tournament is null)
            return new LongTermTipUpsertResult.TournamentNotConfigured();

        var now = DateTimeOffset.UtcNow;
        var validation = LongTermTipRulesValidator.Validate(
            now, tournament.StartsAtUtc, cmd.WinnerTeamId, cmd.TopScorerPlayerId);
        if (!validation.IsValid)
            return new LongTermTipUpsertResult.Rejected(validation);

        if (cmd.WinnerTeamId.HasValue)
        {
            var teamExists = await db.NationalTeams.AnyAsync(t => t.Id == cmd.WinnerTeamId.Value, ct);
            if (!teamExists)
                return new LongTermTipUpsertResult.TeamNotFound(cmd.WinnerTeamId.Value);

            var existing = await db.LongTermTips
                .FirstOrDefaultAsync(t => t.UserId == cmd.UserId && t.Type == LongTermTipType.Winner, ct);
            if (existing is null)
                db.LongTermTips.Add(LongTermTip.ForWinner(cmd.UserId, cmd.WinnerTeamId.Value));
            else
                existing.UpdateWinner(cmd.WinnerTeamId.Value);
        }

        if (cmd.TopScorerPlayerId.HasValue)
        {
            var playerExists = await db.Players.AnyAsync(p => p.Id == cmd.TopScorerPlayerId.Value, ct);
            if (!playerExists)
                return new LongTermTipUpsertResult.PlayerNotFound(cmd.TopScorerPlayerId.Value);

            var existing = await db.LongTermTips
                .FirstOrDefaultAsync(t => t.UserId == cmd.UserId && t.Type == LongTermTipType.TopScorer, ct);
            if (existing is null)
                db.LongTermTips.Add(LongTermTip.ForTopScorer(cmd.UserId, cmd.TopScorerPlayerId.Value));
            else
                existing.UpdateTopScorer(cmd.TopScorerPlayerId.Value);
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Long-term tips upserted for user {UserId}: winner={Winner}, topScorerPlayerId={Scorer}",
            cmd.UserId, cmd.WinnerTeamId, cmd.TopScorerPlayerId);

        var snapshot = await BuildSnapshotAsync(cmd.UserId, tournament.StartsAtUtc, ct);
        return new LongTermTipUpsertResult.Success(snapshot);
    }

    public async Task<LongTermTipSnapshot?> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var tournament = await db.Tournaments
            .OrderByDescending(t => t.StartsAtUtc)
            .FirstOrDefaultAsync(ct);
        if (tournament is null) return null;

        return await BuildSnapshotAsync(userId, tournament.StartsAtUtc, ct);
    }

    private async Task<LongTermTipSnapshot> BuildSnapshotAsync(Guid userId, DateTimeOffset lockUtc, CancellationToken ct)
    {
        var winner = await (
            from t in db.LongTermTips.AsNoTracking().Where(t => t.UserId == userId && t.Type == LongTermTipType.Winner)
            join team in db.NationalTeams.AsNoTracking() on t.TargetTeamId equals team.Id
            select new { t, team.Name }).FirstOrDefaultAsync(ct);

        var topScorer = await (
            from t in db.LongTermTips.AsNoTracking().Where(t => t.UserId == userId && t.Type == LongTermTipType.TopScorer)
            from p in db.Players.AsNoTracking().Where(p => p.Id == t.TargetPlayerId).DefaultIfEmpty()
            from team in db.NationalTeams.AsNoTracking().Where(n => p != null && n.Id == p.NationalTeamId).DefaultIfEmpty()
            select new
            {
                t,
                PlayerName = p != null ? p.Name : null,
                TeamCode = team != null ? team.Code : null
            }).FirstOrDefaultAsync(ct);

        return new LongTermTipSnapshot(
            WinnerTeamId: winner?.t.TargetTeamId,
            WinnerTeamName: winner?.Name,
            TopScorerPlayerId: topScorer?.t.TargetPlayerId,
            TopScorerPlayerName: topScorer?.PlayerName,
            TopScorerTeamCode: topScorer?.TeamCode,
            WinnerSubmittedAt: winner?.t.SubmittedAt,
            TopScorerSubmittedAt: topScorer?.t.SubmittedAt,
            LockUtc: lockUtc,
            Locked: DateTimeOffset.UtcNow >= lockUtc);
    }
}
