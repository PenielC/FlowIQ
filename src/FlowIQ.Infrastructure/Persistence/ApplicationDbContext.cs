using System.Reflection;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CashFlowForecasting;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Common;
using FlowIQ.Domain.Customers;
using FlowIQ.Domain.Invoicing;
using FlowIQ.Domain.StripeSubscriptions;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDateTimeProvider dateTimeProvider) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceEmail> InvoiceEmails => Set<InvoiceEmail>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PlannedOwnerDraw> PlannedOwnerDraws => Set<PlannedOwnerDraw>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Domain events are an in-memory-only concept, never persisted.
        modelBuilder.Ignore<BaseDomainEvent>();

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAtUtc = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
