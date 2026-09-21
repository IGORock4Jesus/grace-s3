using GraceS3.Utils;

namespace GraceS3.Endpoints.Objects.UploadFile;

internal sealed class PutFileOnDisk : IRollbackAction<RollbackContext>
{
	public async Task Execute(RollbackContext context, CancellationToken cancellationToken)
	{
		await context.DiskService.PutFile(context.ClientId, context.ObjectId, context.Stream);
	}

	public async Task Undo(RollbackContext context, CancellationToken cancellationToken)
	{
		await context.DiskService.TryDeleteFile(context.ClientId, context.ObjectId);
	}
}
