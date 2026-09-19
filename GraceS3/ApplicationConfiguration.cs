namespace GraceS3;

public sealed class ApplicationConfiguration
{
	[ConfigurationKeyName("GRACES3_OAUTH_URI")]
	public required string OAuthUri { get; init; }

	[ConfigurationKeyName("GRACES3_OAUTH_REALM")]
	public required string OAuthRealm { get; init; }

	[ConfigurationKeyName("GRACES3_OAUTH_CLIENT_ID")]
	public required string OAuthClientID { get; init; }

	[ConfigurationKeyName("GRACES3_OAUTH_CLIENT_SECRET")]
	public required string OAuthClientSecret { get; init; }

	[ConfigurationKeyName("GRACES3_DATABASE_CONNECTION_STRING")]
	public required string DatabaseConnectionString { get; init; }

	[ConfigurationKeyName("GRACES3_SWAGGER_OAUTH_CLIENT_ID")]
	public required string SwaggerOAuthClientID { get; init; }

	[ConfigurationKeyName("GRACES3_WORKING_DIRECTORY")]
	public required string WorkingDirectory { get; init; }

	[ConfigurationKeyName("GRACES3_WORKER_DELAY_SECONDS")]
	public required string WorkerDelaySeconds { get; init; }
}
