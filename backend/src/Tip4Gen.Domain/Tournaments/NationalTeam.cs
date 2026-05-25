namespace Tip4Gen.Domain.Tournaments;

public class NationalTeam
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Code { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private NationalTeam() { }

    public NationalTeam(string externalId, string name, string? code = null)
    {
        Id = Guid.NewGuid();
        ExternalId = externalId;
        Name = name;
        Code = code;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Rename(string name) => Name = name;

    public void SetCode(string? code) => Code = code;
}
