using GraceS3.Utils;

namespace GraceS3.Endpoints.Clients.CreateClient;

class CreateClientDirectoryAction() : IRollbackAction<RollbackContext>
{
	public Task Execute(RollbackContext context, CancellationToken cancellationToken)
	{
		context.DiskService.TryCreateDirectory(context.Id.ToString());

		return Task.CompletedTask;
	}

	public Task Undo(RollbackContext context, CancellationToken cancellationToken)
	{
		context.DiskService.TryDeleteDirectory(context.Id.ToString());

		return Task.CompletedTask;
	}
}
