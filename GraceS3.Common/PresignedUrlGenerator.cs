namespace GraceS3.Common;

public static class PresignedUrlGenerator
{
	public static string Generate(PresignedUrlOptions options)
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;

		string amzDate = now.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");

		string date = now.ToUniversalTime().ToString("yyyyMMdd");

		long expires = (long)options.Expires.TotalSeconds;

		string credential =
			$"{options.AccessKey}/{date}/{options.Region}/{options.Service}/aws4_request";

		SortedDictionary<string, string> query = new SortedDictionary<string, string>(
			StringComparer.Ordinal
		)
		{
			["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
			["X-Amz-Credential"] = credential,
			["X-Amz-Date"] = amzDate,
			["X-Amz-Expires"] = expires.ToString(),
			["X-Amz-SignedHeaders"] = "host",
		};

		string canonicalQueryString = CanonicalQueryString(query);

		string canonicalHeaders = $"host:{options.Host}\n";

		string signedHeaders = "host";

		string payloadHash = "UNSIGNED-PAYLOAD";

		string canonicalRequest =
			$"{options.Method}\n"
			+ $"{CanonicalUri(options.Path)}\n"
			+ $"{canonicalQueryString}\n"
			+ $"{canonicalHeaders}\n"
			+ $"{signedHeaders}\n"
			+ $"{payloadHash}";

		string credentialScope = $"{date}/{options.Region}/{options.Service}/aws4_request";

		string stringToSign =
			$"AWS4-HMAC-SHA256\n"
			+ $"{amzDate}\n"
			+ $"{credentialScope}\n"
			+ $"{S3SignatureV4.Sha256Hex(canonicalRequest)}";

		string signature = S3SignatureV4.CalculateSignature(
			options.SecretKey,
			date,
			options.Region,
			options.Service,
			stringToSign
		);

		query["X-Amz-Signature"] = signature;

		return BuildUrl(options.Host, options.Path, query, options.RequireTLS);
	}

	private static string CanonicalQueryString(SortedDictionary<string, string> query)
	{
		return string.Join(
			"&",
			query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}")
		);
	}

	private static string CanonicalUri(string path)
	{
		if (!path.StartsWith('/'))
		{
			path = "/" + path;
		}

		return Uri.EscapeDataString(path);
	}

	private static string BuildUrl(
		string host,
		string path,
		SortedDictionary<string, string> query,
		bool requireTLS
	)
	{
		string queryString = CanonicalQueryString(query);

		return $"http{(requireTLS ? "s" : "")}://{host}{path}?{queryString}";
	}
}
