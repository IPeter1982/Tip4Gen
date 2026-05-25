namespace Tip4Gen.Domain.Football;

public interface IFootballDataProvider
{
    Task<IReadOnlyList<ProviderFixture>> GetFixturesAsync(string leagueId, int season, CancellationToken ct);

    Task<IReadOnlyList<ProviderTeam>> GetTeamsAsync(string leagueId, int season, CancellationToken ct);
}
