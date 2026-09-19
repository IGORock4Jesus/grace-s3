using Microsoft.EntityFrameworkCore;

namespace GraceS3.Data;

public sealed class ClientEntity : Entity
{
	public required string AccessKey { get; init; }

	public required string SecretKey { get; init; }

	public List<ObjectEntity> Objects { get; set; } = [];
}
