using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Football;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Domain.Tournaments.Events;
using Tip4Gen.Infrastructure.Football;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Tournaments;

public record FixtureSyncResult(
    Guid TournamentId,
    int TeamsCreated,
    int TeamsUpdated,
    int MatchesCreated,
    int MatchesUpdated,
    IReadOnlyList<Guid> FinalizedMatchIds);

public interface IFixtureSyncService
{
    /// <summary>
    /// Pulls fixtures (and optionally the teams roster) from the football provider
    /// and upserts tournament/teams/matches. Idempotent. Dispatches MatchFinalized
    /// for every match that transitioned to Finished during this run.
    /// </summary>
    /// <param name="includeTeamsRoster">When true, calls /teams as well as /fixtures.
    /// The poller passes false to halve its API quota usage; missing teams are still
    /// backfilled from the fixtures payload.</param>
    Task<FixtureSyncResult> SyncAsync(bool includeTeamsRoster, CancellationToken ct);
}

public class FixtureSyncService(
    IFootballDataProvider provider,
    AppDbContext db,
    IOptions<ApiFootballOptions> opts,
    IEnumerable<IMatchFinalizedHandler> matchFinalizedHandlers,
    ILogger<FixtureSyncService> logger) : IFixtureSyncService
{
    public async Task<FixtureSyncResult> SyncAsync(bool includeTeamsRoster, CancellationToken ct)
    {
        var leagueId = opts.Value.LeagueId;
        var season = opts.Value.Season;

        var providerFixtures = await provider.GetFixturesAsync(leagueId, season, ct);
        if (providerFixtures.Count == 0)
            throw new InvalidOperationException(
                $"Provider returned no fixtures for league={leagueId}, season={season}. Refusing to sync an empty tournament.");

        var providerTeams = includeTeamsRoster
            ? await provider.GetTeamsAsync(leagueId, season, ct)
            : Array.Empty<ProviderTeam>();

        var startsAt = providerFixtures.Min(f => f.KickoffUtc);
        var endsAt = providerFixtures.Max(f => f.KickoffUtc);

        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(t => t.ExternalLeagueId == leagueId && t.Season == season, ct);
        if (tournament is null)
        {
            tournament = new Tournament($"FIFA World Cup {season}", leagueId, season, startsAt, endsAt);
            db.Tournaments.Add(tournament);
        }
        else
        {
            tournament.UpdateSchedule(startsAt, endsAt);
        }

        var teamsByExternalId = await db.NationalTeams
            .ToDictionaryAsync(t => t.ExternalId, ct);

        int teamsCreated = 0, teamsUpdated = 0;
        foreach (var pt in providerTeams)
        {
            UpsertTeam(pt.ExternalId, pt.Name, pt.Code, teamsByExternalId, ref teamsCreated, ref teamsUpdated);
        }

        // Backfill any teams that appear in fixtures but were missing from /teams
        // (or skipped because includeTeamsRoster was false).
        foreach (var pf in providerFixtures)
        {
            UpsertTeam(pf.HomeTeamExternalId, pf.HomeTeamName, null, teamsByExternalId, ref teamsCreated, ref teamsUpdated);
            UpsertTeam(pf.AwayTeamExternalId, pf.AwayTeamName, null, teamsByExternalId, ref teamsCreated, ref teamsUpdated);
        }

        await db.SaveChangesAsync(ct);

        var matchesByExternalId = await db.Matches
            .Where(m => m.TournamentId == tournament.Id)
            .ToDictionaryAsync(m => m.ExternalId, ct);

        var finalizedMatches = new List<Match>();
        int matchesCreated = 0, matchesUpdated = 0;

        foreach (var pf in providerFixtures)
        {
            var (stage, groupCode) = StageMapper.FromProviderLabel(pf.RoundLabel);
            var domainStatus = MatchStatusMapper.FromProvider(pf.Status);
            var home = teamsByExternalId[pf.HomeTeamExternalId];
            var away = teamsByExternalId[pf.AwayTeamExternalId];

            if (matchesByExternalId.TryGetValue(pf.ExternalId, out var match))
            {
                var wasFinished = match.Status == MatchStatus.Finished;
                if (UpdateExistingMatch(match, pf, domainStatus))
                {
                    matchesUpdated++;
                    if (!wasFinished && match.Status == MatchStatus.Finished)
                        finalizedMatches.Add(match);
                }
            }
            else
            {
                var newMatch = new Match(
                    tournament.Id,
                    pf.ExternalId,
                    stage,
                    groupCode,
                    pf.RoundLabel,
                    home.Id,
                    away.Id,
                    pf.KickoffUtc);
                ApplyStatusAndScore(newMatch, domainStatus, pf.HomeGoalsFullTime, pf.AwayGoalsFullTime);
                db.Matches.Add(newMatch);
                matchesCreated++;
                if (newMatch.Status == MatchStatus.Finished)
                    finalizedMatches.Add(newMatch);
            }
        }

        await db.SaveChangesAsync(ct);

        var finalizedIds = new List<Guid>(finalizedMatches.Count);
        foreach (var match in finalizedMatches)
        {
            var evt = new MatchFinalized(match.Id, match.TournamentId, DateTimeOffset.UtcNow);
            finalizedIds.Add(match.Id);
            foreach (var handler in matchFinalizedHandlers)
            {
                try
                {
                    await handler.HandleAsync(evt, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "MatchFinalized handler {Handler} failed for match {MatchId}",
                        handler.GetType().Name, match.Id);
                }
            }
        }

        logger.LogInformation(
            "Synced tournament {TournamentId} (league {League}, season {Season}): teams +{TC}/~{TU}, matches +{MC}/~{MU}, finalized {FC}",
            tournament.Id, leagueId, season, teamsCreated, teamsUpdated, matchesCreated, matchesUpdated, finalizedIds.Count);

        return new FixtureSyncResult(tournament.Id, teamsCreated, teamsUpdated, matchesCreated, matchesUpdated, finalizedIds);
    }

    private void UpsertTeam(
        string externalId,
        string name,
        string? code,
        Dictionary<string, NationalTeam> teamsByExternalId,
        ref int created,
        ref int updated)
    {
        if (teamsByExternalId.TryGetValue(externalId, out var existing))
        {
            var changed = false;
            if (!string.Equals(existing.Name, name, StringComparison.Ordinal))
            {
                existing.Rename(name);
                changed = true;
            }
            if (code is not null && !string.Equals(existing.Code, code, StringComparison.Ordinal))
            {
                existing.SetCode(code);
                changed = true;
            }
            if (changed) updated++;
        }
        else
        {
            var team = new NationalTeam(externalId, name, code);
            db.NationalTeams.Add(team);
            teamsByExternalId[externalId] = team;
            created++;
        }
    }

    private static bool UpdateExistingMatch(Match match, ProviderFixture pf, MatchStatus domainStatus)
    {
        var changed = false;

        if (match.KickoffUtc != pf.KickoffUtc)
        {
            match.Reschedule(pf.KickoffUtc);
            changed = true;
        }

        var statusChanged = match.Status != domainStatus;
        var scoreChanged = domainStatus == MatchStatus.Finished
            && pf.HomeGoalsFullTime is int hg
            && pf.AwayGoalsFullTime is int ag
            && (match.HomeGoals != hg || match.AwayGoals != ag);

        if (statusChanged || scoreChanged)
        {
            ApplyStatusAndScore(match, domainStatus, pf.HomeGoalsFullTime, pf.AwayGoalsFullTime);
            changed = true;
        }

        return changed;
    }

    private static void ApplyStatusAndScore(Match match, MatchStatus status, int? homeGoals, int? awayGoals)
    {
        if (status == MatchStatus.Finished && homeGoals is int hg && awayGoals is int ag)
        {
            match.SetFinalScore(hg, ag);
            return;
        }

        if (match.Status != status)
            match.UpdateStatus(status);
    }
}
