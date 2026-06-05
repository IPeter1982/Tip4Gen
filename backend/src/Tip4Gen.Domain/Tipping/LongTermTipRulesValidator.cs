namespace Tip4Gen.Domain.Tipping;

public enum LongTermTipRejectionReason
{
    None = 0,
    Locked,
    NothingProvided,
}

public record LongTermTipValidationResult(bool IsValid, LongTermTipRejectionReason Reason, string? Message)
{
    public static readonly LongTermTipValidationResult Ok = new(true, LongTermTipRejectionReason.None, null);

    public static LongTermTipValidationResult Fail(LongTermTipRejectionReason reason, string message)
        => new(false, reason, message);
}

public static class LongTermTipRulesValidator
{
    public static LongTermTipValidationResult Validate(
        DateTimeOffset now,
        DateTimeOffset tournamentStartsAtUtc,
        Guid? winnerTeamId,
        Guid? topScorerPlayerId)
    {
        if (now >= tournamentStartsAtUtc)
        {
            return LongTermTipValidationResult.Fail(
                LongTermTipRejectionReason.Locked,
                "Végső győztes tippek lezárultak (a torna első mérkőzésénél).");
        }

        if (!winnerTeamId.HasValue && !topScorerPlayerId.HasValue)
        {
            return LongTermTipValidationResult.Fail(
                LongTermTipRejectionReason.NothingProvided,
                "Legalább egy tipp (győztes vagy gólkirály) szükséges.");
        }

        return LongTermTipValidationResult.Ok;
    }
}
