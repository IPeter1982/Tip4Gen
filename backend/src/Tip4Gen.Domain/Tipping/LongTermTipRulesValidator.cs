namespace Tip4Gen.Domain.Tipping;

public enum LongTermTipRejectionReason
{
    None = 0,
    Locked,
    NothingProvided,
    PlayerNameTooLong,
    PlayerNameBlank,
}

public record LongTermTipValidationResult(bool IsValid, LongTermTipRejectionReason Reason, string? Message)
{
    public static readonly LongTermTipValidationResult Ok = new(true, LongTermTipRejectionReason.None, null);

    public static LongTermTipValidationResult Fail(LongTermTipRejectionReason reason, string message)
        => new(false, reason, message);
}

public static class LongTermTipRulesValidator
{
    public const int MaxPlayerNameLength = 120;

    public static LongTermTipValidationResult Validate(
        DateTimeOffset now,
        DateTimeOffset tournamentStartsAtUtc,
        Guid? winnerTeamId,
        string? topScorerName)
    {
        if (now >= tournamentStartsAtUtc)
        {
            return LongTermTipValidationResult.Fail(
                LongTermTipRejectionReason.Locked,
                "Végső győztes tippek lezárultak (a torna első mérkőzésénél).");
        }

        var providingWinner = winnerTeamId.HasValue;
        var providingTopScorer = topScorerName is not null;

        if (!providingWinner && !providingTopScorer)
        {
            return LongTermTipValidationResult.Fail(
                LongTermTipRejectionReason.NothingProvided,
                "Legalább egy tipp (győztes vagy gólkirály) szükséges.");
        }

        if (providingTopScorer)
        {
            if (string.IsNullOrWhiteSpace(topScorerName))
            {
                return LongTermTipValidationResult.Fail(
                    LongTermTipRejectionReason.PlayerNameBlank,
                    "A gólkirály neve nem lehet üres.");
            }

            if (topScorerName.Length > MaxPlayerNameLength)
            {
                return LongTermTipValidationResult.Fail(
                    LongTermTipRejectionReason.PlayerNameTooLong,
                    $"A gólkirály neve max {MaxPlayerNameLength} karakter.");
            }
        }

        return LongTermTipValidationResult.Ok;
    }
}
