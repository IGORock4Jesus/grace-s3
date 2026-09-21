using GraceS3.Data;
using GraceS3.Utils;
using Microsoft.EntityFrameworkCore;

namespace GraceS3.Endpoints.Objects.UploadFile;

internal sealed class CreateObjectInDatabase : IRollbackAction<RollbackContext>
{
	public async Task Execute(RollbackContext context, CancellationToken cancellationToken)
	{
		await context.Database.Objects.AddAsync(
			new ObjectEntity()
			{
				Id = context.ObjectId,
				ClientId = context.ClientId,
				FileName = context.FileName,
				ContentType = context.ContentType,
				ContentDisposition = context.ContentDisposition,
				Size = context.Size,
			},
			cancellationToken
		);

		await context.Database.SaveChangesAsync(cancellationToken);
	}

	public async Task Undo(RollbackContext context, CancellationToken cancellationToken)
	{
		ObjectEntity? @object = await context.Database.Objects.FirstOrDefaultAsync(
			x => x.Id == context.ObjectId,
			cancellationToken
		);
		if (@object is null)
		{
			return;
		}

		context.Database.Remove(@object);

		await context.Database.SaveChangesAsync(cancellationToken);
	}
}
