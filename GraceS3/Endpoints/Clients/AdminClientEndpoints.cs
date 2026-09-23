using GraceS3.Data;
using GraceS3.Services;
using Microsoft.EntityFrameworkCore;

namespace GraceS3.Endpoints.Clients;

public static class AdminClientEndpoints
{
	public sealed record AdminObjectResponse(Guid Id, string FileName, string ContentType, long Size);

	public static void Map(IEndpointRouteBuilder group)
	{
		group.MapDelete("/{clientId:guid}/files/{objectId:guid}", DeleteFile)
			.WithName("AdminDeleteFile")
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status404NotFound);
		group.MapDelete("/{clientId:guid}", DeleteClient).WithName("DeleteClient");
		group.MapGet("/{clientId:guid}/files", ListFiles).WithName("ListClientFiles")
			.Produces<List<AdminObjectResponse>>();
		group.MapGet("/{clientId:guid}/files/{objectId:guid}", DownloadFile).WithName("AdminDownloadFile");
	}

	private static async Task<IResult> DeleteClient(Guid clientId, Database database,
		DiskService disk, CancellationToken cancellationToken)
	{
		var client = await database.Clients.FindAsync([clientId], cancellationToken);
		if (client is null) return Results.NotFound();
		if (await database.Objects.AnyAsync(x => x.ClientId == clientId, cancellationToken))
			return Results.Conflict("Client still has files. Delete its files before deleting the client.");
		database.Clients.Remove(client);
		try
		{
			await database.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23503" })
		{
			return Results.Conflict("Client still has files.");
		}
		disk.TryDeleteDirectory(clientId.ToString());
		return Results.NoContent();
	}

	private static async Task<IResult> ListFiles(Guid clientId, Database database, CancellationToken cancellationToken)
	{
		if (!await database.Clients.AnyAsync(x => x.Id == clientId, cancellationToken)) return Results.NotFound();
		return Results.Ok(await database.Objects.AsNoTracking().Where(x => x.ClientId == clientId)
			.OrderBy(x => x.FileName).ThenBy(x => x.Id)
			.Select(x => new AdminObjectResponse(x.Id, x.FileName, x.ContentType, x.Size))
			.ToListAsync(cancellationToken));
	}

	private static async Task<IResult> DeleteFile(Guid clientId, Guid objectId, Database database,
		DiskService disk, CancellationToken cancellationToken)
	{
		var file = await database.Objects.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == objectId && x.ClientId == clientId, cancellationToken);
		if (file is null) return Results.NotFound();

		// Retain metadata if disk deletion fails so the administrator can retry.
		// A missing disk file is harmless: retry also completes a previous partial deletion.
		await disk.TryDeleteFile(clientId, objectId);
		await database.Objects.Where(x => x.Id == objectId && x.ClientId == clientId)
			.ExecuteDeleteAsync(cancellationToken);
		return Results.NoContent();
	}

	private static async Task<IResult> DownloadFile(Guid clientId, Guid objectId, Database database,
		DiskService disk, CancellationToken cancellationToken)
	{
		var file = await database.Objects.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == objectId && x.ClientId == clientId, cancellationToken);
		if (file is null) return Results.NotFound();
		try
		{
			return Results.File(disk.OpenStream(file), "application/octet-stream", file.FileName,
				enableRangeProcessing: true);
		}
		catch (FileNotFoundException) { return Results.NotFound(); }
	}
}
