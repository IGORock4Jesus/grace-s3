namespace GraceS3.Configs;

public sealed class SwaggerConfig
{
	public required string OAuthUri { get; init; }
	public required string OAuthRealm { get; init; }
	public required string OAuthClientID { get; init; }
}
