using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GraceS3.Data.Configurations;

public sealed class ObjectEntityConfiguration : IEntityTypeConfiguration<ObjectEntity>
{
	public void Configure(EntityTypeBuilder<ObjectEntity> builder)
	{
		builder.HasKey(x => x.Id);

		builder
			.HasOne(x => x.Client)
			.WithMany(x => x.Objects)
			.HasForeignKey(x => x.ClientId)
			.IsRequired()
			.OnDelete(DeleteBehavior.NoAction);

		builder.Property(x => x.FileName).IsRequired().HasMaxLength(256);
		builder.Property(x => x.ContentType).IsRequired().HasMaxLength(256);
		builder.Property(x => x.Size);

		builder.Property(x => x.FileStatus).HasConversion<string>();
	}
}
