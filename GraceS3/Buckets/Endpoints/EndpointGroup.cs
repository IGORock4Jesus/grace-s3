using GraceS3.Services;
using Serilog;

namespace GraceS3.Buckets.Endpoints;

public static class EndpointGroup
{
	public static void MapBuckets(this WebApplication app)
	{
		RouteGroupBuilder group = app.MapGroup("buckets")
			.RequireAuthorization()
			.WithTags("Buckets");

		group.MapGet("", HandleGetList).WithName("GetBucketList");
		group.MapPost("", CreateBucketEndpoint.Handle).WithName("CreateBucket");
		group.MapGet("{id:guid}", HandleGetOne).WithName("GetBucket");
		group.MapPut("{id:guid}", HandleUpdate).WithName("UpdateBucket");
		group.MapDelete("{id:guid}", HandleDelete).WithName("DeleteBucket");
	}

	private static async Task HandleGetList(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	private static async Task HandleDelete(Guid id)
	{
		throw new NotImplementedException();
	}

	private static async Task HandleUpdate(Guid id)
	{
		throw new NotImplementedException();
	}

	private static async Task HandleGetOne(Guid id)
	{
		throw new NotImplementedException();
	}
}
