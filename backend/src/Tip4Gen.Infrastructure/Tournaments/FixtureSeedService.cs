using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Football;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Football;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Tournaments;

public record FixtureSeedResult(
    Guid TournamentId,
    int TeamsCreated,
    int TeamsUpdated,
    int MatchesCreated,
    int MatchesUpdated);

public interface IFixtureSeedService
{
    Task<FixtureSeedResult> SeedAsync(CancellationToken ct);
}

public class FixtureSeedService(
    IFootballDataProvider provider,
    AppDbContext db,
    IOptions<ApiFootballOptions> opts,
    ILogger<FixtureSeedService> logger) : IFixtureSeedService
{
    public async Task<FixtureSeedResult> SeedAsync(CancellationToken ct)
    {
        var leagueId = opts.Value.LeagueId;
        var season = opts.Value.Season;

        var providerTeams = await provider.GetTeamsAsync(leagueId, season, ct);
        var providerFixtures = await provider.GetFixturesAsync(leagueId, season, ct);

        if (providerFixtures.Count == 0)
            throw new InvalidOperationException(
                $"Provider returned no fixtures for league={leagueId}, season={season}. Refusing to seed an empty tournament.");

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

        // Some teams may appear in fixtures but not in the /teams payload (rare, but possible
        // mid-tournament if a late qualifier is added). Backfill from the fixtures pass.
        foreach (var pf in providerFixtures)
        {
            UpsertTeam(pf.HomeTeamExternalId, pf.HomeTeamName, null, teamsByExternalId, ref teamsCreated, ref teamsUpdated);
            UpsertTeam(pf.AwayTeamExternalId, pf.AwayTeamName, null, teamsByExternalId, ref teamsCreated, ref teamsUpdated);
        }

        await db.SaveChangesAsync(ct);

        var matchesByExternalId = await db.Matches
            .Where(m => m.TournamentId == tournament.Id)
            .ToDictionaryAsync(m => m.ExternalId, ct);

        int matchesCreated = 0, matchesUpdated = 0;
        foreach (var pf in providerFixtures)
        {
            var (stage, groupCode) = StageMapper.FromProviderLabel(pf.RoundLabel);
            var domainStatus = MatchStatusMapper.FromProvider(pf.Status);
            var home = teamsByExternalId[pf.HomeTeamExternalId];
            var away = teamsByExternalId[pf.AwayTeamExternalId];

            if (matchesByExternalId.TryGetValue(pf.ExternalId, out var match))
            {
                if (UpdateExistingMatch(match, pf, domainStatus))
                    matchesUpdated++;
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
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seeded tournament {TournamentId} (league {League}, season {Season}): teams +{TC}/~{TU}, matches +{MC}/~{MU}",
            tournament.Id, leagueId, season, teamsCreated, teamsUpdated, matchesCreated, matchesUpdated);

        return new FixtureSeedResult(tournament.Id, teamsCreated, teamsUpdated, matchesCreated, matchesUpdated);
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

        // Finished but provider didn't include a score — keep the status but skip the score.
        if (match.Status != status)
            match.UpdateStatus(status);
    }
}
