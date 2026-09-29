using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowIQ.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Currency).IsRequired().HasMaxLength(3);
        builder.Property(t => t.AmountInReportingCurrency).HasPrecision(18, 2);
        builder.Property(t => t.ExchangeRateToReportingCurrency).HasPrecision(18, 6);

        builder.HasIndex(t => new { t.CompanyId, t.TransactionDateUtc });

        // The income recorded for a paid invoice: at most one per invoice. Deleting the invoice keeps the income.
        builder.HasOne<Invoice>().WithMany().HasForeignKey(t => t.InvoiceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(t => t.InvoiceId).IsUnique().HasFilter("\"InvoiceId\" IS NOT NULL");
    }
}
