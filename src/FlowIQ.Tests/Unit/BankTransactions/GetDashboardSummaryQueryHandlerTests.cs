using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.BankTransactions.Queries.GetDashboardSummary;
using FlowIQ.Domain.BankTransactions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.BankTransactions;

public class GetDashboardSummaryQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();

    private GetDashboardSummaryQueryHandler CreateHandler() => new(_transactionRepository.Object);

    private static Transaction MakeTx(DateTime dateUtc, decimal amount) =>
        new(Guid.NewGuid(), "Test", TransactionCategory.Other, amount, dateUtc, TransactionStatus.Completed, "USD", amount, 1m);

    [Fact]
    public async Task Handle_WithNoExplicitRange_UsesCurrentCalendarMonth()
    {
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var rangeStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEndExclusive = rangeStart.AddMonths(1);
        var previousStart = rangeStart.AddMonths(-1);

        _transactionRepository.Setup(r => r.GetBalanceBeforeDateAsync(companyId, rangeEndExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(48250m);
        _transactionRepository.Setup(r => r.GetInDateRangeAsync(companyId, rangeStart, rangeEndExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTx(rangeStart, 3600m), MakeTx(rangeStart, -1800m)]);
        _transactionRepository.Setup(r => r.GetInDateRangeAsync(companyId, previousStart, rangeStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTx(previousStart, 3000m), MakeTx(previousStart, -2000m)]);
        _transactionRepository.Setup(r => r.GetRecentByCompanyAsync(companyId, 4, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetDashboardSummaryQuery(companyId), CancellationToken.None);

        result.CashBalance.Should().Be(48250m);
        result.MonthlyRevenue.Should().Be(3600m);
        result.RevenueChangePercent.Should().Be(20.0m); // (3600-3000)/3000*100
        result.MonthlyExpenses.Should().Be(1800m);
        result.ExpensesChangePercent.Should().Be(-10.0m); // (1800-2000)/2000*100
    }

    [Fact]
    public async Task Handle_ReturnsNullPercentChange_WhenPreviousPeriodHasNoData()
    {
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var rangeStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEndExclusive = rangeStart.AddMonths(1);
        var previousStart = rangeStart.AddMonths(-1);

        _transactionRepository.Setup(r => r.GetBalanceBeforeDateAsync(companyId, rangeEndExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500m);
        _transactionRepository.Setup(r => r.GetInDateRangeAsync(companyId, rangeStart, rangeEndExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTx(rangeStart, 500m)]);
        _transactionRepository.Setup(r => r.GetInDateRangeAsync(companyId, previousStart, rangeStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _transactionRepository.Setup(r => r.GetRecentByCompanyAsync(companyId, 4, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetDashboardSummaryQuery(companyId), CancellationToken.None);

        result.RevenueChangePercent.Should().BeNull();
        result.ExpensesChangePercent.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithExplicitRange_UsesEqualLengthPreviousPeriodAndRangeEndBalance()
    {
        var companyId = Guid.NewGuid();
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc); // inclusive 10-day range (Mar 1-10)
        var rangeEndExclusive = end.AddDays(1); // Mar 11
        var previousStart = start.AddDays(-10); // Feb 19 — an equal-length (10-day) preceding period

        _transactionRepository.Setup(r => r.GetBalanceBeforeDateAsync(companyId, rangeEndExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(9000m);
        _transactionRepository.Setup(r => r.GetInDateRangeAsync(companyId, start, rangeEndExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTx(start, 1000m), MakeTx(start, -400m)]);
        _transactionRepository.Setup(r => r.GetInDateRangeAsync(companyId, previousStart, start, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTx(previousStart, 500m)]);
        _transactionRepository.Setup(r => r.GetRecentByCompanyAsync(companyId, 4, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetDashboardSummaryQuery(companyId, start, end), CancellationToken.None);

        result.CashBalance.Should().Be(9000m);
        result.MonthlyRevenue.Should().Be(1000m);
        result.MonthlyExpenses.Should().Be(400m);
        result.RevenueChangePercent.Should().Be(100.0m); // (1000-500)/500*100
        result.ExpensesChangePercent.Should().BeNull(); // previous period had no expenses
    }
}
