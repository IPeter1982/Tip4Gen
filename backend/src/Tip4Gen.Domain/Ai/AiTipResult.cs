namespace Tip4Gen.Domain.Ai;

/// <summary>
/// Outcome of one attempt to generate an AI tip. The orchestrator decides what to do
/// with each variant: Success → persist as a tip; anything else → log + schedule retry
/// or fall through to the 1–1 default at T-1h.
/// </summary>
public abstract record AiTipResult
{
    public sealed record Success(AiTipResponse Response) : AiTipResult;

    /// <summary>The provider answered, but the response didn't pass validation.</summary>
    public sealed record InvalidResponse(string Error, string? RawText) : AiTipResult;

    /// <summary>The provider didn't respond in time (HTTP timeout or our own deadline).</summary>
    public sealed record Timeout : AiTipResult;

    /// <summary>HTTP error, deserialization failure, or unexpected provider exception.</summary>
    public sealed record ProviderError(string Error) : AiTipResult;

    /// <summary>The tipper is configured but no API key is set — fail safely so the
    /// fallback path still runs.</summary>
    public sealed record Disabled : AiTipResult;
}
