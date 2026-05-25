using Tip4Gen.Domain.Football;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Tests.Tournaments;

public class MatchStatusMapperTests
{
    [Theory]
    [InlineData(ProviderStatus.Scheduled, MatchStatus.Scheduled)]
    [InlineData(ProviderStatus.Live, MatchStatus.Live)]
    [InlineData(ProviderStatus.Finished, MatchStatus.Finished)]
    [InlineData(ProviderStatus.Postponed, MatchStatus.Postponed)]
    [InlineData(ProviderStatus.Cancelled, MatchStatus.Cancelled)]
    [InlineData(ProviderStatus.Abandoned, MatchStatus.Abandoned)]
    [InlineData(ProviderStatus.Awarded, MatchStatus.Awarded)]
    public void Every_provider_status_maps_to_match_status(ProviderStatus input, MatchStatus expected)
    {
        Assert.Equal(expected, MatchStatusMapper.FromProvider(input));
    }

    [Fact]
    public void Unknown_value_throws()
    {
        var bogus = (ProviderStatus)99;
        Assert.Throws<ArgumentOutOfRangeException>(() => MatchStatusMapper.FromProvider(bogus));
    }
}
