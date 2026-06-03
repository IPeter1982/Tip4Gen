using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Football;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Infrastructure.Football;

public class WorldCup26IrProvider : IFootballDataProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // Per user decision (plan: i-want-to-change-squishy-wirth.md), every local_date
    // value is treated as US Eastern. Mexico/West-Coast/Toronto matches will drift
    // 1-3h; verification step spot-checks this.
    private static readonly TimeZoneInfo Eastern = ResolveEastern();

    private static TimeZoneInfo ResolveEastern()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
    }

    private readonly HttpClient _http;
    private readonly WorldCup26IrJwtCache _jwt;
    private readonly ILogger<WorldCup26IrProvider> _logger;

    public WorldCup26IrProvider(HttpClient http, WorldCup26IrJwtCache jwt, ILogger<WorldCup26IrProvider> logger)
    {
        _http = http;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProviderFixture>> GetFixturesAsync(string leagueId, int season, CancellationToken ct)
    {
        var envelope = await GetJsonAsync<WorldCupGamesEnvelope>("get/games", ct);
        var games = envelope?.Games ?? new List<WorldCupGame>();

        var result = new List<ProviderFixture>(games.Count);
        foreach (var g in games)
        {
            if (g.HomeTeamId == "0" || g.AwayTeamId == "0")
            {
                // Knockout bracket placeholder; skip until the upstream fills the
                // actual qualified team_ids. The poller picks them up next tick.
                continue;
            }

            Stage stage;
            try
            {
                stage = StageMapper.FromWorldCupType(g.Type);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Skipping match {Id} with unrecognized type '{Type}'", g.Id, g.Type);
                continue;
            }

            var kickoffUtc = ParseEasternToUtc(g.LocalDate, g.Id);
            if (kickoffUtc is null)
                continue;

            var status = (g.Finished?.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ?? false)
                ? ProviderStatus.Finished
                : ProviderStatus.Scheduled;

            var homeGoals = ParseScore(g.HomeScore);
            var awayGoals = ParseScore(g.AwayScore);

            result.Add(new ProviderFixture(
                ExternalId: g.Id,
                KickoffUtc: kickoffUtc.Value,
                Stage: stage,
                GroupCode: stage == Stage.Group ? g.Group : null,
                RoundLabel: BuildRoundLabel(stage, g.Group, g.Matchday),
                HomeTeamExternalId: g.HomeTeamId,
                HomeTeamName: g.HomeTeamNameEn ?? g.HomeTeamLabel ?? "Unknown",
                AwayTeamExternalId: g.AwayTeamId,
                AwayTeamName: g.AwayTeamNameEn ?? g.AwayTeamLabel ?? "Unknown",
                Status: status,
                HomeGoalsFullTime: status == ProviderStatus.Finished ? homeGoals : null,
                AwayGoalsFullTime: status == ProviderStatus.Finished ? awayGoals : null));
        }

        return result;
    }

    public async Task<IReadOnlyList<ProviderTeam>> GetTeamsAsync(string leagueId, int season, CancellationToken ct)
    {
        var envelope = await GetJsonAsync<WorldCupTeamsEnvelope>("get/teams", ct);
        var teams = envelope?.Teams ?? new List<WorldCupTeam>();

        return teams
            .Select(t => new ProviderTeam(t.Id, t.NameEn, t.FifaCode))
            .ToList();
    }

    private async Task<T?> GetJsonAsync<T>(string path, CancellationToken ct) where T : class
    {
        var resp = await SendAsync(path, useToken: false, ct);
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            resp.Dispose();
            var token = await _jwt.GetOrRefreshAsync(_http, ct);
            if (token is null)
            {
                _logger.LogError("worldcup26.ir returned 401 and no JWT credentials are configured");
                throw new HttpRequestException("worldcup26.ir requires authentication but no credentials are configured.");
            }

            resp = await SendAsync(path, useToken: true, ct);
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Token was rejected — likely expired; clear and retry once.
                _jwt.Invalidate();
                resp.Dispose();
                token = await _jwt.GetOrRefreshAsync(_http, ct);
                if (token is not null)
                    resp = await SendAsync(path, useToken: true, ct);
            }
        }

        try
        {
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
        }
        finally
        {
            resp.Dispose();
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string path, bool useToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        if (useToken && _jwt.CurrentToken is { } t)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t);
        return await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private DateTimeOffset? ParseEasternToUtc(string localDate, string matchId)
    {
        if (!DateTime.TryParseExact(
                localDate,
                "MM/dd/yyyy HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            _logger.LogWarning("Could not parse local_date '{Date}' for match {Id}", localDate, matchId);
            return null;
        }

        var unspecified = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, Eastern);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static int? ParseScore(string? raw) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static string BuildRoundLabel(Stage stage, string? group, string? matchday) => stage switch
    {
        Stage.Group => $"Group {group ?? "?"} - {matchday ?? "?"}",
        Stage.R32 => "Round of 32",
        Stage.R16 => "Round of 16",
        Stage.QF => "Quarter-finals",
        Stage.SF => "Semi-finals",
        Stage.Bronze => "Third Place",
        Stage.Final => "Final",
        _ => stage.ToString(),
    };
}
