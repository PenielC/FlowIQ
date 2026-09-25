using FlowIQ.Application.BankTransactions;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class TransactionRepository(ApplicationDbContext dbContext)
    : EfRepository<Transaction>(dbContext), ITransactionRepository
{
    public async Task<(List<Transaction> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Transactions.Where(t => t.CompanyId == companyId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.TransactionDateUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<Transaction>> GetRecentByCompanyAsync(Guid companyId, int count, CancellationToken cancellationToken = default) =>
        DbContext.Transactions
            .Where(t => t.CompanyId == companyId)
            .OrderByDescending(t => t.TransactionDateUtc)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<decimal> GetBalanceAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        DbContext.Transactions
            .Where(t => t.CompanyId == companyId)
            .SumAsync(t => t.AmountInReportingCurrency, cancellationToken);

    public async Task<TransactionMonthTotals> GetTotalsForMonthAsync(
        Guid companyId, int year, int month, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var transactions = await DbContext.Transactions
            .Where(t => t.CompanyId == companyId && t.TransactionDateUtc >= monthStart && t.TransactionDateUtc < monthEnd)
            .Select(t => t.AmountInReportingCurrency)
            .ToListAsync(cancellationToken);

        var income = transactions.Where(a => a > 0).Sum();
        var expenses = -transactions.Where(a => a < 0).Sum();

        return new TransactionMonthTotals(income, expenses);
    }

    public Task<decimal> GetBalanceBeforeDateAsync(Guid companyId, DateTime dateUtc, CancellationToken cancellationToken = default) =>
        DbContext.Transactions
            .Where(t => t.CompanyId == companyId && t.TransactionDateUtc < dateUtc)
            .SumAsync(t => t.AmountInReportingCurrency, cancellationToken);

    public Task<List<Transaction>> GetInDateRangeAsync(
        Guid companyId, DateTime startUtcInclusive, DateTime endUtcExclusive, CancellationToken cancellationToken = default) =>
        DbContext.Transactions
            .Where(t => t.CompanyId == companyId && t.TransactionDateUtc >= startUtcInclusive && t.TransactionDateUtc < endUtcExclusive)
            .ToListAsync(cancellationToken);
}
