using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Tipping;

public enum TipRejectionReason
{
    None = 0,
    DeadlinePassed,
    ScoreOutOfRange,
    JokerNotAllowedOnKnockoutMatch,
    JokerQuotaExceeded,
}

public record TipValidationResult(bool IsValid, TipRejectionReason Reason, string? Message)
{
    public static readonly TipValidationResult Ok = new(true, TipRejectionReason.None, null);

    public static TipValidationResult Fail(TipRejectionReason reason, string message)
        => new(false, reason, message);
}

public static class TipRulesValidator
{
    public const int MaxJokersPerUser = 3;
    public const int MaxScore = 15;
    public static readonly TimeSpan DeadlineBeforeKickoff = TimeSpan.FromHours(1);

    /// <summary>
    /// Validate a tip submission. All inputs are facts the caller has gathered;
    /// this method is pure so it can be unit-tested without a DB.
    /// </summary>
    /// <param name="now">Current UTC time at decision moment.</param>
    /// <param name="matchKickoffUtc">Match kickoff in UTC.</param>
    /// <param name="matchStage">Stage of the match being tipped.</param>
    /// <param name="homeGoals">Tipped home score.</param>
    /// <param name="awayGoals">Tipped away score.</param>
    /// <param name="usingJoker">True if this tip claims a joker.</param>
    /// <param name="otherJokerCountForUser">Joker count across the user's OTHER tips
    /// (i.e., excluding the tip currently being upserted). Caller must exclude
    /// the current match to make update flows correct.</param>
    public static TipValidationResult Validate(
        DateTimeOffset now,
        DateTimeOffset matchKickoffUtc,
        Stage matchStage,
        int homeGoals,
        int awayGoals,
        bool usingJoker,
        int otherJokerCountForUser)
    {
        var deadline = matchKickoffUtc - DeadlineBeforeKickoff;
        if (now >= deadline)
        {
            return TipValidationResult.Fail(
                TipRejectionReason.DeadlinePassed,
                $"Tippelési határidő lejárt (kezdés előtt 1 órával zár).");
        }

        if (homeGoals < 0 || homeGoals > MaxScore || awayGoals < 0 || awayGoals > MaxScore)
        {
            return TipValidationResult.Fail(
                TipRejectionReason.ScoreOutOfRange,
                $"A gólszám 0 és {MaxScore} közötti kell legyen.");
        }

        if (usingJoker)
        {
            if (matchStage != Stage.Group)
            {
                return TipValidationResult.Fail(
                    TipRejectionReason.JokerNotAllowedOnKnockoutMatch,
                    "Joker csak csoportkörös meccsre játszható ki.");
            }

            if (otherJokerCountForUser >= MaxJokersPerUser)
            {
                return TipValidationResult.Fail(
                    TipRejectionReason.JokerQuotaExceeded,
                    $"Már elhasználtad mind a(z) {MaxJokersPerUser} jokeredet.");
            }
        }

        return TipValidationResult.Ok;
    }
}
