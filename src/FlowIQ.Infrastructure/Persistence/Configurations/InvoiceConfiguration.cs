using FlowIQ.Domain.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowIQ.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.AmountInReportingCurrency).HasPrecision(18, 2);
        builder.Property(i => i.ExchangeRateToReportingCurrency).HasPrecision(18, 6);
        builder.Property(i => i.Notes).HasMaxLength(2000);

        builder.HasMany(i => i.LineItems)
            .WithOne()
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.LineItems).AutoInclude();

        builder.HasIndex(i => new { i.CompanyId, i.DueDateUtc });
    }
}
