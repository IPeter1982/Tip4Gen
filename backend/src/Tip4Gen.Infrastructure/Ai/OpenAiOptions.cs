using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Infrastructure.Ai;

/// <summary>
/// Bound from configuration section "OpenAi" — empty ApiKey is intentional: the tipper
/// short-circuits to AiTipResult.Disabled so the scaffold runs without a key. Drop the
/// key into user-secrets (OpenAi:ApiKey = sk-…) when ready.
/// </summary>
public class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = "gpt-4o-mini";

    [Required]
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;

    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.7;
}
