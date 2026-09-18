using GraceS3.Data;
using GraceS3.Services;
using GraceS3.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GraceS3.Endpoints.Clients.CreateClient;

public static class CreateClientEndpoint
{
	public sealed record CreateClientListRequest(string AccessKey, string SecretKey);

	public sealed record CreateClientListResponse(Guid Id, string AccessKey);

	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapPost("", Handle)
			.WithName("CreateClient")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<CreateClientListResponse>(StatusCodes.Status201Created)
			.Produces(StatusCodes.Status409Conflict);
	}

	private static async Task<IResult> Handle(
		CreateClientListRequest request,
		Database database,
		DiskService diskService,
		CancellationToken cancellationToken
	)
	{
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

			return TypedResults.Created($"objects/{context.Id}");
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to handle get client list endpoint");

			throw;
		}
	}
}
