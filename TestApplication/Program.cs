// See https://aka.ms/new-console-template for more information
using GraceS3.Common;

Console.WriteLine("Hello, World!");

string preSignedUrl = PresignedUrlGenerator.Generate(
	new PresignedUrlOptions(
		AccessKey: "igor-kozlov",
		SecretKey: "qweQWE123!@#",
		Region: "spb",
		Service: "s3",
		Host: "locahost:5000",
		Method: "GET",
		Path: "/objects/abc/photo.jpg",
		Expires: TimeSpan.FromDays(1),
		RequireTLS: false
	)
);

Console.WriteLine($"Presigned URL: {preSignedUrl}");
