using Tip4Gen.Domain.Teams;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Ai;

/// <summary>
/// External AI tipper. One call per (team_member, match) attempt. The interface stays
/// in Domain so the orchestrator can be tested without a network mock.
/// </summary>
public interface IAiTipper
{
    Task<AiTipResult> GenerateAsync(
        string homeTeamName,
        string awayTeamName,
        Stage stage,
        AiMode mode,
        CancellationToken ct);
}
