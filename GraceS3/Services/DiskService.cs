using GraceS3.Data;
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
}
