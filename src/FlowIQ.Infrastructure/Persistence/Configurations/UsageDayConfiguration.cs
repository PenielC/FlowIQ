using FlowIQ.Domain.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowIQ.Infrastructure.Persistence.Configurations;

public class UsageDayConfiguration : IEntityTypeConfiguration<UsageDay>
{
    public void Configure(EntityTypeBuilder<UsageDay> builder)
    {
        builder.ToTable("UsageDays");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Feature).IsRequired().HasMaxLength(40);
        builder.Property(u => u.Day).HasColumnType("date");
        // The upsert in UsageStore relies on this exact key.
        builder.HasIndex(u => new { u.UserId, u.Feature, u.Day }).IsUnique();
        builder.HasIndex(u => new { u.Day, u.CompanyId });
        builder.HasIndex(u => new { u.CompanyId, u.Feature });
    }
}
