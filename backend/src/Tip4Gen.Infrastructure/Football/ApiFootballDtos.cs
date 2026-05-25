using System.Text.Json.Serialization;

namespace Tip4Gen.Infrastructure.Football;

internal sealed record ApiFootballFixturesResponse(
    [property: JsonPropertyName("response")] List<ApiFootballFixtureItem>? Response);

internal sealed record ApiFootballFixtureItem(
    [property: JsonPropertyName("fixture")] ApiFixtureBlock Fixture,
    [property: JsonPropertyName("league")] ApiLeagueBlock League,
    [property: JsonPropertyName("teams")] ApiTeamsBlock Teams,
    [property: JsonPropertyName("score")] ApiScoreBlock? Score);

internal sealed record ApiFixtureBlock(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("date")] DateTimeOffset Date,
    [property: JsonPropertyName("status")] ApiStatusBlock Status);

internal sealed record ApiStatusBlock(
    [property: JsonPropertyName("short")] string Short);

internal sealed record ApiLeagueBlock(
    [property: JsonPropertyName("round")] string Round);

internal sealed record ApiTeamsBlock(
    [property: JsonPropertyName("home")] ApiTeamRef Home,
    [property: JsonPropertyName("away")] ApiTeamRef Away);

internal sealed record ApiTeamRef(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name);

internal sealed record ApiScoreBlock(
    [property: JsonPropertyName("fulltime")] ApiScoreSide? Fulltime);

internal sealed record ApiScoreSide(
    [property: JsonPropertyName("home")] int? Home,
    [property: JsonPropertyName("away")] int? Away);

internal sealed record ApiFootballTeamsResponse(
    [property: JsonPropertyName("response")] List<ApiFootballTeamItem>? Response);

internal sealed record ApiFootballTeamItem(
    [property: JsonPropertyName("team")] ApiTeamDetail Team);

internal sealed record ApiTeamDetail(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string? Code);
