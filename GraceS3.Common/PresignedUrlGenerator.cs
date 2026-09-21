using System.Globalization;

namespace GraceS3.Common;

public static class PresignedUrlGenerator
{
	public const string DefaultAlgorithm = S3SignatureV4.Algorithm;
	public const string signedHeaders = S3SignatureV4.SignedHeaders;
	public const string PayloadHash = S3SignatureV4.PayloadHash;

	public static string Generate(PresignedUrlOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		if (options.Expires.TotalSeconds < 1 || options.Expires.TotalSeconds > S3SignatureV4.MaxExpiresSeconds)
			throw new ArgumentOutOfRangeException(nameof(options.Expires));

		DateTimeOffset now = DateTimeOffset.UtcNow;
		string timestamp = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
		string date = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
		string scope = S3SignatureV4.CredentialScope(date, options.Region, options.Service);
		Dictionary<string, string> query = new(StringComparer.Ordinal)
		{
			[PresignedUrlQueryParameters.Algorithm] = DefaultAlgorithm,
			[PresignedUrlQueryParameters.Credential] = $"{options.AccessKey}/{scope}",
			[PresignedUrlQueryParameters.Date] = timestamp,
			[PresignedUrlQueryParameters.Expires] = ((long)options.Expires.TotalSeconds).ToString(CultureInfo.InvariantCulture),
			[PresignedUrlQueryParameters.SignedHeaders] = signedHeaders,
		};
		string canonicalRequest = S3SignatureV4.CreateCanonicalRequest(
		 options.Method, options.Path, S3SignatureV4.CanonicalQueryString(query),
		 $"host:{S3SignatureV4.NormalizeHeaderValue(options.Host)}\n", signedHeaders);
		query[PresignedUrlQueryParameters.Signature] = S3SignatureV4.CalculateSignature(
		 options.SecretKey, date, options.Region, options.Service,
		 S3SignatureV4.CreateStringToSign(timestamp, scope, canonicalRequest));

		return $"http{(options.RequireTLS ? "s" : "")}://{options.Host}{S3SignatureV4.CanonicalUri(options.Path)}?{S3SignatureV4.CanonicalQueryString(query)}";
	}
}
