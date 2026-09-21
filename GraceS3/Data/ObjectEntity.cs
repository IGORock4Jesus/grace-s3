namespace GraceS3.Data;

public sealed class ObjectEntity : Entity
{
	public ClientEntity Client { get; set; } = null!;
	public required Guid ClientId { get; init; }

	public required string FileName { get; init; }
	public required string ContentType { get; init; }
	public required string ContentDisposition { get; init; }
	public required long Size { get; init; }
}
