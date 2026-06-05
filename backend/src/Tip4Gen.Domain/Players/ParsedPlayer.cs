namespace Tip4Gen.Domain.Players;

/// <summary>
/// Pure-data record emitted by the Wikipedia squads parser. Lives in Domain so the
/// parser (in Infrastructure) and the importer service share a single value type
/// without an Infrastructure→Domain dependency leak.
/// </summary>
public sealed record ParsedPlayer(string CountryCode, string CountryName, string Name);
