using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Scoring;

public static class StageMultipliers
{
    /// <summary>Per guide §4: Group 1× · R32 1.5× · R16 1.5× · QF 2× · SF 2.5× · Bronze 2× · Final 3×.</summary>
    public static decimal For(Stage stage) => stage switch
    {
        Stage.Group => 1.0m,
        Stage.R32 => 1.5m,
        Stage.R16 => 1.5m,
        Stage.QF => 2.0m,
        Stage.SF => 2.5m,
        Stage.Bronze => 2.0m,
        Stage.Final => 3.0m,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown stage"),
    };
}
