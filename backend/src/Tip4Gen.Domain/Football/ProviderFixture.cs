namespace Tip4Gen.Domain.Football;

public record ProviderFixture(
    string ExternalId,
    DateTimeOffset KickoffUtc,
    string RoundLabel,
    string HomeTeamExternalId,
    string HomeTeamName,
    string AwayTeamExternalId,
    string AwayTeamName,
    ProviderStatus Status,
    int? HomeGoalsFullTime,
    int? AwayGoalsFullTime);
