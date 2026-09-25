using FlowIQ.Domain.StripeSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowIQ.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.PlanKey).IsRequired().HasMaxLength(50);
        builder.Property(s => s.StripeCustomerId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.StripeSubscriptionId).HasMaxLength(100);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.CompanyId).IsUnique();
        builder.HasIndex(s => s.StripeCustomerId).IsUnique();
    }
}
