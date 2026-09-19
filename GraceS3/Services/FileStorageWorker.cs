// using GraceS3.Data;
// using GraceS3.Utils;
// using Serilog;

// namespace GraceS3.Services;

// public sealed class FileStorageWorker(IServiceProvider services) : BackgroundService
// {
// 	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
// 	{
// 		while (!cancellationToken.IsCancellationRequested)
// 		{
// 			using IServiceScope scope = services.CreateScope();
// 			Database database = scope.ServiceProvider.GetRequiredService<Database>();
// 			DiskService diskService = scope.ServiceProvider.GetRequiredService<DiskService>();

// 			await CreateFileDirectoriesIfNotExists(database, diskService, cancellationToken);

// 			// Simulate some work with a delay
// 			await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
// 		}
// 	}

// 	private static async Task CreateFileDirectoriesIfNotExists(
// 		Database database,
// 		DiskService diskService,
// 		CancellationToken cancellationToken
// 	)
// 	{
// 		try
// 		{
// 			List<ObjectEntity> files = await database
// 				.Files.Find(x => x.StorageStatus == ObjectFileStatus.ToCreate)
// 				.ToListAsync(cancellationToken);

// 			await files
// 				.Select(bucket => new CreateFileDirectoryAction(diskService, bucket.Id))
// 				.ToRollback()
// 				.ExecuteAllAsync();
// 		}
// 		catch (Exception ex)
// 		{
// 			Log.Error(ex, "Failed to create bucket directories");
// 		}
// 	}

// 	sealed class CreateFileDirectoryAction(DiskService diskService, string bucketId)
// 		: IRollbackAction
// 	{
// 		public async Task Execute()
// 		{
// 			diskService.CreateBucketDirectory(bucketId);
// 		}

// 		public async Task Undo()
// 		{
// 			await diskService.DeleteBucketDirectory(bucketId);
// 		}
// 	}
// }
