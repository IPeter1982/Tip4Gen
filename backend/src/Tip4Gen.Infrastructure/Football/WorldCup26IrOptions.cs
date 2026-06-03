using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Infrastructure.Football;

public class WorldCup26IrOptions
{
    [Required]
    public string BaseUrl { get; set; } = "https://worldcup26.ir";

    [Required]
    public string LeagueId { get; set; } = "wc26";

    [Range(1900, 2100)]
    public int Season { get; set; } = 2026;

    public string? AuthEmail { get; set; }

    public string? AuthPassword { get; set; }

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;
}
