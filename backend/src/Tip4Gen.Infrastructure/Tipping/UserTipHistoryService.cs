using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Scoring;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Infrastructure.Tipping;

public sealed record UserTipHistoryTeam(Guid Id, string Name, string? Code);

public sealed record UserTipScore(
    ScoreCategory Category,
    int BasePoints,
    decimal Multiplier,
    bool JokerApplied,
    int FinalPoints);

public sealed record UserTipDetail(
    int HomeGoals,
    int AwayGoals,
    bool Joker,
    DateTimeOffset SubmittedAt,
    UserTipScore? Score);

public sealed record UserTipHistoryItem(
    Guid MatchId,
    Stage Stage,
    string? GroupCode,
    string? RoundLabel,
    UserTipHistoryTeam HomeTeam,
    UserTipHistoryTeam AwayTeam,
    DateTimeOffset KickoffUtc,
    MatchStatus Status,
    int? HomeGoals,
    int? AwayGoals,
    UserTipDetail Tip);

public sealed record UserTipHistoryResponse(
    Guid UserId,
    string DisplayName,
    string? AvatarVersion,
    int TotalPoints,
    IReadOnlyList<UserTipHistoryItem> Items);

public interface IUserTipHistoryService
{
    Task<UserTipHistoryResponse?> GetAsync(Guid userId, CancellationToken ct);
}

public class UserTipHistoryService(AppDbContext db) : IUserTipHistoryService
{
    private static readonly MatchStatus[] FinalStatuses =
    {
        MatchStatus.Finished,
        MatchStatus.Awarded,
        MatchStatus.Cancelled,
        MatchStatus.Abandoned,
    };

    public async Task<UserTipHistoryResponse?> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.DisplayName, u.AvatarVersion })
            .FirstOrDefaultAsync(ct);
        if (user is null) return null;

        var rows = await (
            from t in db.Tips.AsNoTracking()
            where t.UserId == userId
            join m in db.Matches.AsNoTracking() on t.MatchId equals m.Id
            where FinalStatuses.Contains(m.Status)
            join home in db.NationalTeams.AsNoTracking() on m.HomeTeamId equals home.Id
            join away in db.NationalTeams.AsNoTracking() on m.AwayTeamId equals away.Id
            join st in db.ScoredTips.AsNoTracking() on t.Id equals st.TipId into stJoin
            from st in stJoin.DefaultIfEmpty()
            orderby m.KickoffUtc
            select new
            {
                m.Id,
                m.Stage,
                m.GroupCode,
                m.RoundLabel,
                HomeId = home.Id,
                HomeName = home.Name,
                HomeCode = home.Code,
                AwayId = away.Id,
                AwayName = away.Name,
                AwayCode = away.Code,
                m.KickoffUtc,
                m.Status,
                m.HomeGoals,
                m.AwayGoals,
                TipHome = t.HomeGoals,
                TipAway = t.AwayGoals,
                t.Joker,
                t.SubmittedAt,
                ScoreCategory = (ScoreCategory?)(st != null ? st.Category : null),
                ScoreBase = (int?)(st != null ? st.BasePoints : null),
                ScoreMultiplier = (decimal?)(st != null ? st.Multiplier : null),
                ScoreJokerApplied = (bool?)(st != null ? st.JokerApplied : null),
                ScoreFinal = (int?)(st != null ? st.FinalPoints : null),
            }
        ).ToListAsync(ct);

        var items = rows.Select(r =>
        {
            UserTipScore? score = r.ScoreCategory.HasValue
                ? new UserTipScore(
                    r.ScoreCategory.Value,
                    r.ScoreBase!.Value,
                    r.ScoreMultiplier!.Value,
                    r.ScoreJokerApplied!.Value,
                    r.ScoreFinal!.Value)
                : null;

            return new UserTipHistoryItem(
                r.Id,
                r.Stage,
                r.GroupCode,
                r.RoundLabel,
                new UserTipHistoryTeam(r.HomeId, r.HomeName, r.HomeCode),
                new UserTipHistoryTeam(r.AwayId, r.AwayName, r.AwayCode),
                r.KickoffUtc,
                r.Status,
                r.HomeGoals,
                r.AwayGoals,
                new UserTipDetail(r.TipHome, r.TipAway, r.Joker, r.SubmittedAt, score));
        }).ToList();

        var total = items.Sum(i => i.Tip.Score?.FinalPoints ?? 0);

        return new UserTipHistoryResponse(user.Id, user.DisplayName, user.AvatarVersion, total, items);
    }
}
