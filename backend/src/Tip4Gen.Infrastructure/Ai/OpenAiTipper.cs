using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Ai;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Infrastructure.Ai;

/// <summary>
/// OpenAI Chat Completions implementation of <see cref="IAiTipper"/>. Uses JSON mode
/// (response_format: json_object) so the model returns parseable content; we still
/// validate goals/reasoning through <see cref="AiTipResponseValidator"/>.
///
/// Resilience comes from the registered HttpClient handler (AddStandardResilienceHandler
/// — transient HTTP/429 retries with backoff). Timeout is driven by HttpClient.Timeout
/// from <see cref="OpenAiOptions.TimeoutSeconds"/>.
///
/// Scaffold-mode: when ApiKey is unset the tipper returns AiTipResult.Disabled without
/// making a request, so the orchestrator + fallback path can be exercised before the
/// key is in user-secrets.
/// </summary>
public class OpenAiTipper(
    HttpClient http,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiTipper> logger) : IAiTipper
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<AiTipResult> GenerateAsync(
        string homeTeamName,
        string awayTeamName,
        Stage stage,
        AiMode mode,
        CancellationToken ct)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogDebug(
                "OpenAI ApiKey not configured; AI tipper disabled (will fall back to 1–1 at T-1h). Match: {Home} vs {Away}",
                homeTeamName, awayTeamName);
            return new AiTipResult.Disabled();
        }

        var prompt = AiTipPromptBuilder.Build(homeTeamName, awayTeamName, stage, mode);
        var temperature = mode switch
        {
            AiMode.Conservative => 0.25,
            AiMode.Balanced => 0.55,
            AiMode.Bold => 0.85,
            _ => opts.Temperature,
        };
        var requestBody = new
        {
            model = opts.Model,
            messages = new[]
            {
                new { role = "system", content = prompt.System },
                new { role = "user", content = prompt.User },
            },
            response_format = new { type = "json_object" },
            temperature,
        };

        try
        {
            using var resp = await http.PostAsJsonAsync("chat/completions", requestBody, JsonOpts, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning("OpenAI returned {Status} for {Home} vs {Away}: {Body}",
                    resp.StatusCode, homeTeamName, awayTeamName, body);
                return new AiTipResult.ProviderError($"HTTP {(int)resp.StatusCode}");
            }

            var payload = await resp.Content.ReadFromJsonAsync<OpenAiChatResponse>(JsonOpts, ct);
            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
                return new AiTipResult.InvalidResponse("OpenAI returned no content", null);

            OpenAiTipBody? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<OpenAiTipBody>(content, JsonOpts);
            }
            catch (JsonException ex)
            {
                return new AiTipResult.InvalidResponse($"content was not valid JSON: {ex.Message}", content);
            }

            if (parsed is null)
                return new AiTipResult.InvalidResponse("parsed body was null", content);

            var validation = AiTipResponseValidator.Validate(parsed.HomeGoals, parsed.AwayGoals, parsed.Reasoning);
            return validation switch
            {
                AiTipValidationResult.Valid v => new AiTipResult.Success(v.Response),
                AiTipValidationResult.Invalid i => new AiTipResult.InvalidResponse(i.Error, content),
                _ => new AiTipResult.InvalidResponse("unknown validation result", content),
            };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // HttpClient timeout (our own deadline expired) — distinguish from caller-cancel.
            return new AiTipResult.Timeout();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenAI call threw for {Home} vs {Away}", homeTeamName, awayTeamName);
            return new AiTipResult.ProviderError(ex.GetType().Name + ": " + ex.Message);
        }
    }

    private sealed class OpenAiChatResponse
    {
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiMessage
    {
        public string? Content { get; set; }
    }

    private sealed class OpenAiTipBody
    {
        [JsonPropertyName("home_goals")] public int? HomeGoals { get; set; }
        [JsonPropertyName("away_goals")] public int? AwayGoals { get; set; }
        [JsonPropertyName("reasoning")] public string? Reasoning { get; set; }
    }
}
