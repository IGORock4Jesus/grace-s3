using System.Runtime.CompilerServices;
using GraceS3.Configs;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace GraceS3.Data;

public sealed class Database : IDisposable
{
	private readonly MongoClient mongoClient;
	private readonly IMongoDatabase mongoDatabase;

	public readonly IMongoCollection<BucketEntity> Buckets;

	public Database(IOptions<DatabaseConfig> options)
	{
		BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

		this.mongoClient = new MongoClient(options.Value.ConnectionString);
		this.mongoDatabase = this.mongoClient.GetDatabase(options.Value.DatabaseName);

		this.Buckets = this.mongoDatabase.GetCollection<BucketEntity>("buckets");
	}

	public void Dispose()
	{
		mongoClient.Dispose();
	}
}
