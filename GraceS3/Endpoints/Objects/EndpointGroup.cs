using GraceS3.Endpoints.Objects;
using GraceS3.Endpoints.Objects.UploadFile;
using Microsoft.AspNetCore.Mvc;

namespace GraceS3.Files.Endpoints;

public static class ObjectsEndpointGroup
{
	public static void MapObjectsEndpoints(this WebApplication application)
	{
		RouteGroupBuilder group = application.MapGroup("objects").WithTags("Objects");

		GetObjectInfoEndpoint.Map(group);

		UploadObjectEndpoint.Map(group);

		DownloadObjectEndpoint.Map(group);

		// DeleteFileEndpoint.Map(group);
	}
}
