using System.Data;
using GraceS3.Data;
using GraceS3.Services;
using MongoDB.Driver;
using Serilog;

namespace GraceS3.Buckets.Endpoints;

public static class CreateBucketEndpoint
{
	public sealed record CreateBucketRequest(string Name);

	public sealed record CreateBucketConflictResponse(string Error);

	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapPost("", Handle)
			.WithName("CreateBucket")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<BucketEntity>(StatusCodes.Status201Created)
			.Produces<CreateBucketConflictResponse>(StatusCodes.Status409Conflict);
	}

	private static async Task<IResult> Handle(
		CreateBucketRequest request,
		Database database,
		UserService userService,
		CancellationToken cancellationToken
	)
	{
		try
		{
			Guid userId = userService.GetId();

			BucketEntity? existing = await database
				.Buckets.Find(x => x.Name.ToLower() == request.Name.ToLower() && x.UserId == userId)
				.FirstOrDefaultAsync(cancellationToken);
			if (existing is not null)
			{
				return Results.Conflict(
					new CreateBucketConflictResponse("A bucket with the same name is already exist")
				);
			}

			BucketEntity entity = new() { Name = request.Name, UserId = userId };
			await database.Buckets.InsertOneAsync(entity, null, cancellationToken);

			return Results.Created($"/buckets/{entity.Id}", entity);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to create bucket");

			throw;
		}
	}
}
