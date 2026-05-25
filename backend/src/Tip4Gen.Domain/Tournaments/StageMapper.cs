using System.Text.RegularExpressions;

namespace Tip4Gen.Domain.Tournaments;

public static class StageMapper
{
    private static readonly Regex GroupLabel = new(
        @"^\s*group\s+([a-l])\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static (Stage Stage, string? GroupCode) FromProviderLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Provider label must not be empty.", nameof(label));

        var groupMatch = GroupLabel.Match(label);
        if (groupMatch.Success)
            return (Stage.Group, groupMatch.Groups[1].Value.ToUpperInvariant());

        var normalized = label.Trim().ToLowerInvariant();

        if (normalized.Contains("round of 32") || normalized.Contains("1/16"))
            return (Stage.R32, null);

        if (normalized.Contains("round of 16") || normalized.Contains("1/8"))
            return (Stage.R16, null);

        if (normalized.Contains("quarter"))
            return (Stage.QF, null);

        if (normalized.Contains("semi"))
            return (Stage.SF, null);

        if (normalized.Contains("3rd place") || normalized.Contains("third place") || normalized.Contains("bronze"))
            return (Stage.Bronze, null);

        if (normalized.Contains("final"))
            return (Stage.Final, null);

        throw new ArgumentException($"Unrecognized stage label: '{label}'.", nameof(label));
    }
}
