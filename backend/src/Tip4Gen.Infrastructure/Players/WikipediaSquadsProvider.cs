using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tip4Gen.Domain.Players;

namespace Tip4Gen.Infrastructure.Players;

public interface IWikipediaSquadsProvider
{
    /// <summary>
    /// Fetches the 2026 FIFA World Cup squads page via MediaWiki's parse API and
    /// returns the flattened player list. Throws on transport/JSON failures so the
    /// caller (PlayersImportService) can translate to a tagged-union failure result.
    /// </summary>
    Task<IReadOnlyList<ParsedPlayer>> FetchAsync(CancellationToken ct);
}

public class WikipediaSquadsProvider : IWikipediaSquadsProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly WikipediaSquadsProviderOptions _options;
    private readonly ILogger<WikipediaSquadsProvider> _logger;

    public WikipediaSquadsProvider(
        HttpClient http,
        IOptions<WikipediaSquadsProviderOptions> options,
        ILogger<WikipediaSquadsProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ParsedPlayer>> FetchAsync(CancellationToken ct)
    {
        var path = $"/w/api.php?action=parse&page={Uri.EscapeDataString(_options.PageTitle)}"
                   + "&prop=text&format=json&formatversion=2";

        using var resp = await _http.GetAsync(path, ct);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<WikiParseResponse>(JsonOpts, ct);
        var html = payload?.Parse?.Text;
        if (string.IsNullOrWhiteSpace(html))
        {
            _logger.LogWarning("Wikipedia parse API returned empty text for page {Page}", _options.PageTitle);
            return Array.Empty<ParsedPlayer>();
        }

        var players = WikipediaSquadsParser.Parse(html);
        _logger.LogInformation("Parsed {Count} player rows from Wikipedia page {Page}", players.Count, _options.PageTitle);
        return players;
    }

    private sealed record WikiParseResponse([property: JsonPropertyName("parse")] WikiParse? Parse);
    private sealed record WikiParse(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("pageid")] long PageId,
        [property: JsonPropertyName("text")] string? Text);
}
