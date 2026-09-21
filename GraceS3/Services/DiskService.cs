using GraceS3.Data;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace GraceS3.Services;

public sealed class DiskService(IOptions<ApplicationConfiguration> options)
{
	private const string TempAppDirectory = "grace-s3";

	public string GetFilePath(string bucketId, string fileId)
	{
		return Path.Combine(GetWorkingDirectory(), bucketId, fileId);
	}

	internal void TryCreateDirectory(string directoryName)
	{
		string path = Path.Combine(GetWorkingDirectory(), directoryName);
		if (Directory.Exists(path))
		{
			return;
		}

		Directory.CreateDirectory(path);
	}

	public void TryDeleteDirectory(string directoryName)
	{
		string bucketDirectoryPath = Path.Combine(GetWorkingDirectory(), directoryName);
		if (!Directory.Exists(bucketDirectoryPath))
		{
			return;
		}

		Directory.Delete(bucketDirectoryPath, true);
	}

	private string GetWorkingDirectory()
	{
		return Path.Combine(Path.GetTempPath(), TempAppDirectory, options.Value.WorkingDirectory);
	}

	internal Stream OpenStream(ObjectEntity @object)
	{
		string path = Path.Combine(
			GetWorkingDirectory(),
			@object.ClientId.ToString(),
			@object.Id.ToString()
		);
		if (!File.Exists(path))
		{
			throw new FileNotFoundException($"File not found: {path}");
		}

		return File.OpenRead(path);
	}

	internal async Task PutFile(Guid clientId, Guid objectId, Stream stream)
	{
		string path = Path.Combine(GetWorkingDirectory(), clientId.ToString(), objectId.ToString());

		if (File.Exists(path))
		{
			throw new InvalidProgramException($"The same file is already exist: {path}");
		}

		await using FileStream writer = File.OpenWrite(path);
		await stream.CopyToAsync(writer);
	}

	internal async Task TryDeleteFile(Guid clientId, Guid objectId)
	{
		string path = Path.Combine(GetWorkingDirectory(), clientId.ToString(), objectId.ToString());
		if (!File.Exists(path))
		{
			return;
		}

		File.Delete(path);
	}
}
