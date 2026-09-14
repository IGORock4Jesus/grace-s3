namespace GraceS3.Configs;

public sealed class AuthConfig
{
    public required string Uri { get; init; }
    public required string Realm { get; init; }
    public required string ClientID { get; init; }
    public required string ClientSecret { get; init; }
}
