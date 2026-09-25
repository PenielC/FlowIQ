using FlowIQ.Domain.StripeSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowIQ.Infrastructure.Persistence.Configurations;

public class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.ToTable("ProcessedWebhookEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.StripeEventId).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => e.StripeEventId).IsUnique();
    }
}
