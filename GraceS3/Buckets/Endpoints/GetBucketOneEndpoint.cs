using GraceS3.Data;
using GraceS3.Services;
using MongoDB.Driver;
using Serilog;

namespace GraceS3.Buckets.Endpoints;

public static class GetBucketOneEndpoint
{
	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapGet("{id}", Handle)
			.WithName("GetBucketOne")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<BucketEntity>(StatusCodes.Status200OK)
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
			BucketEntity? bucket = await database
				.Buckets.Find(x => x.Id == id && x.UserId == userId)
				.FirstOrDefaultAsync(cancellationToken);
			if (bucket is null)
			{
				return Results.NotFound(new { error = "Bucket not found" });
			}

			return Results.Ok(bucket);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to get bucket list");
			throw;
		}
	}
}
