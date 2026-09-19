namespace GraceS3.Common;

public sealed record PresignedUrlOptions(
	string AccessKey,
	string SecretKey,
	string Region,
	string Service,
	string Host,
	string Method,
	string Path,
	TimeSpan Expires,
	bool RequireTLS = true
);
