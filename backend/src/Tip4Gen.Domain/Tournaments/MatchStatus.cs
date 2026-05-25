namespace Tip4Gen.Domain.Tournaments;

public enum MatchStatus
{
    Scheduled = 0,
    Live = 1,
    Finished = 2,
    Postponed = 3,
    Cancelled = 4,
    Abandoned = 5,
    Awarded = 6
}
