using Tip4Gen.Domain.Teams;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Ai;

/// <summary>
/// Pure prompt assembly. Kept minimal (team names + stage + mode) per Phase 6
/// scope — recent form / standings can be folded in later without changing the
/// IAiTipper contract.
/// </summary>
public static class AiTipPromptBuilder
{
    public sealed record AiPrompt(string System, string User);

    public static AiPrompt Build(string homeTeamName, string awayTeamName, Stage stage, AiMode mode)
    {
        var modeLabel = mode switch
        {
            AiMode.Conservative => "conservative — favour ranking and form; narrow goal differences; draws are uncommon",
            AiMode.Balanced => "balanced — pick the most-likely result; occasional upsets",
            AiMode.Bold => "bold — more draws, more upsets, higher goal counts",
            _ => "balanced",
        };

        var system =
            "You predict the final score of a single football (soccer) match at the 2026 FIFA World Cup for a Hungarian tipping game. " +
            "Scoring per tip (the prediction is judged after the match): exact score = 10 points; " +
            "correct winner and correct goal difference = 5 points; correct winner only = 3 points; " +
            "one team's exact goal count matches (strictly home-to-home or away-to-away, never swapped) = 1 point; " +
            "otherwise = 0 points. Pick the scoreline that maximises expected value within the requested style — " +
            "a riskier exact-score guess only pays off if it lands; a safe winner-only pick still earns 3. " +
            "WC 2026 host nations are USA, Mexico, and Canada — only they have actual home-crowd advantage. " +
            "For every other fixture the 'home' / 'away' label is administrative only and should not bias your prediction. " +
            "Respond with strict JSON only, in this exact shape: " +
            "{\"home_goals\": int, \"away_goals\": int, \"reasoning\": string}. " +
            "Goals must be integers 0–15. Reasoning must be written in Hungarian and stay under 500 characters. " +
            "Do not include any text outside the JSON object.";

        var user =
            $"Match: {homeTeamName} (home) vs {awayTeamName} (away). " +
            $"Stage: {StageLabel(stage)}. " +
            $"Style: {modeLabel}.";

        return new AiPrompt(system, user);
    }

    private static string StageLabel(Stage stage) => stage switch
    {
        Stage.Group => "group stage",
        Stage.R32 => "round of 32",
        Stage.R16 => "round of 16",
        Stage.QF => "quarter-final",
        Stage.SF => "semi-final",
        Stage.Bronze => "third-place play-off",
        Stage.Final => "final",
        _ => stage.ToString(),
    };
}
