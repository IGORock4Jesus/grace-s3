// using GraceS3.Data;
// using GraceS3.Services;
// using MongoDB.Driver;

// namespace GraceS3.Files.Endpoints;

// public static class GetFilesListEndpoint
// {
// 	public static void Map(IEndpointRouteBuilder builder)
// 	{
// 		builder
// 			.MapGet("", Handle)
// 			.WithName("GetFilesList")
// 			.ProducesProblem(StatusCodes.Status500InternalServerError)
// 			.Produces<List<ObjectEntity>>(StatusCodes.Status200OK);
// 	}

// 	private static async Task<IResult> Handle(
// 		Guid bucketId,
// 		Database database,
// 		UserService userService,
// 		CancellationToken cancellationToken
// 	)
// 	{
// 		try
// 		{
// 			Guid userId = userService.GetId();
// 			List<ObjectEntity> files = await database
// 				.Files.Find(x => x.UserId == userId && x.BucketId == bucketId)
// 				.ToListAsync(cancellationToken);

// 			return TypedResults.Ok(files);
// 		}
// 		catch (Exception ex)
// 		{
// 			Log.Error(ex, "Failed to get files list");
// 			throw;
// 		}
// 	}
// }
