using GraceS3.Data;
using GraceS3.Endpoints.Clients.CreateClient;
using Keycloak.AuthServices.Authorization;

namespace GraceS3.Endpoints.Clients;

public static class ClientsEndpointsGroupMapper
{
	public static void MapCliensEndpoints(this WebApplication application)
	{
		RouteGroupBuilder group = application
			.MapGroup("clients")
			.RequireAuthorization(x => x.RequireRealmRoles(Roles.Admin))
			.WithTags("Clients");

		GetClientListEndpoint.Map(group);
		CreateClientEndpoint.Map(group);
	}
}
