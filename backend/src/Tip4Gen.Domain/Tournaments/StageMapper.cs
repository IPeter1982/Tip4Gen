namespace Tip4Gen.Domain.Tournaments;

public static class StageMapper
{
    public static Stage FromWorldCupType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Match type must not be empty.", nameof(type));

        return type.Trim().ToLowerInvariant() switch
        {
            "group" => Stage.Group,
            "r32" => Stage.R32,
            "r16" => Stage.R16,
            "qf" => Stage.QF,
            "sf" => Stage.SF,
            "third" => Stage.Bronze,
            "final" => Stage.Final,
            _ => throw new ArgumentException($"Unrecognized match type: '{type}'.", nameof(type)),
        };
    }
}
