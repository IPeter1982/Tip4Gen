namespace Tip4Gen.Api.Auth;

public class Auth0Options
{
    public const string SectionName = "Auth0";

    public string? Domain { get; set; }
    public string? Audience { get; set; }
    public string? AdminSub { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(Audience);

    public string Authority => $"https://{Domain}/";
}
