using System.Security.Cryptography;
using System.Text;

namespace GraceS3.Common;

public static class S3SignatureV4
{
	public const string DefaultAws4Request = "aws4_request";

	public const string Algorithm = "AWS4-HMAC-SHA256";
	public const string SignedHeaders = "host";
	public const string PayloadHash = "UNSIGNED-PAYLOAD";
	public const int MaxExpiresSeconds = 7 * 24 * 60 * 60;

	// Paths are decoded object paths; preserve separators and empty segments.
	public static string CanonicalUri(string path)
	{
		if (!path.StartsWith('/')) path = "/" + path;
		return string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
	}

	public static string CanonicalQueryString(IEnumerable<KeyValuePair<string, string>> query)
	{
		return string.Join("&", query
		 .Select(x => new KeyValuePair<string, string>(Uri.EscapeDataString(x.Key), Uri.EscapeDataString(x.Value)))
		 .OrderBy(x => x.Key, StringComparer.Ordinal)
		 .ThenBy(x => x.Value, StringComparer.Ordinal)
		 .Select(x => $"{x.Key}={x.Value}"));
	}

	public static string CredentialScope(string date, string region, string service) =>
	 $"{date}/{region}/{service}/{DefaultAws4Request}";

	public static string CreateCanonicalRequest(string method, string path, string canonicalQuery, string canonicalHeaders, string signedHeaders) =>
	 $"{method}\n{CanonicalUri(path)}\n{canonicalQuery}\n{canonicalHeaders}\n{signedHeaders}\n{PayloadHash}";

	public static string CreateStringToSign(string timestamp, string scope, string canonicalRequest) =>
	 $"{Algorithm}\n{timestamp}\n{scope}\n{Sha256Hex(canonicalRequest)}";

	public static string NormalizeHeaderValue(string value) =>
	 string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

	public static string HmacSha256Hex(byte[] key, string data)
	{
		return Convert.ToHexString(HmacSha256(key, data)).ToLowerInvariant();
	}

	public static byte[] HmacSha256(byte[] key, string data)
	{
		using HMACSHA256 hmac = new(key);

		return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
	}

	public static string Sha256Hex(string data)
	{
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));

		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	public static byte[] DeriveSigningKey(
		string secretKey,
		string date,
		string region,
		string service
	)
	{
		byte[] kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secretKey), date);

		byte[] kRegion = HmacSha256(kDate, region);

		byte[] kService = HmacSha256(kRegion, service);

		return HmacSha256(kService, DefaultAws4Request);
	}

	public static string CalculateSignature(
		string secretKey,
		string date,
		string region,
		string service,
		string stringToSign
	)
	{
		byte[] signingKey = DeriveSigningKey(secretKey, date, region, service);

		return HmacSha256Hex(signingKey, stringToSign);
	}
}
