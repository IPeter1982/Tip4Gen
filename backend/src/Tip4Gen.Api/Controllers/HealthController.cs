using Microsoft.AspNetCore.Mvc;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        timestamp = DateTimeOffset.UtcNow,
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "0.0.0",
    });
}
