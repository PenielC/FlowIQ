using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Application.ReportsAndAnalytics.Queries.GetReportsSummary;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.Invoicing;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.ReportsAndAnalytics;

public class GetReportsSummaryQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime Today = new(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);

    private static Transaction MakeTransaction(Guid companyId, TransactionCategory category, decimal amount) =>
        new(companyId, "Test", category, amount, Today, TransactionStatus.Completed, "USD", amount, 1m);

    private GetReportsSummaryQueryHandler CreateHandler() =>
        new(_transactionRepository.Object, _invoiceRepository.Object, _dateTimeProvider.Object);

    private void SetupDefaults(Guid companyId, List<Transaction>? transactions = null)
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(Today);
        _transactionRepository
            .Setup(r => r.GetTotalsForMonthAsync(companyId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionMonthTotals(0m, 0m));
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions ?? []);
        _invoiceRepository
            .Setup(r => r.GetStatusBreakdownAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task Handle_ReturnsOneTrendPointPerRequestedMonth_OldestFirst()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);

        var result = await CreateHandler().Handle(new GetReportsSummaryQuery(companyId, MonthsBack: 6), CancellationToken.None);

        result.MonthlyTrend.Should().HaveCount(6);
        result.MonthlyTrend.Last().Year.Should().Be(2026);
        result.MonthlyTrend.Last().Month.Should().Be(9);
        result.MonthlyTrend.First().Month.Should().Be(4); // 5 months before September
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(100, 24)]
    public async Task Handle_ClampsMonthsBackToValidRange(int requested, int expectedCount)
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);

        var result = await CreateHandler().Handle(new GetReportsSummaryQuery(companyId, MonthsBack: requested), CancellationToken.None);

        result.MonthlyTrend.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task Handle_CategoryBreakdown_OnlyIncludesExpenses_SummedAndSortedDescending()
    {
        var companyId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            MakeTransaction(companyId, TransactionCategory.Sales, 500m), // income — excluded
            MakeTransaction(companyId, TransactionCategory.OperatingExpense, -100m),
            MakeTransaction(companyId, TransactionCategory.OperatingExpense, -50m),
            MakeTransaction(companyId, TransactionCategory.Utilities, -30m),
        };
        SetupDefaults(companyId, transactions);

        var result = await CreateHandler().Handle(new GetReportsSummaryQuery(companyId), CancellationToken.None);

        result.CategoryBreakdown.Should().HaveCount(2);
        result.CategoryBreakdown.Should().NotContain(c => c.Category == TransactionCategory.Sales);
        result.CategoryBreakdown.First().Category.Should().Be(TransactionCategory.OperatingExpense);
        result.CategoryBreakdown.First().Total.Should().Be(150m);
        result.CategoryBreakdown.Last().Category.Should().Be(TransactionCategory.Utilities);
        result.CategoryBreakdown.Last().Total.Should().Be(30m);
    }

    [Fact]
    public async Task Handle_PassesThroughInvoiceStatusBreakdownFromRepository()
    {
        var companyId = Guid.NewGuid();
        SetupDefaults(companyId);
        _invoiceRepository
            .Setup(r => r.GetStatusBreakdownAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new InvoiceStatusTotal(InvoiceStatus.Paid, 3, 900m), new InvoiceStatusTotal(InvoiceStatus.Sent, 2, 400m)]);

        var result = await CreateHandler().Handle(new GetReportsSummaryQuery(companyId), CancellationToken.None);

        result.InvoiceStatusBreakdown.Should().HaveCount(2);
        result.InvoiceStatusBreakdown.Should().ContainSingle(s => s.Status == InvoiceStatus.Paid && s.Count == 3 && s.TotalAmount == 900m);
    }
}
