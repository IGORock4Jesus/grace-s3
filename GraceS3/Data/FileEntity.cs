using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GraceS3.Data;

public sealed class FileEntity : Entity
{
	[BsonElement("bucket_id")]
	public Guid UserId { get; set; }

	[BsonElement("name")]
	public string Name { get; set; } = string.Empty;

	[BsonElement("path")]
	public string Path { get; set; } = string.Empty;
}
