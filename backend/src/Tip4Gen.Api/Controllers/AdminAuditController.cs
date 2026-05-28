using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Admin;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Controllers;

public record AdminAuditRow(
    Guid Id,
    AdminAuditAction Action,
    string EntityType,
    Guid? EntityId,
    Guid AdminUserId,
    string AdminDisplayName,
    string? BeforeJson,
    string? AfterJson,
    string? Reason,
    DateTimeOffset OccurredAt);

public record AdminAuditResponse(int Total, int Take, int Skip, IReadOnlyList<AdminAuditRow> Rows);

[ApiController]
[Route("api/admin/audit")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class AdminAuditController(AppDbContext db) : ControllerBase
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? matchId,
        [FromQuery] string? entityType,
        [FromQuery] int? take,
        [FromQuery] int? skip,
        CancellationToken ct)
    {
        // Server-clamp paging so a hostile query string can't ToList the whole table.
        var safeTake = Math.Clamp(take ?? DefaultTake, 1, MaxTake);
        var safeSkip = Math.Max(0, skip ?? 0);

        var query = db.AdminAudits.AsNoTracking().AsQueryable();
        if (matchId is Guid mid)
            query = query.Where(a => a.EntityType == AdminAudit.EntityTypeMatch && a.EntityId == mid);
        else if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        var total = await query.CountAsync(ct);

        var rows = await (
            from a in query.OrderByDescending(a => a.OccurredAt).Skip(safeSkip).Take(safeTake)
            join u in db.Users.AsNoTracking() on a.AdminUserId equals u.Id
            select new AdminAuditRow(
                a.Id,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.AdminUserId,
                u.DisplayName,
                a.BeforeJson,
                a.AfterJson,
                a.Reason,
                a.OccurredAt)).ToListAsync(ct);

        return Ok(new AdminAuditResponse(total, safeTake, safeSkip, rows));
    }
}
