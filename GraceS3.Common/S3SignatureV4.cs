using System.Security.Cryptography;
using System.Text;

namespace GraceS3.Common;

public static class S3SignatureV4
{
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

		return HmacSha256(kService, "aws4_request");
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
