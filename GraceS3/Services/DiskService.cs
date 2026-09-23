using GraceS3.Data;
using Microsoft.Extensions.Options;

namespace GraceS3.Services;

public sealed class DiskService(IOptions<ApplicationConfiguration> options)
{

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
		return Path.GetFullPath(options.Value.WorkingDirectory);
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

		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		await using FileStream writer = new(path, FileMode.CreateNew, FileAccess.Write);
		await stream.CopyToAsync(writer);
	}

	internal Task TryDeleteFile(Guid clientId, Guid objectId)
	{
		string path = GetFilePath(clientId.ToString(), objectId.ToString());
		try
		{
			File.Delete(path);
		}
		catch (DirectoryNotFoundException)
		{
			// The file is already gone, including its client directory.
		}
		return Task.CompletedTask;
	}
}
