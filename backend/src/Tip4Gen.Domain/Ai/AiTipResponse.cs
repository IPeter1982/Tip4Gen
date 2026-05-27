namespace Tip4Gen.Domain.Ai;

/// <summary>
/// The shape an AI tipper must produce: a final score plus short Hungarian reasoning
/// shown to users after the tip deadline.
/// </summary>
public sealed record AiTipResponse(int HomeGoals, int AwayGoals, string Reasoning);
