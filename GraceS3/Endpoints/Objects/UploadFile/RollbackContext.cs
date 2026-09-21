using GraceS3.Data;
using GraceS3.Services;

namespace GraceS3.Endpoints.Objects.UploadFile;

internal sealed record RollbackContext(
	Guid ObjectId,
	Guid ClientId,
	string FileName,
	string ContentType,
	string ContentDisposition,
	long Size,
	Database Database,
	DiskService DiskService,
	Stream Stream
);
