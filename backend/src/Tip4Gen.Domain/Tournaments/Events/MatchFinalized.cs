namespace Tip4Gen.Domain.Tournaments.Events;

public record MatchFinalized(Guid MatchId, Guid TournamentId, DateTimeOffset OccurredAt);
