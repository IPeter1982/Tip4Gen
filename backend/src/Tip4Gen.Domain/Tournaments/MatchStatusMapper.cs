using Tip4Gen.Domain.Football;

namespace Tip4Gen.Domain.Tournaments;

public static class MatchStatusMapper
{
    public static MatchStatus FromProvider(ProviderStatus status) => status switch
    {
        ProviderStatus.Scheduled => MatchStatus.Scheduled,
        ProviderStatus.Live => MatchStatus.Live,
        ProviderStatus.Finished => MatchStatus.Finished,
        ProviderStatus.Postponed => MatchStatus.Postponed,
        ProviderStatus.Cancelled => MatchStatus.Cancelled,
        ProviderStatus.Abandoned => MatchStatus.Abandoned,
        ProviderStatus.Awarded => MatchStatus.Awarded,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown provider status."),
    };
}
