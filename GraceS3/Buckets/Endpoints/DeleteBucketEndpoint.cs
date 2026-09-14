using GraceS3.Data;
using GraceS3.Services;
using MongoDB.Driver;
using Serilog;

namespace GraceS3.Buckets.Endpoints;

public static class DeleteBucketEndpoint
{
	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapDelete("{id}", Handle)
			.WithName("DeleteBucket")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status404NotFound);
	}

	private static async Task<IResult> Handle(
		string id,
		UserService userService,
		Database database,
		CancellationToken cancellationToken
	)
	{
		try
		{
			Guid userId = userService.GetId();
			DeleteResult result = await database.Buckets.DeleteOneAsync(
				x => x.Id == id && x.UserId == userId,
				cancellationToken
			);

			if (result.DeletedCount == 0)
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
