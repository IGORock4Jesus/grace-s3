using GraceS3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GraceS3.Endpoints.Clients;

public static class GetClientListEndpoint
{
	public sealed record GetClientListRequest(int Skip, int Take);

	public sealed record GetClientListResponse(Guid Id, string AccessKey);

	public static void Map(IEndpointRouteBuilder builder)
	{
		builder
			.MapGet("", Handle)
			.WithName("GetClientList")
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<List<GetClientListResponse>>(StatusCodes.Status200OK);
	}

	private static async Task<IResult> Handle(
		[AsParameters] GetClientListRequest request,
		Database database,
		CancellationToken cancellationToken
	)
	{
		try
		{
			return TypedResults.Ok(
				await database
					.Clients.OrderBy(x => x.AccessKey)
					.Select(x => new GetClientListResponse(x.Id, x.AccessKey))
					.ToListAsync(cancellationToken)
			);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to handle get client list endpoint");

			throw;
		}
	}
}
