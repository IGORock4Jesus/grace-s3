using GraceS3.Data;
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

		GetBucketListEndpoint.Map(group);

		CreateBucketEndpoint.Map(group);

		GetBucketOneEndpoint.Map(group);

		UpdateBucketEndpoint.Map(group);

		DeleteBucketEndpoint.Map(group);
	}
}
