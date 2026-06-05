namespace Tip4Gen.Domain.Players;

public class Player
{
    public const int MaxNameLength = 120;

    public Guid Id { get; private set; }
    public Guid NationalTeamId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Player() { }

    public Player(Guid nationalTeamId, string name)
    {
        Id = Guid.NewGuid();
        NationalTeamId = nationalTeamId;
        Name = name;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Rename(string name) => Name = name;
}
