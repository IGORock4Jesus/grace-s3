// using GraceS3.Data;
// using GraceS3.Services;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Options;
// using MongoDB.Driver;
// using Serilog;

// namespace GraceS3.Files.Endpoints;

// public static class DeleteFileEndpoint
// {
// 	public record DeleteFileNotFoundResponse(string Message);

// 	public static void Map(IEndpointRouteBuilder builder)
// 	{
// 		builder
// 			.MapDelete("{fileId}", Handle)
// 			.WithName("DeleteFile")
// 			.ProducesProblem(StatusCodes.Status500InternalServerError)
// 			.Produces<DeleteFileNotFoundResponse>(StatusCodes.Status404NotFound)
// 			.Produces(StatusCodes.Status204NoContent);
// 	}

// 	private static async Task<IResult> Handle(
// 		string bucketId,
// 		string fileId,
// 		Database database,
// 		UserService userService,
// 		IOptions<ApplicationConfiguration> options,
// 		CancellationToken cancellationToken
// 	)
// 	{
// 		try
// 		{
// 			Guid userId = userService.GetId();

// 			BucketEntity? bucket = await database
// 				.Buckets.Find(x => x.Id == bucketId && x.UserId == userId)
// 				.FirstOrDefaultAsync(cancellationToken);
// 			if (bucket == null)
// 			{
// 				return TypedResults.NotFound(new DeleteFileNotFoundResponse("Bucket not found"));
// 			}

// 			ObjectEntity? entity = await database.Files.FindOneAndDeleteAsync(
// 				x => x.Id == fileId && x.BucketId == bucketId,
// 				null,
// 				cancellationToken
// 			);

// 			if (entity == null)
// 			{
// 				return TypedResults.NotFound(new DeleteFileNotFoundResponse("File not found"));
// 			}

// 			return TypedResults.NoContent();
// 		}
// 		catch (Exception ex)
// 		{
// 			Log.Error(ex, "Failed to delete file");

// 			throw;
// 		}
// 	}
// }
