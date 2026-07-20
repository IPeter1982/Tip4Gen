using Tip4Gen.Domain.Scoring;

namespace Tip4Gen.Domain.Tests.Scoring;

public class LongTipBonusTests
{
    [Fact]
    public void Both_null_returns_zero()
    {
        Assert.Equal(0, LongTipBonus.Compute(null, null));
    }

    [Fact]
    public void Winner_only_returns_fifty()
    {
        Assert.Equal(50, LongTipBonus.Compute(true, null));
        Assert.Equal(50, LongTipBonus.Compute(true, false));
    }

    [Fact]
    public void Top_scorer_only_returns_thirty()
    {
        Assert.Equal(30, LongTipBonus.Compute(null, true));
        Assert.Equal(30, LongTipBonus.Compute(false, true));
    }

    [Fact]
    public void Both_correct_returns_eighty()
    {
        Assert.Equal(80, LongTipBonus.Compute(true, true));
    }

    [Fact]
    public void Both_false_returns_zero()
    {
        Assert.Equal(0, LongTipBonus.Compute(false, false));
    }

    [Fact]
    public void Constants_match_guide_section_5()
    {
        Assert.Equal(50, LongTipBonus.WinnerPoints);
        Assert.Equal(30, LongTipBonus.TopScorerPoints);
    }
}
