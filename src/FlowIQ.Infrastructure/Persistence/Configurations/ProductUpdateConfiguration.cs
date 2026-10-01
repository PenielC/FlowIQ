using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.ProductUpdates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowIQ.Infrastructure.Persistence.Configurations;

public class ProductUpdateConfiguration : IEntityTypeConfiguration<ProductUpdate>
{
    public void Configure(EntityTypeBuilder<ProductUpdate> builder)
    {
        builder.ToTable("ProductUpdates");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Title).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Summary).IsRequired().HasMaxLength(400);
        builder.Property(u => u.LinkUrl).HasMaxLength(300);
        builder.Property(u => u.LinkLabel).HasMaxLength(40);
        builder.Property(u => u.Audience).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(u => u.PublishedAtUtc);
    }
}

public class ProductUpdateDismissalConfiguration : IEntityTypeConfiguration<ProductUpdateDismissal>
{
    public void Configure(EntityTypeBuilder<ProductUpdateDismissal> builder)
    {
        builder.ToTable("ProductUpdateDismissals");
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => new { d.UserId, d.ProductUpdateId }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductUpdate>().WithMany().HasForeignKey(d => d.ProductUpdateId).OnDelete(DeleteBehavior.Cascade);
    }
}
