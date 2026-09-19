using GraceS3.Data;
using GraceS3.Utils;
using Microsoft.EntityFrameworkCore;

namespace GraceS3.Endpoints.Clients.CreateClient;

class CreateClientAction : IRollbackAction<RollbackContext>
{
	public async Task Execute(RollbackContext context, CancellationToken cancellationToken)
	{
		ClientEntity client = new()
		{
			Id = context.Id,
			AccessKey = context.AccessKey,
			SecretKey = context.SecretKey,
		};
		await context.Database.Clients.AddAsync(client, cancellationToken);
		await context.Database.SaveChangesAsync(cancellationToken);
	}

	public async Task Undo(RollbackContext context, CancellationToken cancellationToken)
	{
		ClientEntity? entity = await context.Database.Clients.FirstOrDefaultAsync(
			x => x.Id == context.Id,
			cancellationToken
		);
		if (entity is null)
		{
			return;
		}

		context.Database.Clients.Remove(entity);
		await context.Database.SaveChangesAsync(cancellationToken);
	}
}
