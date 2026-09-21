using System.Globalization;
using GraceS3.Common;
using GraceS3.Validators;
using Microsoft.AspNetCore.Http;

int count = 0;
void Check(bool condition, string name)
{
	if (!condition) throw new Exception(name);
	count++;
}
HttpRequest Request(string url)
{
	var uri = new Uri(url);
	var request = new DefaultHttpContext().Request;
	request.Method = "GET";
	request.Host = new HostString(uri.Authority);
	request.Path = PathString.FromUriComponent(uri);
	request.QueryString = new QueryString(uri.Query);
	return request;
}
bool Valid(HttpRequest request) => S3PresignedUrlValidator.Validate(request, "key", "secret", "region", "s3");
var options = new PresignedUrlOptions("key", "secret", "region", "s3", "localhost:5000", "GET", "/objects/file", TimeSpan.FromHours(1));
foreach (string path in new[] { "/objects/file", "objects/file", "", "/", "/a//b/", "/файл с пробелом+?#%" })
	Check(Valid(Request(PresignedUrlGenerator.Generate(options with { Path = path }))), $"Round trip: {path}");
string url = PresignedUrlGenerator.Generate(options);
foreach (string parameter in new[] { "Algorithm", "Credential", "Date", "Expires", "SignedHeaders", "Signature" })
{
	var request = Request(url);
	request.QueryString = request.QueryString.Add("X-Grace-" + parameter, "invalid");
	Check(!Valid(request), "Duplicate " + parameter);
	request = Request(url);
	request.Query = new QueryCollection(request.Query.ToDictionary(x => x.Key, x => x.Key == "X-Grace-" + parameter ? new Microsoft.Extensions.Primitives.StringValues("invalid") : x.Value));
	Check(!Valid(request), "Invalid " + parameter);
}
var changed = Request(url); changed.Method = "PUT"; Check(!Valid(changed), "Method tamper");
changed = Request(url); changed.Host = new HostString("other"); Check(!Valid(changed), "Host tamper");
changed = Request(url); changed.Path = "/other"; Check(!Valid(changed), "Path tamper");
changed = Request(url); changed.QueryString = changed.QueryString.Add("extra", "1"); Check(!Valid(changed), "Query tamper");
changed = Request(url); changed.PathBase = "/objects"; changed.Path = "/file"; Check(Valid(changed), "PathBase");
foreach (string expiration in new[] { "0", "-1", "604801", "2147483648" })
{
	changed = Request(url);
	changed.Query = new QueryCollection(changed.Query.ToDictionary(x => x.Key, x => x.Key == PresignedUrlQueryParameters.Expires ? new Microsoft.Extensions.Primitives.StringValues(expiration) : x.Value));
	Check(!Valid(changed), "Expiration " + expiration);
}
foreach (double seconds in new[] { 0, -1, 0.5, 604801 })
{
	bool threw = false;
	try { PresignedUrlGenerator.Generate(options with { Expires = TimeSpan.FromSeconds(seconds) }); }
	catch (ArgumentOutOfRangeException) { threw = true; }
	Check(threw, "Generator expiration");
}
// Re-sign altered dates to exercise time checks independently of signature mismatches.
foreach (var offset in new[] { TimeSpan.FromHours(-2), TimeSpan.FromMinutes(10) })
{
	changed = Request(url);
	var query = changed.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
	var time = DateTimeOffset.UtcNow.Add(offset);
	string date = time.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
	string timestamp = time.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
	string scope = S3SignatureV4.CredentialScope(date, "region", "s3");
	query[PresignedUrlQueryParameters.Date] = timestamp;
	query[PresignedUrlQueryParameters.Credential] = "key/" + scope;
	query.Remove(PresignedUrlQueryParameters.Signature);
	string requestText = S3SignatureV4.CreateCanonicalRequest("GET", "/objects/file", S3SignatureV4.CanonicalQueryString(query), "host:localhost:5000\n", "host");
	query[PresignedUrlQueryParameters.Signature] = S3SignatureV4.CalculateSignature("secret", date, "region", "s3", S3SignatureV4.CreateStringToSign(timestamp, scope, requestText));
	changed.QueryString = new QueryString("?" + S3SignatureV4.CanonicalQueryString(query));
	Check(!Valid(changed), "Signed expired/future request");
}
CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
Check(Valid(Request(PresignedUrlGenerator.Generate(options))), "Invariant culture");
Check(S3SignatureV4.CanonicalUri("/a b/+%") == "/a%20b/%2B%25", "URI encoding");
Check(S3SignatureV4.CanonicalQueryString(new[] { KeyValuePair.Create("z", "0"), KeyValuePair.Create("é", "2"), KeyValuePair.Create("z", "+") }) == "%C3%A9=2&z=%2B&z=0", "Encoded sorting");
// Published AWS S3 SigV4 query authentication example (2013-05-24).
string canonical = "GET\n/test.txt\nX-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIAIOSFODNN7EXAMPLE%2F20130524%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20130524T000000Z&X-Amz-Expires=86400&X-Amz-SignedHeaders=host\nhost:examplebucket.s3.amazonaws.com\n\nhost\nUNSIGNED-PAYLOAD";
Check(S3SignatureV4.CalculateSignature("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", "20130524", "us-east-1", "s3", S3SignatureV4.CreateStringToSign("20130524T000000Z", "20130524/us-east-1/s3/aws4_request", canonical)) == "aeeed9bbccd4d02ee5c0109b86d86835f995330da4c265957d157751f604d404", "AWS reference signature");
Console.WriteLine($"Passed {count} signature checks.");
