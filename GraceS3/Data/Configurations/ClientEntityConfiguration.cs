using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GraceS3.Data.Configurations;

public sealed class ClientEntityConfiguration : IEntityTypeConfiguration<ClientEntity>
{
	public void Configure(EntityTypeBuilder<ClientEntity> builder)
	{
		builder.HasKey(x => x.Id);

		builder.Property(x => x.AccessKey).IsRequired().HasMaxLength(100);
		builder.HasIndex(x => x.AccessKey).IsUnique();
	}
}
