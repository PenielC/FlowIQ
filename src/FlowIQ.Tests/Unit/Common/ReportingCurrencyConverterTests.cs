using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.Invoicing;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Common;

public class ReportingCurrencyConverterTests
{
    private readonly Mock<ITransactionRepository> _transactions = new();
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<IExchangeRateProvider> _rates = new();
    private readonly Guid _companyId = Guid.NewGuid();

    private ReportingCurrencyConverter Converter() => new(_transactions.Object, _invoices.Object, _rates.Object);

    private static DateTime Day(int d) => new(2026, 9, d, 12, 0, 0, DateTimeKind.Utc);

    private Transaction Tx(decimal amount, string currency, int day, decimal oldRate = 1m) =>
        new(_companyId, "x", TransactionCategory.Other, amount, Day(day), TransactionStatus.Completed, currency, amount * oldRate, oldRate);

    [Fact]
    public async Task Restate_UsesEachRecordsOwnDateAndTheLastPublishedRateForWeekends()
    {
        var friday = Tx(100m, "ZAR", 4);
        var sunday = Tx(100m, "ZAR", 6);
        _rates.Setup(p => p.GetHistoricalRatesAsync("ZAR", "USD", new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 6), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, decimal> { [new DateOnly(2026, 9, 3)] = 0.050m, [new DateOnly(2026, 9, 4)] = 0.060m });

        await Converter().RestateAsync([friday, sunday], [], "USD", null, CancellationToken.None);

        friday.AmountInReportingCurrency.Should().Be(6m);
        sunday.AmountInReportingCurrency.Should().Be(6m); // no rate on Sunday: Friday's applies
    }

    [Fact]
    public async Task Restate_AlwaysConvertsFromTheOriginalAmountNotTheOldReportingFigure()
    {
        // Saved in the old world at a silent 1:1: R89 stored as "$89".
        var zarExpense = Tx(-89m, "ZAR", 10);
        _rates.Setup(p => p.GetRateAsync("ZAR", "ZAR", It.IsAny<CancellationToken>())).ReturnsAsync(1m);

        await Converter().RestateAsync([zarExpense], [], "ZAR", null, CancellationToken.None);

        zarExpense.AmountInReportingCurrency.Should().Be(-89m);
        zarExpense.ExchangeRateToReportingCurrency.Should().Be(1m);
    }

    [Fact]
    public async Task Restate_FallsBackToTodaysRateWhenThereIsNoHistory_AndToManualRates()
    {
        var kes = Tx(10_000m, "KES", 10);
        var ngn = Tx(50_000m, "NGN", 12);
        _rates.Setup(p => p.GetRateAsync("KES", "USD", It.IsAny<CancellationToken>())).ReturnsAsync(0.0077m);

        await Converter().RestateAsync([kes, ngn], [], "USD", new Dictionary<string, decimal> { ["ngn"] = 0.00065m }, CancellationToken.None);

        kes.AmountInReportingCurrency.Should().Be(77m);
        ngn.AmountInReportingCurrency.Should().Be(32.5m);
    }

    [Fact]
    public async Task Restate_ChangesNothingWhenAnyCurrencyHasNoRate()
    {
        var zar = Tx(100m, "ZAR", 10, oldRate: 0.06m);
        var ghs = Tx(100m, "GHS", 10, oldRate: 1m);
        _rates.Setup(p => p.GetRateAsync("ZAR", "USD", It.IsAny<CancellationToken>())).ReturnsAsync(0.055m);

        var act = () => Converter().RestateAsync([zar, ghs], [], "USD", null, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*GHS*");
        zar.AmountInReportingCurrency.Should().Be(6m); // untouched
    }

    [Fact]
    public async Task Restate_ConvertsInvoicesAtTheirIssueDate()
    {
        var invoice = new Invoice(_companyId, "Client", [("Work", 200m)], Day(2), Day(20), InvoiceStatus.Sent, "EUR", 1.1m, null);
        _rates.Setup(p => p.GetHistoricalRatesAsync("EUR", "ZAR", new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, decimal> { [new DateOnly(2026, 9, 2)] = 20m });

        await Converter().RestateAsync([], [invoice], "ZAR", null, CancellationToken.None);

        invoice.AmountInReportingCurrency.Should().Be(4000m);
    }

    [Fact]
    public async Task Preview_ListsEachForeignCurrencyWithItsCountsAndTodaysRate()
    {
        _transactions.Setup(r => r.ListByCompanyAsync(_companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Tx(1m, "USD", 1), Tx(2m, "USD", 2), Tx(3m, "KES", 3), Tx(4m, "ZAR", 4)]);
        _invoices.Setup(r => r.ListByCompanyAsync(_companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Invoice(_companyId, "C", [("W", 5m)], Day(5), Day(6), InvoiceStatus.Sent, "USD", 1m, null)]);
        _rates.Setup(p => p.GetRateAsync("USD", "ZAR", It.IsAny<CancellationToken>())).ReturnsAsync(18m);

        var preview = await Converter().PreviewAsync(_companyId, "USD", "ZAR", CancellationToken.None);

        preview.TransactionCount.Should().Be(4);
        preview.InvoiceCount.Should().Be(1);
        preview.Currencies.Should().BeEquivalentTo([
            new CurrencyRateNeed("KES", 1, 0, null),
            new CurrencyRateNeed("USD", 2, 1, 18m),
        ]);
    }
}
