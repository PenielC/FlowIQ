using FlowIQ.Domain.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowIQ.Infrastructure.Persistence.Configurations;

public class InvoiceEmailConfiguration : IEntityTypeConfiguration<InvoiceEmail>
{
    public void Configure(EntityTypeBuilder<InvoiceEmail> builder)
    {
        builder.ToTable("InvoiceEmails");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ToEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Error).HasMaxLength(500);

        builder.HasOne<Invoice>().WithMany().HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.InvoiceId, e.AtUtc });

        // Each reminder day goes out once per invoice; a failed attempt doesn't count, so it's retried.
        builder.HasIndex(e => new { e.InvoiceId, e.ReminderDay })
            .IsUnique()
            .HasFilter("\"Kind\" = 'Reminder' AND \"Status\" <> 'Failed'");
    }
}
