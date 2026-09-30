using FlowIQ.Domain.CashFlowForecasting;
using FlowIQ.Domain.CompaniesAndTeams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowIQ.Infrastructure.Persistence.Configurations;

public class PlannedOwnerDrawConfiguration : IEntityTypeConfiguration<PlannedOwnerDraw>
{
    public void Configure(EntityTypeBuilder<PlannedOwnerDraw> builder)
    {
        builder.ToTable("PlannedOwnerDraws");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Amount).HasPrecision(18, 2);
        builder.Property(d => d.Currency).IsRequired().HasMaxLength(3);
        builder.Property(d => d.AmountInReportingCurrency).HasPrecision(18, 2);
        builder.Property(d => d.ExchangeRateToReportingCurrency).HasPrecision(18, 6);
        builder.Property(d => d.Frequency).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Months);
        builder.HasOne<Company>().WithMany().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(d => d.CompanyId);
    }
}
