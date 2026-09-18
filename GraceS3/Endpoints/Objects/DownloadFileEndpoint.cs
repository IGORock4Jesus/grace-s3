using GraceS3.Data;
using GraceS3.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GraceS3.Endpoints.Objects;

public static class DownloadObjectEndpoint
{
	public record DownloadObjectRequest(string Message);

	public record DownloadObjectNotFoundResponse(string Message);

	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapPost("{objectId:guid}/download", Handle)
			.WithName("DownloadObject")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<ObjectEntity>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status404NotFound);
	}

	private static async Task<IResult> Handle(
		Guid objectId,
		Database database,
		UserService userService,
		CancellationToken cancellationToken
	)
	{
		try
		{
			Guid userId = userService.GetId();

			ObjectEntity? entity = await database.Objects.FirstOrDefaultAsync(
				x => x.Id == objectId && x.ClientId == userId,
				cancellationToken
			);
			if (entity == null)
			{
				return TypedResults.NotFound(new DownloadObjectNotFoundResponse("File not found"));
			}

			return TypedResults.Ok(entity);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to get file");

			throw;
		}
	}
}
