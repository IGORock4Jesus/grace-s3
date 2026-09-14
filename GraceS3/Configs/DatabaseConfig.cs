namespace GraceS3.Configs;

public sealed class DatabaseConfig
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
}
