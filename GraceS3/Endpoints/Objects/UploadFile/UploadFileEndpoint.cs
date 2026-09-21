using GraceS3.Data;
using GraceS3.Services;
using GraceS3.Utils;
using GraceS3.Validators;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GraceS3.Endpoints.Objects.UploadFile;

public static partial class UploadObjectEndpoint
{
	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapPost("", Handle)
			.WithName("UploadObject")
			.DisableAntiforgery()
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<UploadObjectResponse>(StatusCodes.Status201Created)
			.Produces(StatusCodes.Status404NotFound);
	}

	private static async Task<IResult> Handle(
		IFormFile formFile,
		[AsParameters] GraceAuthRequest authRequest,
		HttpRequest httpRequest,
		Database database,
		DiskService diskService,
		CancellationToken cancellationToken
	)
	{
		try
		{
			string? accessKey = Uri.UnescapeDataString(authRequest.Credential)
				.Split('/')
				.FirstOrDefault();
			if (string.IsNullOrWhiteSpace(accessKey))
			{
				return TypedResults.Unauthorized();
			}

			ClientEntity? client = await database.Clients.FirstOrDefaultAsync(
				x => x.AccessKey == accessKey,
				cancellationToken
			);
			if (client is null)
			{
				return TypedResults.Unauthorized();
			}

			bool valid = S3PresignedUrlValidator.Validate(
				httpRequest,
				client.AccessKey,
				client.SecretKey,
				region: "spb", // TODO: move to env
				service: "s3" // TODO: move to env
			);
			if (!valid)
			{
				return TypedResults.Unauthorized();
			}

			Rollback<RollbackContext> rollback = new();
			rollback.Add(new CreateObjectInDatabase());
			rollback.Add(new PutFileOnDisk());

			RollbackContext rollbackContext = new(
				Guid.CreateVersion7(),
				client.Id,
				formFile.FileName,
				formFile.ContentType,
				formFile.ContentDisposition,
				formFile.Length,
				database,
				diskService,
				formFile.OpenReadStream()
			);
			await rollback.ExecuteAllAsync(rollbackContext, cancellationToken);

			return TypedResults.Created(
				$"objects/{rollbackContext.ObjectId}",
				new UploadObjectResponse(rollbackContext.ObjectId)
			);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to upload file");

			throw;
		}
	}
}
