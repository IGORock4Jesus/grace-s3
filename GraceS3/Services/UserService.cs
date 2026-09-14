using System.Security.Claims;
using Keycloak.AuthServices.Common.Claims;

namespace GraceS3.Services;

public sealed class UserService(IHttpContextAccessor accessor)
{
	public Guid GetId()
	{
		string? idString = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(idString) || !Guid.TryParse(idString, out Guid id))
		{
			throw new InvalidOperationException(
				$"The user has no or invalid {ClaimTypes.NameIdentifier}"
			);
		}

		return id;
	}
}
