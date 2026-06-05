using Tip4Gen.Infrastructure.Players;

namespace Tip4Gen.Infrastructure.Tests.Players;

public class WikipediaSquadsParserTests
{
    private const string TwoSquadsHtml = """
        <div class="mw-content-text">
          <div class="mw-heading mw-heading3"><h3 id="Argentina">Argentina</h3>
            <span class="mw-editsection">[<a href="/edit">edit</a>]</span>
          </div>
          <table class="sortable wikitable plainrowheaders" style="font-size:100%; width: 98%">
            <thead><tr><th>No.</th><th>Pos.</th><th>Player</th><th>DoB</th><th>Caps</th><th>Goals</th><th>Club</th></tr></thead>
            <tbody>
              <tr class="nat-fs-player">
                <td>10</td>
                <td><a>FW</a></td>
                <th data-sort-value="Messi, Lionel" scope="row"><a href="/wiki/Lionel_Messi">Lionel Messi</a></th>
                <td>Jun 24, 1987</td><td>180</td><td>110</td><td>Inter Miami</td>
              </tr>
              <tr class="nat-fs-player">
                <td>21</td>
                <td><a>FW</a></td>
                <th data-sort-value="Alvarez, Julian" scope="row"><a href="/wiki/Julian_Alvarez">Julián Álvarez</a></th>
                <td>Jan 31, 2000</td><td>40</td><td>10</td><td>Atlético Madrid</td>
              </tr>
            </tbody>
          </table>
          <div class="mw-heading mw-heading3"><h3 id="France">France</h3></div>
          <table class="sortable wikitable plainrowheaders">
            <thead><tr><th>No.</th><th>Pos.</th><th>Player</th><th>DoB</th><th>Caps</th><th>Goals</th><th>Club</th></tr></thead>
            <tbody>
              <tr class="nat-fs-player">
                <td>10</td>
                <td><a>FW</a></td>
                <th data-sort-value="Mbappe, Kylian" scope="row"><a href="/wiki/Kylian_Mbappe">Kylian Mbappé</a></th>
                <td>Dec 20, 1998</td><td>85</td><td>50</td><td>Real Madrid</td>
              </tr>
            </tbody>
          </table>
        </div>
        """;

    [Fact]
    public void Parse_TwoSquads_ReturnsAllPlayers()
    {
        var players = WikipediaSquadsParser.Parse(TwoSquadsHtml);

        Assert.Equal(3, players.Count);
        Assert.Contains(players, p => p.CountryName == "Argentina" && p.Name == "Lionel Messi");
        Assert.Contains(players, p => p.CountryName == "Argentina" && p.Name == "Julián Álvarez");
        Assert.Contains(players, p => p.CountryName == "France" && p.Name == "Kylian Mbappé");
    }

    [Fact]
    public void Parse_HandlesDiacritics()
    {
        var html = """
            <div class="mw-heading mw-heading3"><h3 id="Germany">Germany</h3></div>
            <table class="sortable wikitable">
              <tbody>
                <tr class="nat-fs-player">
                  <td>13</td><td><a>FW</a></td>
                  <th scope="row"><a>Thomas Müller</a></th>
                  <td>...</td><td>1</td><td>0</td><td>Bayern</td>
                </tr>
              </tbody>
            </table>
            """;

        var players = WikipediaSquadsParser.Parse(html);

        Assert.Single(players);
        Assert.Equal("Thomas Müller", players[0].Name);
    }

    [Fact]
    public void Parse_StripsCaptainMarker()
    {
        var html = """
            <div class="mw-heading mw-heading3"><h3 id="USA">USA</h3></div>
            <table class="wikitable">
              <tbody>
                <tr class="nat-fs-player">
                  <td>10</td><td><a>MF</a></td>
                  <th scope="row"><a>Christian Pulisic</a> (c)</th>
                  <td>...</td><td>1</td><td>0</td><td>Milan</td>
                </tr>
                <tr class="nat-fs-player">
                  <td>4</td><td><a>DF</a></td>
                  <th scope="row"><a>Tyler Adams</a> (vc)</th>
                  <td>...</td><td>1</td><td>0</td><td>Bournemouth</td>
                </tr>
              </tbody>
            </table>
            """;

        var players = WikipediaSquadsParser.Parse(html);

        Assert.Equal(2, players.Count);
        Assert.Equal("Christian Pulisic", players[0].Name);
        Assert.Equal("Tyler Adams", players[1].Name);
    }

