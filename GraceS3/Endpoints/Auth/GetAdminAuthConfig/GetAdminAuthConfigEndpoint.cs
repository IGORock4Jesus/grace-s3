using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Serilog;

namespace GraceS3.Endpoints.Auth.GetAdminAuthConfig;

public static class GetAdminAuthConfigEndpoint
{
	public static void MapGetAdminAuthConfig(this WebApplication application)
	{
		application
			.MapGet("admin/auth/config", Handle)
			.WithName("GetAdminAuthConfig")
			.WithTags("Admin")
			.AllowAnonymous()
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.Produces<GetAdminAuthConfigResponse>(StatusCodes.Status200OK);
	}

	private static Ok<GetAdminAuthConfigResponse> Handle(IOptions<ApplicationConfiguration> options)
	{
		try
		{
			return TypedResults.Ok(
				new GetAdminAuthConfigResponse(
					options.Value.OAuthUri,
					options.Value.OAuthRealm,
					options.Value.OAuthAdminWebClientID
				)
			);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to get admin auth config");

			throw;
		}
	}
}
