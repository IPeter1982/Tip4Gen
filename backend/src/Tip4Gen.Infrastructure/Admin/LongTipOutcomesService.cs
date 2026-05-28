using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Admin;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Admin;

public sealed record LongTipOutcomesSnapshot(Guid? WinnerTeamId, string? WinnerTeamName, string? TopScorerName);

public abstract record LongTipOutcomesResult
{
    public sealed record Success(LongTipOutcomesSnapshot Snapshot) : LongTipOutcomesResult;
    public sealed record TournamentNotConfigured : LongTipOutcomesResult;
    public sealed record WinnerTeamNotFound(Guid TeamId) : LongTipOutcomesResult;
    public sealed record TopScorerNameTooLong(int Length, int Max) : LongTipOutcomesResult;
}

public record SetOutcomesCommand(Guid AdminUserId, Guid? WinnerTeamId, string? TopScorerName, string? Reason);

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
    public const int MaxTopScorerLength = 120;

    public async Task<LongTipOutcomesSnapshot?> GetAsync(CancellationToken ct)
    {
        var row = await (
            from t in db.Tournaments.AsNoTracking().OrderByDescending(t => t.StartsAtUtc)
            from team in db.NationalTeams.AsNoTracking().Where(n => n.Id == t.WinnerTeamId).DefaultIfEmpty()
            select new { t.WinnerTeamId, WinnerName = team != null ? team.Name : null, t.TopScorerName }
        ).FirstOrDefaultAsync(ct);

        return row is null
            ? null
            : new LongTipOutcomesSnapshot(row.WinnerTeamId, row.WinnerName, row.TopScorerName);
    }

    public async Task<LongTipOutcomesResult> SetAsync(SetOutcomesCommand cmd, CancellationToken ct)
    {
        var trimmedScorer = string.IsNullOrWhiteSpace(cmd.TopScorerName) ? null : cmd.TopScorerName.Trim();
        if (trimmedScorer is { Length: > MaxTopScorerLength })
            return new LongTipOutcomesResult.TopScorerNameTooLong(trimmedScorer.Length, MaxTopScorerLength);

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

        var before = new
        {
            winnerTeamId = tournament.WinnerTeamId,
            topScorerName = tournament.TopScorerName,
        };

        tournament.RecordOutcomes(cmd.WinnerTeamId, trimmedScorer);

        var after = new
        {
            winnerTeamId = tournament.WinnerTeamId,
            topScorerName = tournament.TopScorerName,
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

        var winnerName = tournament.WinnerTeamId is Guid wid
            ? await db.NationalTeams.AsNoTracking().Where(t => t.Id == wid).Select(t => t.Name).FirstOrDefaultAsync(ct)
            : null;

        logger.LogInformation(
            "Admin {AdminUserId} set tournament outcomes: winner={WinnerTeamId}, topScorer={TopScorerName}",
            cmd.AdminUserId, tournament.WinnerTeamId, tournament.TopScorerName);

        return new LongTipOutcomesResult.Success(
            new LongTipOutcomesSnapshot(tournament.WinnerTeamId, winnerName, tournament.TopScorerName));
    }
}
