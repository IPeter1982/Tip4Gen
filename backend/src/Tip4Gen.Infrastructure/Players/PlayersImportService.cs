using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Admin;
using Tip4Gen.Domain.Players;
using Tip4Gen.Infrastructure.Admin;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Players;

public sealed record PlayersImportSummary(int Added, int Skipped, int UnmatchedTeams, int TotalAfter);

public abstract record PlayersImportResult
{
    public sealed record Success(PlayersImportSummary Summary) : PlayersImportResult;
    public sealed record ProviderFailure(string Message) : PlayersImportResult;
}

public interface IPlayersImportService
{
    Task<PlayersImportResult> ImportAsync(Guid adminUserId, CancellationToken ct);
}

public class PlayersImportService(
    IWikipediaSquadsProvider provider,
    AppDbContext db,
    IAdminAuditWriter auditWriter,
    ILogger<PlayersImportService> logger) : IPlayersImportService
{
    public async Task<PlayersImportResult> ImportAsync(Guid adminUserId, CancellationToken ct)
    {
        IReadOnlyList<ParsedPlayer> parsed;
        try
        {
            parsed = await provider.FetchAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Wikipedia squads fetch failed");
            return new PlayersImportResult.ProviderFailure(ex.Message);
        }

        // Resolve country → NationalTeam via Name (Wikipedia emits full names like
        // "Czech Republic"; the optional FIFA code from worldcup26.ir is a tiebreaker
        // only when the Wikipedia header happens to match it).
        var teams = await db.NationalTeams.AsNoTracking().ToListAsync(ct);
        var byName = teams
            .GroupBy(t => Normalize(t.Name))
            .ToDictionary(g => g.Key, g => g.First());
        var byCode = teams
            .Where(t => !string.IsNullOrWhiteSpace(t.Code))
            .GroupBy(t => Normalize(t.Code!))
            .ToDictionary(g => g.Key, g => g.First());

        var existing = await db.Players.AsNoTracking()
            .Select(p => new { p.NationalTeamId, p.Name })
            .ToListAsync(ct);
        var existingByTeam = existing
            .GroupBy(p => p.NationalTeamId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => Normalize(p.Name)).ToHashSet());

        var added = 0;
        var skipped = 0;
        var unmatched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in parsed)
        {
            if (!TryResolveTeam(p, byCode, byName, out var team))
            {
                if (unmatched.Add(p.CountryName))
                    logger.LogWarning("No NationalTeam match for Wikipedia country '{Country}' (code='{Code}')",
                        p.CountryName, p.CountryCode);
                continue;
            }

            var nameKey = Normalize(p.Name);
            if (existingByTeam.TryGetValue(team.Id, out var set) && set.Contains(nameKey))
            {
                skipped++;
                continue;
            }

            db.Players.Add(new Player(team.Id, p.Name));
            added++;

            if (!existingByTeam.TryGetValue(team.Id, out var bucket))
                existingByTeam[team.Id] = bucket = new HashSet<string>();
            bucket.Add(nameKey);
        }

        var totalAfter = existing.Count + added;

        var summary = new PlayersImportSummary(added, skipped, unmatched.Count, totalAfter);

        await auditWriter.RecordAsync(
            adminUserId: adminUserId,
            action: AdminAuditAction.PlayersImported,
            entityType: AdminAudit.EntityTypePlayers,
            entityId: null,
            before: null,
            after: new
            {
                added,
                skipped,
                unmatchedTeams = unmatched.Count,
                totalAfter,
                parsedTotal = parsed.Count,
            },
            reason: null,
            ct: ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Players import done: added={Added} skipped={Skipped} unmatchedTeams={Unmatched} totalAfter={Total}",
            added, skipped, unmatched.Count, totalAfter);

        return new PlayersImportResult.Success(summary);
    }

    private static bool TryResolveTeam(
        ParsedPlayer p,
        IReadOnlyDictionary<string, Domain.Tournaments.NationalTeam> byCode,
        IReadOnlyDictionary<string, Domain.Tournaments.NationalTeam> byName,
        out Domain.Tournaments.NationalTeam team)
    {
        if (!string.IsNullOrWhiteSpace(p.CountryCode)
            && byCode.TryGetValue(Normalize(p.CountryCode), out var byCodeMatch))
        {
            team = byCodeMatch;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(p.CountryName)
            && byName.TryGetValue(Normalize(p.CountryName), out var byNameMatch))
        {
            team = byNameMatch;
            return true;
        }

        team = null!;
        return false;
    }

    private static string Normalize(string s) => s.Trim().ToLowerInvariant();
}
