using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Tests.Tournaments;

public class StageMapperTests
{
    [Theory]
    [InlineData("group", Stage.Group)]
    [InlineData("r32", Stage.R32)]
    [InlineData("r16", Stage.R16)]
    [InlineData("qf", Stage.QF)]
    [InlineData("sf", Stage.SF)]
    [InlineData("third", Stage.Bronze)]
    [InlineData("final", Stage.Final)]
    public void World_cup_type_codes_map_to_correct_stage(string type, Stage expected)
    {
        Assert.Equal(expected, StageMapper.FromWorldCupType(type));
    }

    [Theory]
    [InlineData("GROUP")]
    [InlineData("R32")]
    [InlineData("Final")]
    [InlineData("  group  ")]
    public void Type_lookup_is_case_insensitive_and_trims_whitespace(string type)
    {
        // The worldcup26.ir payload is lowercase in practice; tolerating mixed
        // case keeps us robust to upstream formatting drift.
        Assert.True(Enum.IsDefined(StageMapper.FromWorldCupType(type)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_type_throws(string type)
    {
        Assert.Throws<ArgumentException>(() => StageMapper.FromWorldCupType(type));
    }

    [Fact]
    public void Null_type_throws()
    {
        Assert.Throws<ArgumentException>(() => StageMapper.FromWorldCupType(null!));
    }

    [Fact]
    public void Unrecognized_type_throws()
    {
        Assert.Throws<ArgumentException>(() => StageMapper.FromWorldCupType("playoff"));
    }
}
