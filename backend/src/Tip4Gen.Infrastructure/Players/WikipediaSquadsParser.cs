using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tip4Gen.Domain.Players;

namespace Tip4Gen.Infrastructure.Players;

/// <summary>
/// Pure HTML→ParsedPlayer transformer. Takes the rendered HTML emitted by
/// MediaWiki's parse API (<c>action=parse&amp;prop=text</c>) and pulls out every
/// squad table that follows a country &lt;h3&gt; header.
///
/// Stateful only via HtmlAgilityPack — no I/O, no DB, no DI. Golden-tested in
/// <c>Tip4Gen.Domain.Tests.Players.WikipediaSquadsParserTests</c>.
/// </summary>
public static class WikipediaSquadsParser
{
    private static readonly Regex CaptainMarker =
        new(@"\s*\((?:c|vc)\)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FootnoteMarker =
        new(@"\[[^\]]+\]", RegexOptions.Compiled);

    private static readonly Regex CollapseWhitespace =
        new(@"\s+", RegexOptions.Compiled);

    public static IReadOnlyList<ParsedPlayer> Parse(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return Array.Empty<ParsedPlayer>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<ParsedPlayer>();
        var seen = new HashSet<(string Country, string Name)>();

        var h3Nodes = doc.DocumentNode.SelectNodes("//h3");
        if (h3Nodes is null) return results;

        foreach (var h3 in h3Nodes)
        {
            var countryName = ExtractCountryName(h3);
            if (string.IsNullOrEmpty(countryName)) continue;

            var squadTable = FindNextSquadTable(h3);
            if (squadTable is null) continue;

            var playerRows = squadTable.SelectNodes(".//tr[contains(@class, 'nat-fs-player')]");
            if (playerRows is null) continue;

            foreach (var row in playerRows)
            {
                var playerName = ExtractPlayerName(row);
                if (string.IsNullOrEmpty(playerName)) continue;

                var key = (countryName, playerName);
                if (seen.Add(key))
                    results.Add(new ParsedPlayer(string.Empty, countryName, playerName));
            }
        }

        return results;
    }

    private static string ExtractCountryName(HtmlNode h3)
    {
        // The id is always the most reliable source: MediaWiki encodes the section
        // header as "Czech_Republic" / "South_Africa" / "Mexico". The visible header
        // text matches but can include trailing [edit] junk from mw-editsection.
        var id = h3.GetAttributeValue("id", null);
        if (!string.IsNullOrEmpty(id))
            return HtmlEntity.DeEntitize(id.Replace('_', ' ')).Trim();

        // Fallback: strip mw-editsection then take inner text.
        var clone = h3.Clone();
        var edits = clone.SelectNodes(".//span[contains(@class, 'mw-editsection')]");
        if (edits is not null)
        {
            foreach (var e in edits) e.Remove();
        }
        return HtmlEntity.DeEntitize(clone.InnerText).Trim();
    }

    private static HtmlNode? FindNextSquadTable(HtmlNode h3)
    {
        // Wikipedia 1.39+ wraps headings in <div class="mw-heading">; walk forward from
        // that wrapper if present, else from the h3 itself. Stop at the next h2/h3 to
        // avoid pulling in another country's table (defensive — squads are siblings).
        var startNode = h3.ParentNode is { } parent
            && parent.GetAttributeValue("class", string.Empty).Contains("mw-heading")
            ? parent
            : h3;

        for (var current = startNode.NextSibling; current is not null; current = current.NextSibling)
        {
            if (current.NodeType != HtmlNodeType.Element) continue;

            var name = current.Name.ToLowerInvariant();
            if (name is "h2" or "h3") return null;

            // Wikipedia's heading wrapper for a sibling header also acts as a boundary.
            if (name == "div"
                && current.GetAttributeValue("class", string.Empty).Contains("mw-heading"))
            {
                return null;
            }

            if (name == "table" && IsSquadTable(current))
                return current;

            // Tables sometimes live one level inside a wrapper div (rare).
            if (name == "div")
            {
                var inner = current.SelectSingleNode(".//table[contains(@class, 'wikitable')]");
                if (inner is not null && IsSquadTable(inner))
                    return inner;
            }
        }

        return null;
    }

    private static bool IsSquadTable(HtmlNode table)
    {
        var cls = table.GetAttributeValue("class", string.Empty);
        // The actual page uses "sortable wikitable plainrowheaders". We only require
        // "wikitable" + presence of nat-fs-player rows — keeps the parser tolerant to
        // future class additions.
        if (!cls.Contains("wikitable", StringComparison.OrdinalIgnoreCase))
            return false;
        return table.SelectSingleNode(".//tr[contains(@class, 'nat-fs-player')]") is not null;
    }

    private static string ExtractPlayerName(HtmlNode row)
    {
        var nameCell = row.SelectSingleNode(".//th[@scope='row']");
        if (nameCell is null) return string.Empty;

        // Prefer the inner anchor: avoids hidden <span style="display:none"> sort-keys
        // that occasionally surface in inner-text.
        var link = nameCell.SelectSingleNode(".//a");
        var raw = link?.InnerText ?? nameCell.InnerText;
        return CleanupName(raw);
    }

    private static string CleanupName(string raw)
    {
        var decoded = HtmlEntity.DeEntitize(raw);
        decoded = decoded.Replace(' ', ' ');
        decoded = FootnoteMarker.Replace(decoded, string.Empty);
        decoded = CaptainMarker.Replace(decoded, string.Empty);
        decoded = CollapseWhitespace.Replace(decoded, " ");
        return decoded.Trim();
    }
}
