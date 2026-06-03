using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Teams;

public record TeamLockSummary(int Locked, int Skipped);

public interface ITeamLockService
{
    /// <summary>
    /// Iterates every Forming team and applies <see cref="TeamLockPolicy"/> against the
    /// earliest tournament's start time. Locks any team that has reached
    /// <see cref="Team.MaxMembers"/> after tournament start; under-sized teams stay Forming
    /// so they can keep accepting joins. Safe to call on every tick — locking is one-way.
    /// </summary>
    Task<TeamLockSummary> LockAllAsync(CancellationToken ct);
}

public class TeamLockService(AppDbContext db, ILogger<TeamLockService> logger) : ITeamLockService
{
    public async Task<TeamLockSummary> LockAllAsync(CancellationToken ct)
    {
        var tournamentStart = await db.Tournaments
            .OrderBy(t => t.StartsAtUtc)
            .Select(t => (DateTimeOffset?)t.StartsAtUtc)
            .FirstOrDefaultAsync(ct);
        if (tournamentStart is null)
        {
            return new TeamLockSummary(0, 0);
        }

        var now = DateTimeOffset.UtcNow;
        var formingTeams = await db.Teams
            .Where(t => t.Status == TeamStatus.Forming)
            .ToListAsync(ct);
        if (formingTeams.Count == 0)
            return new TeamLockSummary(0, 0);

        var counts = await db.TeamMembers
            .Where(m => formingTeams.Select(t => t.Id).Contains(m.TeamId))
            .GroupBy(m => m.TeamId)
            .Select(g => new { TeamId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TeamId, x => x.Count, ct);

        int locked = 0, skipped = 0;
        foreach (var team in formingTeams)
        {
            var memberCount = counts.GetValueOrDefault(team.Id);
            var decision = TeamLockPolicy.Decide(now, tournamentStart.Value, team.Status, memberCount);
            switch (decision)
            {
                case TeamLockDecision.Lock:
                    team.Lock();
                    locked++;
                    logger.LogInformation("Team {TeamId} ({Name}) locked — full roster reached", team.Id, team.Name);
                    break;
                case TeamLockDecision.Skip:
                    skipped++;
                    break;
            }
        }

        await db.SaveChangesAsync(ct);
        return new TeamLockSummary(locked, skipped);
    }
}
