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

    [Fact]
    public async Task Handle_ComputesPercentChange_WhenPreviousMonthHasData()
    {
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var previous = now.AddMonths(-1);

        _transactionRepository.Setup(r => r.GetBalanceAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(48250m);
        _transactionRepository
            .Setup(r => r.GetTotalsForMonthAsync(companyId, now.Year, now.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionMonthTotals(3600m, 1800m));
        _transactionRepository
            .Setup(r => r.GetTotalsForMonthAsync(companyId, previous.Year, previous.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionMonthTotals(3000m, 2000m));
        _transactionRepository
            .Setup(r => r.GetRecentByCompanyAsync(companyId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetDashboardSummaryQuery(companyId), CancellationToken.None);

        result.CashBalance.Should().Be(48250m);
        result.MonthlyRevenue.Should().Be(3600m);
        result.RevenueChangePercent.Should().Be(20.0m); // (3600-3000)/3000*100
        result.MonthlyExpenses.Should().Be(1800m);
        result.ExpensesChangePercent.Should().Be(-10.0m); // (1800-2000)/2000*100
    }

    [Fact]
    public async Task Handle_ReturnsNullPercentChange_WhenPreviousMonthHasNoData()
    {
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var previous = now.AddMonths(-1);

        _transactionRepository.Setup(r => r.GetBalanceAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(500m);
        _transactionRepository
            .Setup(r => r.GetTotalsForMonthAsync(companyId, now.Year, now.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionMonthTotals(500m, 0m));
        _transactionRepository
            .Setup(r => r.GetTotalsForMonthAsync(companyId, previous.Year, previous.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionMonthTotals(0m, 0m));
        _transactionRepository
            .Setup(r => r.GetRecentByCompanyAsync(companyId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetDashboardSummaryQuery(companyId), CancellationToken.None);

        result.RevenueChangePercent.Should().BeNull();
        result.ExpensesChangePercent.Should().BeNull();
    }
}
