using GraceS3.Data;
using GraceS3.Services;
using MongoDB.Driver;
using Serilog;

namespace GraceS3.Buckets.Endpoints;

public static class GetBucketListEndpoint
{
	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapGet("", Handle)
			.WithName("GetBucketList")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<BucketEntity[]>(StatusCodes.Status200OK);
	}

	private static async Task<IResult> Handle(
		UserService userService,
		Database database,
		CancellationToken cancellationToken
	)
	{
		try
		{
			Guid userId = userService.GetId();
			IEnumerable<BucketEntity> buckets = await database
				.Buckets.Find(x => x.UserId == userId)
				.ToListAsync(cancellationToken);
			return Results.Ok(buckets);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to get bucket list");
			throw;
		}
	}
}
