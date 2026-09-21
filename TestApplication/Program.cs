// See https://aka.ms/new-console-template for more information
using GraceS3.Common;

Console.WriteLine("Hello, World!");

NewMethod("INFO", "Get info URL", "/objects/01a0c55f-c657-701e-868f-9cc74fca544e");
NewMethod("GET", "Download URL", "/objects/01a0c55f-c657-701e-868f-9cc74fca544e");
NewMethod("POST", "Upload URL", "/objects");

static void NewMethod(string method, string outputMessage, string path)
{
	string downloadUrl = PresignedUrlGenerator.Generate(
		new PresignedUrlOptions(
			AccessKey: "igor",
			SecretKey: "qwe",
			Region: "spb",
			Service: "s3",
			Host: "localhost:5000",
			Method: method,
			Path: path,
			Expires: TimeSpan.FromDays(1),
			RequireTLS: false
		)
	);

	Console.WriteLine($"{outputMessage}: {downloadUrl}");
}
