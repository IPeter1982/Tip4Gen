using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Admin;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Admin;

public sealed record LongTipOutcomesSnapshot(
    Guid? WinnerTeamId,
    string? WinnerTeamName,
    Guid? TopScorerPlayerId,
    string? TopScorerPlayerName,
    string? TopScorerTeamCode);

public abstract record LongTipOutcomesResult
{
    public sealed record Success(LongTipOutcomesSnapshot Snapshot) : LongTipOutcomesResult;
    public sealed record TournamentNotConfigured : LongTipOutcomesResult;
    public sealed record WinnerTeamNotFound(Guid TeamId) : LongTipOutcomesResult;
    public sealed record TopScorerPlayerNotFound(Guid PlayerId) : LongTipOutcomesResult;
}

public record SetOutcomesCommand(Guid AdminUserId, Guid? WinnerTeamId, Guid? TopScorerPlayerId, string? Reason);

public interface ILongTipOutcomesService
{
    /// <summary>Read the currently recorded outcomes. Returns null if no tournament exists yet.</summary>
    Task<LongTipOutcomesSnapshot?> GetAsync(CancellationToken ct);

    /// <summary>
    /// Record / update the FIFA-decided outcomes that drive §9 leaderboard tiebreakers.
    /// Either field may be null. Editable: re-calling replaces both values atomically.
    /// </summary>
    Task<LongTipOutcomesResult> SetAsync(SetOutcomesCommand cmd, CancellationToken ct);
}

public class LongTipOutcomesService(
    AppDbContext db,
    IAdminAuditWriter auditWriter,
    ILogger<LongTipOutcomesService> logger) : ILongTipOutcomesService
{
    public async Task<LongTipOutcomesSnapshot?> GetAsync(CancellationToken ct)
    {
        var tournament = await db.Tournaments.AsNoTracking()
            .OrderByDescending(t => t.StartsAtUtc)
            .Select(t => new { t.WinnerTeamId, t.TopScorerPlayerId })
            .FirstOrDefaultAsync(ct);
        if (tournament is null) return null;

        return await BuildSnapshotAsync(tournament.WinnerTeamId, tournament.TopScorerPlayerId, ct);
    }

    public async Task<LongTipOutcomesResult> SetAsync(SetOutcomesCommand cmd, CancellationToken ct)
    {
        var tournament = await db.Tournaments
            .OrderByDescending(t => t.StartsAtUtc)
            .FirstOrDefaultAsync(ct);
        if (tournament is null)
            return new LongTipOutcomesResult.TournamentNotConfigured();

        if (cmd.WinnerTeamId is Guid winnerId)
        {
            var teamExists = await db.NationalTeams.AnyAsync(t => t.Id == winnerId, ct);
            if (!teamExists)
                return new LongTipOutcomesResult.WinnerTeamNotFound(winnerId);
        }

        if (cmd.TopScorerPlayerId is Guid scorerId)
        {
            var playerExists = await db.Players.AnyAsync(p => p.Id == scorerId, ct);
            if (!playerExists)
                return new LongTipOutcomesResult.TopScorerPlayerNotFound(scorerId);
        }

        var before = new
        {
            winnerTeamId = tournament.WinnerTeamId,
            topScorerPlayerId = tournament.TopScorerPlayerId,
        };

        tournament.RecordOutcomes(cmd.WinnerTeamId, cmd.TopScorerPlayerId);

        var after = new
        {
            winnerTeamId = tournament.WinnerTeamId,
            topScorerPlayerId = tournament.TopScorerPlayerId,
        };

        await auditWriter.RecordAsync(
            adminUserId: cmd.AdminUserId,
            action: AdminAuditAction.LongTipOutcomesSet,
            entityType: AdminAudit.EntityTypeTournament,
            entityId: tournament.Id,
            before: before,
            after: after,
            reason: cmd.Reason,
            ct: ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminUserId} set tournament outcomes: winner={WinnerTeamId}, topScorerPlayerId={TopScorerPlayerId}",
            cmd.AdminUserId, tournament.WinnerTeamId, tournament.TopScorerPlayerId);

        var snapshot = await BuildSnapshotAsync(tournament.WinnerTeamId, tournament.TopScorerPlayerId, ct);
        return new LongTipOutcomesResult.Success(snapshot);
    }

    private async Task<LongTipOutcomesSnapshot> BuildSnapshotAsync(
        Guid? winnerTeamId,
        Guid? topScorerPlayerId,
        CancellationToken ct)
    {
        var winnerName = winnerTeamId is Guid wid
            ? await db.NationalTeams.AsNoTracking().Where(t => t.Id == wid).Select(t => t.Name).FirstOrDefaultAsync(ct)
            : null;

        string? scorerName = null;
        string? scorerTeamCode = null;
        if (topScorerPlayerId is Guid sid)
        {
            var row = await (
                from p in db.Players.AsNoTracking().Where(p => p.Id == sid)
                join n in db.NationalTeams.AsNoTracking() on p.NationalTeamId equals n.Id
                select new { p.Name, n.Code }).FirstOrDefaultAsync(ct);
            scorerName = row?.Name;
            scorerTeamCode = row?.Code;
        }

        return new LongTipOutcomesSnapshot(
            winnerTeamId,
            winnerName,
            topScorerPlayerId,
            scorerName,
            scorerTeamCode);
    }
}
