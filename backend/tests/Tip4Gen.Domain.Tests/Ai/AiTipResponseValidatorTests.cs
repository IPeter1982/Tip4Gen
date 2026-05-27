using Tip4Gen.Domain.Ai;

namespace Tip4Gen.Domain.Tests.Ai;

public class AiTipResponseValidatorTests
{
    [Fact]
    public void Valid_response_is_accepted_and_trimmed()
    {
        var result = AiTipResponseValidator.Validate(2, 1, "  Magyarország nyer kettő egy ellen.  ");

        var valid = Assert.IsType<AiTipValidationResult.Valid>(result);
        Assert.Equal(2, valid.Response.HomeGoals);
        Assert.Equal(1, valid.Response.AwayGoals);
        Assert.Equal("Magyarország nyer kettő egy ellen.", valid.Response.Reasoning);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(-1, 1)]
    [InlineData(16, 1)]
    [InlineData(1, null)]
    [InlineData(1, -1)]
    [InlineData(1, 16)]
    public void Out_of_range_goals_are_rejected(int? home, int? away)
    {
        var result = AiTipResponseValidator.Validate(home, away, "indoklás");
        Assert.IsType<AiTipValidationResult.Invalid>(result);
    }

    [Fact]
    public void Empty_reasoning_is_rejected()
    {
        var result = AiTipResponseValidator.Validate(1, 1, "   ");
        Assert.IsType<AiTipValidationResult.Invalid>(result);
    }

    [Fact]
    public void Null_reasoning_is_rejected()
    {
        var result = AiTipResponseValidator.Validate(1, 1, null);
        Assert.IsType<AiTipValidationResult.Invalid>(result);
    }

    [Fact]
    public void Reasoning_at_max_length_is_accepted()
    {
        var reasoning = new string('x', AiTipResponseValidator.MaxReasoningLength);
        var result = AiTipResponseValidator.Validate(0, 0, reasoning);
        Assert.IsType<AiTipValidationResult.Valid>(result);
    }

    [Fact]
    public void Reasoning_over_max_length_is_rejected()
    {
        var reasoning = new string('x', AiTipResponseValidator.MaxReasoningLength + 1);
        var result = AiTipResponseValidator.Validate(0, 0, reasoning);
        Assert.IsType<AiTipValidationResult.Invalid>(result);
    }

    [Fact]
    public void Boundary_goal_values_are_accepted()
    {
        Assert.IsType<AiTipValidationResult.Valid>(
            AiTipResponseValidator.Validate(0, AiTipResponseValidator.MaxGoals, "x"));
        Assert.IsType<AiTipValidationResult.Valid>(
            AiTipResponseValidator.Validate(AiTipResponseValidator.MaxGoals, 0, "x"));
    }
}
