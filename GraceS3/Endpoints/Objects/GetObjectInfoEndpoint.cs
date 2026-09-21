using System.Text.Json.Serialization;
using GraceS3.Data;
using GraceS3.Services;
using GraceS3.Validators;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GraceS3.Endpoints.Objects;

public static partial class GetObjectInfoEndpoint
{
	public record GetObjectNotFoundResponse(string Message);

	public record GetObjectResponse(
		Guid ClientId,
		Guid ObjectId,
		string FileName,
		string ContentType,
		string ContentDisposition,
		long Size
	);

	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapGet("{objectId:guid}/info", Handle)
			.WithName("GetObjectInfo")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<GetObjectResponse>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status404NotFound);
	}

	private static async Task<IResult> Handle(
		Guid objectId,
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

			ObjectEntity? @object = await database.Objects.FirstOrDefaultAsync(
				x => x.ClientId == client.Id && x.Id == objectId,
				cancellationToken
			);
			if (@object is null)
			{
				return TypedResults.NotFound();
			}

			return TypedResults.Ok(
				new GetObjectResponse(
					@object.ClientId,
					@object.Id,
					@object.FileName,
					@object.ContentType,
					@object.ContentDisposition,
					@object.Size
				)
			);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to get file info");

			throw;
		}
	}
}
