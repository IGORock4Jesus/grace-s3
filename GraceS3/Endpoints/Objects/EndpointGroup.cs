using GraceS3.Endpoints.Objects;
using Microsoft.AspNetCore.Mvc;

namespace GraceS3.Files.Endpoints;

public static class ObjectsEndpointGroup
{
	public static void MapObjectsEndpoints(this WebApplication application)
	{
		RouteGroupBuilder group = application
			.MapGroup("objects")
			.RequireAuthorization()
			.WithTags("Objects");

		// GetFilesListEndpoint.Map(group);

		// UploadFileEndpoint.Map(group);

		DownloadObjectEndpoint.Map(group);

		// DeleteFileEndpoint.Map(group);
	}
}
