// using GraceS3.Data;
// using GraceS3.Services;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Options;
// using MongoDB.Bson;
// using MongoDB.Driver;
// using Serilog;

// namespace GraceS3.Files.Endpoints;

// public static class UploadFileEndpoint
// {
// 	public record UploadFileNotFoundResponse(string Message);

// 	public static void Map(IEndpointRouteBuilder builder)
// 	{
// 		builder
// 			.MapPut("", Handle)
// 			.WithName("UploadFile")
// 			.ProducesProblem(StatusCodes.Status500InternalServerError)
// 			.Produces<ObjectEntity>(StatusCodes.Status201Created);
// 	}

// 	private static async Task<IResult> Handle(
// 		string bucketId,
// 		IFormFile file,
// 		Database database,
// 		UserService userService,
// 		DiskService fileService,
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
// 				return TypedResults.NotFound(new UploadFileNotFoundResponse("Bucket not found"));
// 			}

// 			Guid fileId = Guid.CreateVersion7();

// 			ObjectEntity entity = new()
// 			{
// 				Id = ObjectId.GenerateNewId().ToString(),
// 				Name = file.FileName,
// 				ContentType = file.ContentType,
// 				Size = file.Length,
// 				BucketId = bucketId,
// 				Path = path,
// 				FileId = fileId,
// 			};

// 			string path = fileService.GetFilePath(bucket, entity);

// 			Path.Combine(
// 				options.Value.WorkingDirectory,
// 				userService.GetId().ToString(),
// 				bucket.Id.ToString(),
// 				fileId.ToString()
// 			);

// 			await database.Files.InsertOneAsync(entity, null, cancellationToken);

// 			return TypedResults.Created($"/buckets/{bucketId}/files/{entity.Id}", entity);
// 		}
// 		catch (Exception ex)
// 		{
// 			Log.Error(ex, "Failed to upload file");
// 			throw;
// 		}
// 	}
// }
