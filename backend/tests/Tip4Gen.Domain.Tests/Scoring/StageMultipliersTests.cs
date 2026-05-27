using Tip4Gen.Domain.Scoring;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Tests.Scoring;

public class StageMultipliersTests
{
    [Theory]
    [InlineData(Stage.Group, 1.0)]
    [InlineData(Stage.R32, 1.5)]
    [InlineData(Stage.R16, 1.5)]
    [InlineData(Stage.QF, 2.0)]
    [InlineData(Stage.SF, 2.5)]
    [InlineData(Stage.Bronze, 2.0)]
    [InlineData(Stage.Final, 3.0)]
    public void Returns_expected_multiplier(Stage stage, double expected)
    {
        Assert.Equal((decimal)expected, StageMultipliers.For(stage));
    }

    [Fact]
    public void Throws_on_unknown_stage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StageMultipliers.For((Stage)99));
    }
}
