namespace GraceS3.AdminProxy;

public sealed class AdminProxyConfiguration
{
	[ConfigurationKeyName("OAUTH_AUTHORITY")]
	public required string OAuthAuthority { get; init; }

	[ConfigurationKeyName("OAUTH_CLIENT_ID")]
	public required string OAuthClientId { get; init; }

	[ConfigurationKeyName("OAUTH_CLIENT_SECRET")]
	public required string OAuthClientSecret { get; init; }

	[ConfigurationKeyName("REDIS_CONNECTION_STRING")]
	public required string RedisConnectionString { get; init; }

	[ConfigurationKeyName("REDIS_KEY_PREFIX")]
	public string RedisKeyPrefix { get; init; } = "graces3-adminproxy:";
}
