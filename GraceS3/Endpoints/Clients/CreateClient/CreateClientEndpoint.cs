using GraceS3.Data;
using GraceS3.Services;
using GraceS3.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GraceS3.Endpoints.Clients.CreateClient;

public static partial class CreateClientEndpoint
{
	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapPost("", Handle)
			.WithName("CreateClient")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<CreateClientResponse>(StatusCodes.Status201Created)
			.Produces(StatusCodes.Status409Conflict);
	}

	private static async Task<IResult> Handle(
		CreateClientRequest request,
		Database database,
		DiskService diskService,
		CancellationToken cancellationToken
	)
	{
		if (string.IsNullOrWhiteSpace(request.AccessKey) || request.AccessKey.Length > 100
			|| string.IsNullOrWhiteSpace(request.SecretKey))
		{
			return TypedResults.BadRequest("Access key (1–100 characters) and secret key are required.");
		}

		try
		{
			ClientEntity? client = await database.Clients.FirstOrDefaultAsync(
				x => x.AccessKey == request.AccessKey,
				cancellationToken
			);
			if (client is not null)
			{
				return TypedResults.Conflict();
			}

			Rollback<RollbackContext> rollback = new();
			rollback.Add(new CreateClientAction()).Add(new CreateClientDirectoryAction());

			RollbackContext context = new(
				Guid.CreateVersion7(),
				request.AccessKey,
				request.SecretKey,
				database,
				diskService
			);
			await rollback.ExecuteAllAsync(context, cancellationToken);

			return TypedResults.Created($"clients/{context.Id}", new CreateClientResponse(context.Id, context.AccessKey));
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to create client");

			throw;
		}
	}
}
