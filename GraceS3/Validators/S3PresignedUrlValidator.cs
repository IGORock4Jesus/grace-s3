using System.Security.Cryptography;
using System.Text;
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
		// ------------------------------------------------------------
		// 1. Получаем X-Amz параметры
		// ------------------------------------------------------------

		if (
			!request.Query.TryGetValue("X-Amz-Algorithm", out StringValues algorithm)
			|| algorithm != "AWS4-HMAC-SHA256"
		)
		{
			return false;
		}

		if (!request.Query.TryGetValue("X-Amz-Credential", out StringValues credential))
		{
			return false;
		}

		if (!request.Query.TryGetValue("X-Amz-Date", out StringValues amzDate))
		{
			return false;
		}

		if (!request.Query.TryGetValue("X-Amz-Expires", out StringValues expiresValue))
		{
			return false;
		}

		if (!request.Query.TryGetValue("X-Amz-SignedHeaders", out StringValues signedHeaders))
		{
			return false;
		}

		if (!request.Query.TryGetValue("X-Amz-Signature", out StringValues signature))
		{
			return false;
		}

		// ------------------------------------------------------------
		// 2. Проверяем Credential
		//
		// accessKey/date/region/service/aws4_request
		// ------------------------------------------------------------

		string[] credentialParts = credential.ToString().Split('/');

		if (credentialParts.Length != 5)
			return false;

		if (credentialParts[0] != accessKey)
			return false;

		string date = credentialParts[1];
		string credentialRegion = credentialParts[2];
		string credentialService = credentialParts[3];
		string terminal = credentialParts[4];

		if (credentialRegion != region)
			return false;

		if (credentialService != service)
			return false;

		if (terminal != "aws4_request")
			return false;

		// ------------------------------------------------------------
		// 3. Проверяем дату
		// ------------------------------------------------------------

		if (
			!DateTimeOffset.TryParseExact(
				amzDate!,
				"yyyyMMdd'T'HHmmss'Z'",
				null,
				System.Globalization.DateTimeStyles.AssumeUniversal,
				out DateTimeOffset requestTime
			)
		)
		{
			return false;
		}

		// ------------------------------------------------------------
		// 4. Проверяем Expires
		// ------------------------------------------------------------

		if (!int.TryParse(expiresValue, out int expires))
			return false;

		// Например, ограничиваем максимальный TTL.
		if (expires <= 0 || expires > 7 * 24 * 60 * 60)
			return false;

		DateTimeOffset now = DateTimeOffset.UtcNow;

		DateTimeOffset expiresAt = requestTime.AddSeconds(expires);

		if (now > expiresAt)
			return false;

		// Защита от URL с датой сильно в будущем.
		if (requestTime > now.AddMinutes(5))
			return false;

		// ------------------------------------------------------------
		// 5. Проверяем SignedHeaders
		// ------------------------------------------------------------

		string signedHeadersValue = signedHeaders.ToString();

		if (string.IsNullOrWhiteSpace(signedHeadersValue))
			return false;

		string[] signedHeaderNames = signedHeadersValue.Split(
			';',
			StringSplitOptions.RemoveEmptyEntries
		);

		if (!signedHeaderNames.Contains("host", StringComparer.Ordinal))
		{
			return false;
		}

		// ------------------------------------------------------------
		// 6. Создаем Canonical Request
		// ------------------------------------------------------------

		string canonicalQueryString = BuildCanonicalQueryString(request.Query);

		string canonicalHeaders = BuildCanonicalHeaders(request, signedHeaderNames);

		string payloadHash = "UNSIGNED-PAYLOAD";

		string canonicalRequest =
			$"{request.Method}\n"
			+ $"{GetCanonicalUri(request)}\n"
			+ $"{canonicalQueryString}\n"
			+ $"{canonicalHeaders}\n"
			+ $"{signedHeadersValue}\n"
			+ $"{payloadHash}";

		// ------------------------------------------------------------
		// 7. String To Sign
		// ------------------------------------------------------------

		string credentialScope = $"{date}/{region}/{service}/aws4_request";

		string canonicalRequestHash = Sha256Hex(canonicalRequest);

		string stringToSign =
			$"AWS4-HMAC-SHA256\n"
			+ $"{amzDate}\n"
			+ $"{credentialScope}\n"
			+ $"{canonicalRequestHash}";

		// ------------------------------------------------------------
		// 8. Получаем signing key
		// ------------------------------------------------------------

		byte[] signingKey = DeriveSigningKey(secretKey, date, region, service);

		// ------------------------------------------------------------
		// 9. Вычисляем ожидаемую подпись
		// ------------------------------------------------------------

		string expectedSignature = HmacSha256Hex(signingKey, stringToSign);

		// ------------------------------------------------------------
		// 10. Constant-time comparison
		// ------------------------------------------------------------

		try
		{
			return CryptographicOperations.FixedTimeEquals(
				Convert.FromHexString(expectedSignature),
				Convert.FromHexString(signature!)
			);
		}
		catch (FormatException)
		{
			return false;
		}
	}

	private static string BuildCanonicalQueryString(IQueryCollection query)
	{
		return string.Join(
			"&",
			query
				.Where(x =>
					!string.Equals(x.Key, "X-Amz-Signature", StringComparison.OrdinalIgnoreCase)
				)
				.SelectMany(x =>
					x.Value.Select(value => new KeyValuePair<string, string>(
						x.Key,
						value ?? string.Empty
					))
				)
				.OrderBy(x => x.Key, StringComparer.Ordinal)
				.ThenBy(x => x.Value, StringComparer.Ordinal)
				.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}")
		);
	}

	private static string BuildCanonicalHeaders(HttpRequest request, string[] signedHeaders)
	{
		StringBuilder result = new StringBuilder();

		foreach (string headerName in signedHeaders)
		{
			string value;

			if (headerName == "host")
			{
				value = request.Host.Value!;
			}
			else
			{
				if (!request.Headers.TryGetValue(headerName, out StringValues headerValue))
				{
					throw new InvalidOperationException(
						$"Signed header '{headerName}' is missing."
					);
				}

				value = headerValue.ToString();
			}

			value = NormalizeHeaderValue(value);

			result.Append(headerName);
			result.Append(':');
			result.Append(value);
			result.Append('\n');
		}

		return result.ToString();
	}

	private static string NormalizeHeaderValue(string value)
	{
		return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
	}

	private static string GetCanonicalUri(HttpRequest request)
	{
		// Важно: здесь нужно использовать именно URI encoding,
		// соответствующий S3 SigV4, а не произвольный Uri.EscapeUriString.
		string path = request.Path.Value ?? "/";

		return Uri.EscapeDataString(path).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
	}

	private static byte[] DeriveSigningKey(
		string secretKey,
		string date,
		string region,
		string service
	)
	{
		byte[] kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secretKey), date);

		byte[] kRegion = HmacSha256(kDate, region);

		byte[] kService = HmacSha256(kRegion, service);

		return HmacSha256(kService, "aws4_request");
	}

	private static byte[] HmacSha256(byte[] key, string data)
	{
		using HMACSHA256 hmac = new HMACSHA256(key);

		return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
	}

	private static string HmacSha256Hex(byte[] key, string data)
	{
		return Convert.ToHexString(HmacSha256(key, data)).ToLowerInvariant();
	}

	private static string Sha256Hex(string data)
	{
		return Convert
			.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data)))
			.ToLowerInvariant();
	}
}
