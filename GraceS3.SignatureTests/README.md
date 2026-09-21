# Signature regression checks

Run with `dotnet run --project GraceS3.SignatureTests` from the repository root.
The executable exits with an error if a check fails. No test framework packages
or running database/server are required; the production validator is linked into
this project and exercised with ASP.NET Core request objects.

Covers generation/validation round trips, URL encoding, encoded query sorting,
request tampering, duplicate parameters, expiration limits, signed expired/future
requests, PathBase, culture independence, and the published AWS SigV4 signature:
https://docs.aws.amazon.com/AmazonS3/latest/API/sigv4-query-string-auth.html

`PresignedUrlOptions.Path` is a decoded path, not an already percent-encoded URI.
GraceS3 uses its existing `X-Grace-*` parameter names.
