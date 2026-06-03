using System.Text.Json.Serialization;

namespace Tip4Gen.Infrastructure.Football;

internal sealed record WorldCupGamesEnvelope(
    [property: JsonPropertyName("games")] List<WorldCupGame>? Games);

internal sealed record WorldCupGame(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("home_team_id")] string HomeTeamId,
    [property: JsonPropertyName("away_team_id")] string AwayTeamId,
    [property: JsonPropertyName("home_score")] string? HomeScore,
    [property: JsonPropertyName("away_score")] string? AwayScore,
    [property: JsonPropertyName("group")] string? Group,
    [property: JsonPropertyName("matchday")] string? Matchday,
    [property: JsonPropertyName("local_date")] string LocalDate,
    [property: JsonPropertyName("stadium_id")] string? StadiumId,
    [property: JsonPropertyName("finished")] string? Finished,
    [property: JsonPropertyName("time_elapsed")] string? TimeElapsed,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("home_team_name_en")] string? HomeTeamNameEn,
    [property: JsonPropertyName("away_team_name_en")] string? AwayTeamNameEn,
    [property: JsonPropertyName("home_team_label")] string? HomeTeamLabel,
    [property: JsonPropertyName("away_team_label")] string? AwayTeamLabel);

internal sealed record WorldCupTeamsEnvelope(
    [property: JsonPropertyName("teams")] List<WorldCupTeam>? Teams);

internal sealed record WorldCupTeam(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name_en")] string NameEn,
    [property: JsonPropertyName("fifa_code")] string? FifaCode);

internal sealed record AuthLoginRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password);

internal sealed record AuthRegisterRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password);

internal sealed record AuthTokenResponse(
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("accessToken")] string? AccessToken);
