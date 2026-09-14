using GraceS3.Data;
using GraceS3.Services;
using MongoDB.Driver;
using Serilog;

namespace GraceS3.Buckets.Endpoints;

public static class UpdateBucketEndpoint
{
	public record UpdateBucketRequest(string Name);

	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapPut("{id}", Handle)
			.WithName("UpdateBucket")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status404NotFound);
	}

	private static async Task<IResult> Handle(
		string id,
		UpdateBucketRequest request,
		UserService userService,
		Database database,
		CancellationToken cancellationToken
	)
	{
		try
		{
			Guid userId = userService.GetId();
			FilterDefinition<BucketEntity> filter =
				Builders<BucketEntity>.Filter.Eq(b => b.Id, id)
				& Builders<BucketEntity>.Filter.Eq(b => b.UserId, userId);
			UpdateResult result = await database.Buckets.UpdateOneAsync(
				filter,
				Builders<BucketEntity>.Update.Set(b => b.Name, request.Name),
				null,
				cancellationToken
			);

			if (result.ModifiedCount == 0)
			{
				return Results.NotFound(new { error = "Bucket not found" });
			}

			return Results.NoContent();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to get bucket list");
			throw;
		}
	}
}
