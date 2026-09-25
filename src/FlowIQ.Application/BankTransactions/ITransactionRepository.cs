using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.BankTransactions;

namespace FlowIQ.Application.BankTransactions;

public record TransactionMonthTotals(decimal Income, decimal Expenses);

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<(List<Transaction> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<List<Transaction>> GetRecentByCompanyAsync(
        Guid companyId,
        int count,
        CancellationToken cancellationToken = default);

    Task<decimal> GetBalanceAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<TransactionMonthTotals> GetTotalsForMonthAsync(
        Guid companyId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>Sum of all transaction amounts strictly before <paramref name="dateUtc"/> — a baseline balance to build a running total from.</summary>
    Task<decimal> GetBalanceBeforeDateAsync(Guid companyId, DateTime dateUtc, CancellationToken cancellationToken = default);

    Task<List<Transaction>> GetInDateRangeAsync(
        Guid companyId,
        DateTime startUtcInclusive,
        DateTime endUtcExclusive,
        CancellationToken cancellationToken = default);
}
