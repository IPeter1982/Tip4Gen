using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Admin;
using Tip4Gen.Infrastructure.Persistence;
using Tip4Gen.Infrastructure.Players;

namespace Tip4Gen.Api.Controllers;

public record PlayersImportResponse(int Added, int Skipped, int UnmatchedTeams, int TotalAfter, int DurationMs);

public record LastImportInfo(DateTimeOffset OccurredAt, string? AfterJson);

[ApiController]
[Route("api/admin/players")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class PlayersAdminController(
    CurrentUserService currentUser,
    IPlayersImportService importer,
    AppDbContext db) : ControllerBase
{
    [HttpPost("import")]
    public async Task<IActionResult> Import(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await importer.ImportAsync(admin.Id, ct);

        return result switch
        {
            PlayersImportResult.Success s => Ok(new PlayersImportResponse(
                s.Summary.Added, s.Summary.Skipped, s.Summary.UnmatchedTeams,
                s.Summary.TotalAfter, (int)stopwatch.ElapsedMilliseconds)),

            PlayersImportResult.ProviderFailure f => Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Wikipedia provider failed",
                detail: f.Message,
                extensions: new Dictionary<string, object?> { ["reason"] = "ProviderFailure" }),

            _ => throw new InvalidOperationException($"Unhandled PlayersImportResult: {result.GetType().Name}"),
        };
    }

    [HttpGet("last-import")]
    public async Task<IActionResult> LastImport(CancellationToken ct)
    {
        var row = await db.AdminAudits.AsNoTracking()
            .Where(a => a.Action == AdminAuditAction.PlayersImported)
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => new LastImportInfo(a.OccurredAt, a.AfterJson))
            .FirstOrDefaultAsync(ct);
        return Ok(row);
    }
}