    [Fact]
    public void Parse_SkipsMalformedRows()
    {
        // No <th scope="row"> in the second row → silently dropped.
        var html = """
            <div class="mw-heading mw-heading3"><h3 id="Spain">Spain</h3></div>
            <table class="wikitable">
              <tbody>
                <tr class="nat-fs-player">
                  <td>1</td><td><a>GK</a></td>
                  <th scope="row"><a>Unai Simón</a></th>
                  <td>...</td><td>1</td><td>0</td><td>Athletic</td>
                </tr>
                <tr class="nat-fs-player">
                  <td>X</td><td>broken row, no name cell</td>
                </tr>
              </tbody>
            </table>
            """;

        var players = WikipediaSquadsParser.Parse(html);

        Assert.Single(players);
        Assert.Equal("Unai Simón", players[0].Name);
    }

    [Fact]
    public void Parse_DeduplicatesWithinSquad()
    {
        var html = """
            <div class="mw-heading mw-heading3"><h3 id="Brazil">Brazil</h3></div>
            <table class="wikitable">
              <tbody>
                <tr class="nat-fs-player">
                  <td>10</td><td><a>FW</a></td>
                  <th scope="row"><a>Vinícius Júnior</a></th>
                  <td>...</td><td>1</td><td>0</td><td>Real Madrid</td>
                </tr>
                <tr class="nat-fs-player">
                  <td>10</td><td><a>FW</a></td>
                  <th scope="row"><a>Vinícius Júnior</a></th>
                  <td>...</td><td>1</td><td>0</td><td>Real Madrid</td>
                </tr>
              </tbody>
            </table>
            """;

        var players = WikipediaSquadsParser.Parse(html);

        Assert.Single(players);
    }

    [Fact]
    public void Parse_NbspBetweenNameParts_NormalizesToSpace()
    {
        // &nbsp; is decoded by DeEntitize then collapsed to a normal space.
        var html = """
            <div class="mw-heading mw-heading3"><h3 id="Italy">Italy</h3></div>
            <table class="wikitable">
              <tbody>
                <tr class="nat-fs-player">
                  <td>10</td><td><a>FW</a></td>
                  <th scope="row"><a>Federico&nbsp;Chiesa</a></th>
                  <td>...</td><td>1</td><td>0</td><td>Juve</td>
                </tr>
              </tbody>
            </table>
            """;

        var players = WikipediaSquadsParser.Parse(html);

        Assert.Single(players);
        Assert.Equal("Federico Chiesa", players[0].Name);
    }

    [Fact]
    public void Parse_EmptyHtml_ReturnsEmpty()
    {
        Assert.Empty(WikipediaSquadsParser.Parse(""));
        Assert.Empty(WikipediaSquadsParser.Parse("   "));
    }

    [Fact]
    public void Parse_HeaderWithoutFollowingSquadTable_IsIgnored()
    {
        // h3 stands alone (e.g. a stub section); parser does not invent players.
        var html = """
            <div class="mw-heading mw-heading3"><h3 id="Iceland">Iceland</h3></div>
            <p>Squad to be announced.</p>
            """;

        Assert.Empty(WikipediaSquadsParser.Parse(html));
    }

    [Fact]
    public void Parse_UsesIdAttribute_OverInnerText()
    {
        // The id-derived name is "Czech Republic" even though the visible header could
        // be wrapped in extra markup; ensures the strip-editsection branch isn't needed
        // when the id is present.
        var html = """
            <div class="mw-heading mw-heading3">
              <h3 id="Czech_Republic">Czech Republic <span class="mw-editsection">[edit]</span></h3>
            </div>
            <table class="wikitable">
              <tbody>
                <tr class="nat-fs-player">
                  <td>1</td><td><a>GK</a></td>
                  <th scope="row"><a>Matěj Kovář</a></th>
                  <td>...</td><td>1</td><td>0</td><td>PSV</td>
                </tr>
              </tbody>
            </table>
            """;

        var players = WikipediaSquadsParser.Parse(html);

        Assert.Single(players);
        Assert.Equal("Czech Republic", players[0].CountryName);
    }
}
