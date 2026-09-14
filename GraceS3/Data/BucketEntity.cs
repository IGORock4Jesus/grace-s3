using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GraceS3.Data;

public sealed class BucketEntity : Entity
{
	[BsonElement("user_id")]
	public Guid UserId { get; set; }

	[BsonElement("name")]
	public string Name { get; set; } = string.Empty;
}
