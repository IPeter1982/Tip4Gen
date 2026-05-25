using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Football;

namespace Tip4Gen.Infrastructure.Football;

public class ApiFootballProvider : IFootballDataProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<ApiFootballProvider> _logger;

    public ApiFootballProvider(HttpClient http, ILogger<ApiFootballProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProviderFixture>> GetFixturesAsync(string leagueId, int season, CancellationToken ct)
    {
        var url = $"fixtures?league={Uri.EscapeDataString(leagueId)}&season={season}";
        var payload = await _http.GetFromJsonAsync<ApiFootballFixturesResponse>(url, JsonOpts, ct);

        var items = payload?.Response ?? new List<ApiFootballFixtureItem>();
        var result = new List<ProviderFixture>(items.Count);

        foreach (var item in items)
        {
            result.Add(new ProviderFixture(
                ExternalId: item.Fixture.Id.ToString(CultureInfo.InvariantCulture),
                KickoffUtc: item.Fixture.Date.ToUniversalTime(),
                RoundLabel: item.League.Round,
                HomeTeamExternalId: item.Teams.Home.Id.ToString(CultureInfo.InvariantCulture),
                HomeTeamName: item.Teams.Home.Name,
                AwayTeamExternalId: item.Teams.Away.Id.ToString(CultureInfo.InvariantCulture),
                AwayTeamName: item.Teams.Away.Name,
                Status: MapStatus(item.Fixture.Status.Short),
                HomeGoalsFullTime: item.Score?.Fulltime?.Home,
                AwayGoalsFullTime: item.Score?.Fulltime?.Away));
        }

        return result;
    }

    public async Task<IReadOnlyList<ProviderTeam>> GetTeamsAsync(string leagueId, int season, CancellationToken ct)
    {
        var url = $"teams?league={Uri.EscapeDataString(leagueId)}&season={season}";
        var payload = await _http.GetFromJsonAsync<ApiFootballTeamsResponse>(url, JsonOpts, ct);

        var items = payload?.Response ?? new List<ApiFootballTeamItem>();
        return items
            .Select(r => new ProviderTeam(
                r.Team.Id.ToString(CultureInfo.InvariantCulture),
                r.Team.Name,
                r.Team.Code))
            .ToList();
    }

    private ProviderStatus MapStatus(string shortCode)
    {
        switch (shortCode)
        {
            case "NS":
            case "TBD":
                return ProviderStatus.Scheduled;
            case "1H":
            case "HT":
            case "2H":
            case "ET":
            case "BT":
            case "P":
            case "SUSP":
            case "INT":
            case "LIVE":
                return ProviderStatus.Live;
            case "FT":
            case "AET":
            case "PEN":
                return ProviderStatus.Finished;
            case "PST":
                return ProviderStatus.Postponed;
            case "CANC":
                return ProviderStatus.Cancelled;
            case "ABD":
                return ProviderStatus.Abandoned;
            case "AWD":
            case "WO":
                return ProviderStatus.Awarded;
            default:
                _logger.LogWarning("Unknown api-football status code '{Code}', defaulting to Scheduled", shortCode);
                return ProviderStatus.Scheduled;
        }
    }
}
