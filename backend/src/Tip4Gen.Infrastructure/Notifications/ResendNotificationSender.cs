using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Notifications;

namespace Tip4Gen.Infrastructure.Notifications;

/// <summary>
/// Resend implementation of <see cref="INotificationSender"/>. POSTs to
/// <c>/emails</c> with a JSON body; on 200 the message id comes back in the response.
///
/// Mirrors <c>OpenAiTipper</c>: short-circuits to <c>Disabled</c> when ApiKey is empty,
/// uses a typed HttpClient with <c>AddStandardResilienceHandler</c> for transient retries,
/// translates 429 → <c>RateLimited</c> and other 4xx/5xx → <c>Failed</c>.
/// </summary>
public class ResendNotificationSender(
    HttpClient http,
    IOptions<ResendOptions> options,
    ILogger<ResendNotificationSender> logger) : INotificationSender
{
    public async Task<NotificationSendResult> SendAsync(NotificationEmail email, CancellationToken ct)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogDebug("Resend ApiKey not configured; notification sender disabled.");
            return new NotificationSendResult.Disabled();
        }

        var body = new ResendRequest(
            From: opts.FromAddress,
            To: [email.ToEmail],
            Subject: email.Subject,
            Html: email.HtmlBody,
            Text: email.TextBody);

        try
        {
            using var resp = await http.PostAsJsonAsync("emails", body, ct);
            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Resend rate-limited (429): {Body}", err);
                return new NotificationSendResult.RateLimited($"HTTP 429: {Truncate(err, 200)}");
            }
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Resend returned {Status} for {To}: {Body}", resp.StatusCode, email.ToEmail, err);
                return new NotificationSendResult.Failed($"HTTP {(int)resp.StatusCode}: {Truncate(err, 200)}");
            }

            var payload = await resp.Content.ReadFromJsonAsync<ResendResponse>(cancellationToken: ct);
            return new NotificationSendResult.Success(payload?.Id ?? "(no id)");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new NotificationSendResult.Failed("Resend HTTP timeout");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Resend call threw for {To}", email.ToEmail);
            return new NotificationSendResult.Failed(ex.GetType().Name + ": " + Truncate(ex.Message, 200));
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private sealed record ResendRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("text")] string Text);

    private sealed class ResendResponse
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
    }
}
