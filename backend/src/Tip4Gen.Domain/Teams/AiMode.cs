namespace Tip4Gen.Domain.Teams;

/// <summary>Tipping behaviour for the AI team member, per guide §7.</summary>
public enum AiMode
{
    /// <summary>Ranking + form, narrow goal differences, rarely draws.</summary>
    Conservative = 0,

    /// <summary>Most likely result, occasional upsets.</summary>
    Balanced = 1,

    /// <summary>More draws, more upsets, higher goal counts.</summary>
    Bold = 2,
}
