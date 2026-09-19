using GraceS3.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace GraceS3.Data;

public sealed class Database(DbContextOptions<Database> options) : DbContext(options)
{
	public DbSet<ClientEntity> Clients => Set<ClientEntity>();
	public DbSet<ObjectEntity> Objects => Set<ObjectEntity>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClientEntityConfiguration).Assembly);
	}
}
