using GraceS3.Common;
using Microsoft.AspNetCore.Mvc;

namespace GraceS3.Endpoints.Objects;

public record GraceAuthRequest(
	[property: FromQuery(Name = PresignedUrlQueryParameters.Algorithm)] string Algorithm,
	[property: FromQuery(Name = PresignedUrlQueryParameters.Credential)] string Credential,
	[property: FromQuery(Name = PresignedUrlQueryParameters.Date)] string Date,
	[property: FromQuery(Name = PresignedUrlQueryParameters.Expires)] string Expires,
	[property: FromQuery(Name = PresignedUrlQueryParameters.Signature)] string Signature,
	[property: FromQuery(Name = PresignedUrlQueryParameters.SignedHeaders)] string SignedHeaders
);
