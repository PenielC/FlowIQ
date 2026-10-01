using FlowIQ.Application.Analytics;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.Invoicing;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Analytics;

public class UsageStore(ApplicationDbContext db) : IUsageStore
{
    public async Task RecordAsync(Guid companyId, Guid userId, string feature, DateTime atUtc, CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(atUtc);
        // One statement, safe when two requests land at once: insert today's row or add one to it.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "UsageDays" ("Id", "CompanyId", "UserId", "Feature", "Day", "Count", "LastAtUtc")
            VALUES ({Guid.NewGuid()}, {companyId}, {userId}, {feature}, {day}, 1, {atUtc})
            ON CONFLICT ("UserId", "Feature", "Day")
            DO UPDATE SET "Count" = "UsageDays"."Count" + 1, "LastAtUtc" = EXCLUDED."LastAtUtc"
            """, cancellationToken);
    }

    public Task<List<UsageRow>> RowsSinceAsync(DateOnly fromDay, Guid? companyId, CancellationToken cancellationToken = default) =>
        db.UsageDays.AsNoTracking()
            .Where(u => u.Day >= fromDay && (companyId == null || u.CompanyId == companyId))
            .Select(u => new UsageRow(u.CompanyId, u.UserId, u.Feature, u.Day, u.Count, u.LastAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, DateTime>> LastActiveByCompanyAsync(CancellationToken cancellationToken = default) =>
        await db.UsageDays.AsNoTracking()
            .GroupBy(u => u.CompanyId)
            .Select(g => new { g.Key, Last = g.Max(u => u.LastAtUtc) })
            .ToDictionaryAsync(x => x.Key, x => x.Last, cancellationToken);

    public async Task<Dictionary<string, DateTime>> LastUseByFeatureAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await db.UsageDays.AsNoTracking()
            .Where(u => u.CompanyId == companyId)
            .GroupBy(u => u.Feature)
            .Select(g => new { g.Key, Last = g.Max(u => u.LastAtUtc) })
            .ToDictionaryAsync(x => x.Key, x => x.Last, cancellationToken);

    public async Task<IReadOnlyList<FeatureSwitch>> FeatureSwitchesAsync(CancellationToken cancellationToken = default)
    {
        async Task<int> Distinct(IQueryable<Guid> companyIds) => await companyIds.Distinct().CountAsync(cancellationToken);

        return
        [
            new("Payment reminders switched on", await db.Companies.CountAsync(c => c.ReminderEnabled, cancellationToken)),
            new("Emailed an invoice", await Distinct(db.InvoiceEmails.Where(e => e.Kind == InvoiceEmailKind.Invoice && e.Status == InvoiceEmailStatus.Sent).Select(e => e.CompanyId))),
            new("Planned personal withdrawals", await Distinct(db.PlannedOwnerDraws.Select(d => d.CompanyId))),
            new("Answered the forecast setup", await db.Companies.CountAsync(c => c.ForecastSetupCompletedAtUtc != null, cancellationToken)),
            new("Recorded owner drawings", await Distinct(db.Transactions.Where(t => t.Category == TransactionCategory.OwnerDrawings).Select(t => t.CompanyId))),
            new("Created an invoice", await Distinct(db.Invoices.Select(i => i.CompanyId))),
            new("Added customers", await Distinct(db.Customers.Select(c => c.CompanyId))),
            new("Uploaded a logo", await db.Companies.CountAsync(c => c.LogoData != null, cancellationToken)),
            new("Invited a team member", await db.Users.GroupBy(u => u.CompanyId).CountAsync(g => g.Count() > 1, cancellationToken)),
            new("Imported a bank statement (tracked from Oct 2026)", await Distinct(db.UsageDays.Where(u => u.Feature == "statement.imported").Select(u => u.CompanyId))),
        ];
    }
}
