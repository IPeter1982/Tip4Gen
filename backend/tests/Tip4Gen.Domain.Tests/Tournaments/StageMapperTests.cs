using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Tests.Tournaments;

public class StageMapperTests
{
    [Theory]
    [InlineData("Group A - 1", "A")]
    [InlineData("Group B - 2", "B")]
    [InlineData("Group C - 3", "C")]
    [InlineData("Group L - 1", "L")]
    [InlineData("group h - 3", "H")]  // case insensitive
    [InlineData("  Group D - 2  ", "D")]  // surrounding whitespace
    public void Group_labels_with_letter_resolve_to_Group_with_extracted_code(string label, string expectedCode)
    {
        var (stage, group) = StageMapper.FromProviderLabel(label);
        Assert.Equal(Stage.Group, stage);
        Assert.Equal(expectedCode, group);
    }

    [Theory]
    [InlineData("Group Stage - 1")]
    [InlineData("Group Stage - 2")]
    [InlineData("Group Stage - 3")]
    [InlineData("group stage - 1")]
    [InlineData("  Group Stage - 2  ")]
    public void Matchday_group_labels_resolve_to_Group_with_null_code(string label)
    {
        // api-football's WC fixtures use "Group Stage - N" — the group letter
        // has to be enriched from /standings, not parsed from the round label.
        var (stage, group) = StageMapper.FromProviderLabel(label);
        Assert.Equal(Stage.Group, stage);
        Assert.Null(group);
    }

    [Theory]
    [InlineData("Round of 32", Stage.R32)]
    [InlineData("1/16 Finals", Stage.R32)]
    [InlineData("Round of 16", Stage.R16)]
    [InlineData("1/8 Finals", Stage.R16)]
    [InlineData("Quarter-finals", Stage.QF)]
    [InlineData("quarterfinals", Stage.QF)]
    [InlineData("Semi-finals", Stage.SF)]
    [InlineData("3rd Place Final", Stage.Bronze)]
    [InlineData("Third Place", Stage.Bronze)]
    [InlineData("Bronze Final", Stage.Bronze)]
    [InlineData("Final", Stage.Final)]
    public void Knockout_labels_resolve_to_correct_stage_with_no_group_code(string label, Stage expected)
    {
        var (stage, group) = StageMapper.FromProviderLabel(label);
        Assert.Equal(expected, stage);
        Assert.Null(group);
    }

    [Fact]
    public void Bronze_takes_precedence_over_generic_Final_match()
    {
        // "3rd Place Final" contains the word "final" — make sure we return Bronze, not Final.
        var (stage, _) = StageMapper.FromProviderLabel("3rd Place Final");
        Assert.Equal(Stage.Bronze, stage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_label_throws(string label)
    {
        Assert.Throws<ArgumentException>(() => StageMapper.FromProviderLabel(label));
    }

    [Fact]
    public void Null_label_throws()
    {
        Assert.Throws<ArgumentException>(() => StageMapper.FromProviderLabel(null!));
    }

    [Fact]
    public void Unrecognized_label_throws()
    {
        Assert.Throws<ArgumentException>(() => StageMapper.FromProviderLabel("Pre-Season Friendly"));
    }
}
