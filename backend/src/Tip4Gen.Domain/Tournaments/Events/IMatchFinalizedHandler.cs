namespace Tip4Gen.Domain.Tournaments.Events;

public interface IMatchFinalizedHandler
{
    Task HandleAsync(MatchFinalized @event, CancellationToken ct);
}
