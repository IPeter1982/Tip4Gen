using Tip4Gen.Domain.Ai;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Tests.Ai;

public class AiTipPromptBuilderTests
{
    [Fact]
    public void System_prompt_requires_strict_json_shape_and_hungarian_reasoning()
    {
        var p = AiTipPromptBuilder.Build("Magyarország", "Németország", Stage.Group, AiMode.Balanced);

        Assert.Contains("home_goals", p.System);
        Assert.Contains("away_goals", p.System);
        Assert.Contains("reasoning", p.System);
        Assert.Contains("Hungarian", p.System);
        Assert.Contains("500", p.System);
    }

    [Fact]
    public void User_prompt_includes_both_teams_and_stage()
    {
        var p = AiTipPromptBuilder.Build("Magyarország", "Németország", Stage.QF, AiMode.Conservative);

        Assert.Contains("Magyarország", p.User);
        Assert.Contains("Németország", p.User);
        Assert.Contains("quarter-final", p.User);
    }

    [Theory]
    [InlineData(AiMode.Conservative, "conservative")]
    [InlineData(AiMode.Balanced, "balanced")]
    [InlineData(AiMode.Bold, "bold")]
    public void User_prompt_reflects_mode(AiMode mode, string expected)
    {
        var p = AiTipPromptBuilder.Build("A", "B", Stage.Group, mode);
        Assert.Contains(expected, p.User);
    }

    [Theory]
    [InlineData(Stage.Group, "group")]
    [InlineData(Stage.R32, "round of 32")]
    [InlineData(Stage.R16, "round of 16")]
    [InlineData(Stage.QF, "quarter-final")]
    [InlineData(Stage.SF, "semi-final")]
    [InlineData(Stage.Bronze, "third-place")]
    [InlineData(Stage.Final, "final")]
    public void Every_stage_has_a_human_readable_label(Stage stage, string fragment)
    {
        var p = AiTipPromptBuilder.Build("A", "B", stage, AiMode.Balanced);
        Assert.Contains(fragment, p.User);
    }
}
