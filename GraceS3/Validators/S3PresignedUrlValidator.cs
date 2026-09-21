using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using GraceS3.Common;
using Microsoft.Extensions.Primitives;

namespace GraceS3.Validators;

public static class S3PresignedUrlValidator
{
	public static bool Validate(
		HttpRequest request,
		string accessKey,
		string secretKey,
		string region,
		string service
	)
	{
		if (
			!GetParameter(request, PresignedUrlQueryParameters.Algorithm, out string? algorithm)
			|| algorithm != S3SignatureV4.Algorithm
			|| !GetParameter(
				request,
				PresignedUrlQueryParameters.Credential,
				out string? credential
			)
			|| !GetParameter(request, PresignedUrlQueryParameters.Date, out string? timestamp)
			|| !GetParameter(request, PresignedUrlQueryParameters.Expires, out string? expiresValue)
			|| !GetParameter(
				request,
				PresignedUrlQueryParameters.SignedHeaders,
				out string? signedHeaders
			)
			|| !GetParameter(request, PresignedUrlQueryParameters.Signature, out string? signature)
		)
			return false;

		string[] parts = credential.Split('/');
		if (
			parts.Length != 5
			|| parts[0] != accessKey
			|| parts[2] != region
			|| parts[3] != service
			|| parts[4] != S3SignatureV4.DefaultAws4Request
		)
			return false;

		if (
			!DateTimeOffset.TryParseExact(
				timestamp,
				"yyyyMMdd'T'HHmmss'Z'",
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
				out DateTimeOffset requestTime
			)
			|| parts[1] != requestTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
			|| !int.TryParse(
				expiresValue,
				NumberStyles.None,
				CultureInfo.InvariantCulture,
				out int expires
			)
			|| expires < 1
			|| expires > S3SignatureV4.MaxExpiresSeconds
		)
			return false;

		DateTimeOffset now = DateTimeOffset.UtcNow;
		if (
			now - requestTime > TimeSpan.FromSeconds(expires)
			|| requestTime - now > TimeSpan.FromMinutes(5)
		)
			return false;

		string[] headerNames = signedHeaders.Split(';');
		if (
			!headerNames.Contains("host", StringComparer.Ordinal)
			|| !headerNames.SequenceEqual(
				headerNames.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
			)
		)
			return false;

		StringBuilder canonicalHeaders = new();
		foreach (string name in headerNames)
		{
			if (string.IsNullOrEmpty(name) || name != name.ToLowerInvariant())
				return false;
			string value;
			if (name == "host")
			{
				if (!request.Host.HasValue)
					return false;
				value = request.Host.Value!;
			}
			else if (request.Headers.TryGetValue(name, out StringValues values))
				value = values.ToString();
			else
				return false;
			canonicalHeaders
				.Append(name)
				.Append(':')
				.Append(S3SignatureV4.NormalizeHeaderValue(value))
				.Append('\n');
		}

		IEnumerable<KeyValuePair<string, string>> query = request
			.Query.Where(x => x.Key != PresignedUrlQueryParameters.Signature)
			.SelectMany(x =>
				x.Value.Select(value => KeyValuePair.Create(x.Key, value ?? string.Empty))
			);
		string canonicalRequest = S3SignatureV4.CreateCanonicalRequest(
			request.Method,
			request.PathBase.Add(request.Path).Value ?? "/",
			S3SignatureV4.CanonicalQueryString(query),
			canonicalHeaders.ToString(),
			signedHeaders
		);
		string expectedSignature = S3SignatureV4.CalculateSignature(
			secretKey,
			parts[1],
			region,
			service,
			S3SignatureV4.CreateStringToSign(
				timestamp,
				S3SignatureV4.CredentialScope(parts[1], region, service),
				canonicalRequest
			)
		);

		if (signature.Length != 64)
			return false;
		try
		{
			return CryptographicOperations.FixedTimeEquals(
				Convert.FromHexString(expectedSignature),
				Convert.FromHexString(signature)
			);
		}
		catch (FormatException)
		{
			return false;
		}
	}

	private static bool GetParameter(
		HttpRequest request,
		string key,
		[NotNullWhen(true)] out string? value
	)
	{
		value = null;
		// Query collections use case-insensitive lookup; signature parameter names are case-sensitive.
		if (
			!request.Query.Keys.Contains(key, StringComparer.Ordinal)
			|| !request.Query.TryGetValue(key, out StringValues values)
			|| values.Count != 1
			|| string.IsNullOrEmpty(values[0])
		)
			return false;
		value = values[0]!;
		return true;
	}
}
