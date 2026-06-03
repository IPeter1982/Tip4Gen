using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tip4Gen.Infrastructure.Football;

/// <summary>
/// Lazy JWT token holder for worldcup26.ir. Anonymous calls are the default;
/// this cache is only touched when the upstream returns 401, at which point we
/// register-or-login with the credentials in <see cref="WorldCup26IrOptions"/>
/// (if configured) and cache the 84-day token in memory.
/// </summary>
public sealed class WorldCup26IrJwtCache
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IOptions<WorldCup26IrOptions> _opts;
    private readonly ILogger<WorldCup26IrJwtCache> _logger;
    private string? _token;

    public WorldCup26IrJwtCache(IOptions<WorldCup26IrOptions> opts, ILogger<WorldCup26IrJwtCache> logger)
    {
        _opts = opts;
        _logger = logger;
    }

    public string? CurrentToken => _token;

    /// <summary>
    /// Returns a token, fetching one if necessary. Returns null when no credentials
    /// are configured (callers should treat that as "anonymous mode, give up").
    /// </summary>
    public async Task<string?> GetOrRefreshAsync(HttpClient http, CancellationToken ct)
    {
        var opts = _opts.Value;
        if (string.IsNullOrWhiteSpace(opts.AuthEmail) || string.IsNullOrWhiteSpace(opts.AuthPassword))
            return null;

        if (!string.IsNullOrEmpty(_token))
            return _token;

        await _gate.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrEmpty(_token))
                return _token;

            _token = await AcquireAsync(http, opts.AuthEmail, opts.AuthPassword, ct);
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate()
    {
        _token = null;
    }

    private async Task<string?> AcquireAsync(HttpClient http, string email, string password, CancellationToken ct)
    {
        var login = await TryLoginAsync(http, email, password, ct);
        if (login is not null)
            return login;

        // Login failed — try to register, then login again. Register may itself fail
        // (account exists, validation, etc.); we treat 4xx as terminal and return null
        // so the caller can fall through to anonymous-mode failure.
        _logger.LogInformation("worldcup26.ir login failed; attempting register-then-login");
        try
        {
            using var registerResp = await http.PostAsJsonAsync(
                "auth/register",
                new AuthRegisterRequest("Tip4Gen", email, password),
                JsonOpts,
                ct);
            if (!registerResp.IsSuccessStatusCode && registerResp.StatusCode != HttpStatusCode.Conflict)
            {
                _logger.LogWarning(
                    "worldcup26.ir register returned {Status}; abandoning JWT acquisition",
                    registerResp.StatusCode);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "worldcup26.ir register request threw");
            return null;
        }

        return await TryLoginAsync(http, email, password, ct);
    }

    private async Task<string?> TryLoginAsync(HttpClient http, string email, string password, CancellationToken ct)
    {
        try
        {
            using var resp = await http.PostAsJsonAsync(
                "auth/authenticate",
                new AuthLoginRequest(email, password),
                JsonOpts,
                ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            var payload = await resp.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOpts, ct);
            return payload?.Token ?? payload?.AccessToken;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "worldcup26.ir authenticate request threw");
            return null;
        }
    }
}
