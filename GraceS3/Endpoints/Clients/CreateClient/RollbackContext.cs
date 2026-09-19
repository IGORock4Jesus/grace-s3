using GraceS3.Data;
using GraceS3.Services;

namespace GraceS3.Endpoints.Clients.CreateClient;

record RollbackContext(
	Guid Id,
	string AccessKey,
	string SecretKey,
	Database Database,
	DiskService DiskService
);
