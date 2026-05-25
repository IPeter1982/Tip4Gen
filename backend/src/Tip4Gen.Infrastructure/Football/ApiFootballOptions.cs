using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Infrastructure.Football;

public class ApiFootballOptions
{
    public string Provider { get; set; } = "api-football";

    [Required]
    public string ApiKey { get; set; } = default!;

    [Required]
    public string BaseUrl { get; set; } = "https://v3.football.api-sports.io";

    [Required]
    public string LeagueId { get; set; } = "1";

    [Range(1900, 2100)]
    public int Season { get; set; } = 2022;
}
