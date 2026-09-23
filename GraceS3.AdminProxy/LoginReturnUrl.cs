namespace GraceS3.AdminProxy;

public static class LoginReturnUrl
{
	public static string GetLocalPath(string? value)
	{
		// Only absolute local paths; reject protocol-relative URLs and browser slash normalization.
		if (string.IsNullOrEmpty(value) || value[0] != '/' || value.StartsWith("//")
			|| value.Contains('\\') || value.Any(char.IsControl)) return "/";
		// Authentication endpoints must not become the post-login destination.
		if (value.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase)) return "/";
		return value;
	}
}
