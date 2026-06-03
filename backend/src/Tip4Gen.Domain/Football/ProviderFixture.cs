using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Football;

public record ProviderFixture(
    string ExternalId,
    DateTimeOffset KickoffUtc,
    Stage Stage,
    string? GroupCode,
    string RoundLabel,
    string HomeTeamExternalId,
    string HomeTeamName,
    string AwayTeamExternalId,
    string AwayTeamName,
    ProviderStatus Status,
    int? HomeGoalsFullTime,
    int? AwayGoalsFullTime);
