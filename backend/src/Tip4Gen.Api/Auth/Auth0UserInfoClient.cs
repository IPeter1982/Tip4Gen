using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Tip4Gen.Api.Auth;

/// <summary>
/// Fetches <c>/userinfo</c> from the Auth0 tenant on behalf of the current request.
/// Auth0 access tokens (audience = our API) include <c>sub</c> but not <c>email</c>;
/// the email is on the ID token or the userinfo response. The notifications worker
/// needs an email column on the users row, so we call userinfo once per user (when
/// <c>users.email</c> is null) and persist.
/// </summary>
public interface IAuth0UserInfoClient
{
    Task<string?> FetchEmailAsync(string accessToken, CancellationToken ct);
}

public class Auth0UserInfoClient(HttpClient http, ILogger<Auth0UserInfoClient> logger) : IAuth0UserInfoClient
{
    public async Task<string?> FetchEmailAsync(string accessToken, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "userinfo");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("Auth0 /userinfo returned {Status}", resp.StatusCode);
                return null;
            }
            var body = await resp.Content.ReadFromJsonAsync<UserInfoResponse>(cancellationToken: ct);
            return string.IsNullOrWhiteSpace(body?.Email) ? null : body.Email;
        }
        catch (Exception ex)
        {
            // Failures here shouldn't break the request — the user just won't get
            // notification emails until the next login retries the lookup.
            logger.LogWarning(ex, "Auth0 /userinfo call threw");
            return null;
        }
    }

    private sealed class UserInfoResponse
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("email_verified")] public bool? EmailVerified { get; set; }
    }
}
