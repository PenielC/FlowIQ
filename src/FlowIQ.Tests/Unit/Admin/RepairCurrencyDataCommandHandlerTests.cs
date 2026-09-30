using AwesomeAssertions;
using FlowIQ.Application.Admin.Commands.RepairCurrencyData;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Invoicing;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Admin;

public class RepairCurrencyDataCommandHandlerTests
{
    private readonly Mock<IRepository<Company>> _companies = new();
    private readonly Mock<ITransactionRepository> _transactions = new();
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<IExchangeRateProvider> _rates = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Company _company = new("Braai Co");

    public RepairCurrencyDataCommandHandlerTests()
    {
        _company.SetCurrency("ZAR");
        _companies.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([_company]);
    }

    private RepairCurrencyDataCommandHandler Handler() => new(
        _companies.Object, _transactions.Object, _invoices.Object,
        new ReportingCurrencyConverter(_transactions.Object, _invoices.Object, FlowIQ.Tests.Unit.Common.NoPlannedDraws.Repository(), _rates.Object), _unitOfWork.Object);

    private void Records(List<Transaction> transactions, List<Invoice> invoices)
    {
        _transactions.Setup(r => r.ListByCompanyAsync(_company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(transactions);
        _invoices.Setup(r => r.ListByCompanyAsync(_company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoices);
    }

    [Fact]
    public async Task Repair_RestatesOnlyForeignRecordsStuckAtOneToOne()
    {
        // Relabelled when the company switched USD -> ZAR: a $89 expense showing as R89.
        var relabelled = new Transaction(_company.Id, "Hosting", TransactionCategory.Utilities, -89m, DateTime.UtcNow,
            TransactionStatus.Completed, "USD", -89m, 1m);
        var fine = new Transaction(_company.Id, "Rent", TransactionCategory.RentAndLease, -5000m, DateTime.UtcNow,
            TransactionStatus.Completed, "ZAR", -5000m, 1m);
        Records([relabelled, fine], []);
        _rates.Setup(p => p.GetRateAsync("USD", "ZAR", It.IsAny<CancellationToken>())).ReturnsAsync(17.5m);

        var result = await Handler().Handle(new RepairCurrencyDataCommand(false, false), CancellationToken.None);

        relabelled.AmountInReportingCurrency.Should().Be(-1557.5m);
        fine.AmountInReportingCurrency.Should().Be(-5000m);
        result.TransactionsRestated.Should().Be(1);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Repair_BackfillsIncomeForPaidInvoicesOnlyWhenAsked_AndNeverTwice()
    {
        var paid = new Invoice(_company.Id, "Client", [("Work", 300m)], DateTime.UtcNow, DateTime.UtcNow, InvoiceStatus.Paid, "ZAR", 1m, null);
        var alreadyRecorded = new Invoice(_company.Id, "Other", [("Work", 50m)], DateTime.UtcNow, DateTime.UtcNow, InvoiceStatus.Paid, "ZAR", 1m, null);
        var unpaid = new Invoice(_company.Id, "Later", [("Work", 70m)], DateTime.UtcNow, DateTime.UtcNow, InvoiceStatus.Sent, "ZAR", 1m, null);
        Records([Transaction.ForPaidInvoice(alreadyRecorded, DateTime.UtcNow)], [paid, alreadyRecorded, unpaid]);
        var added = new List<Transaction>();
        _transactions.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added.Add(t));

        var without = await Handler().Handle(new RepairCurrencyDataCommand(false, false), CancellationToken.None);
        var with = await Handler().Handle(new RepairCurrencyDataCommand(false, true), CancellationToken.None);

        without.IncomeRecorded.Should().Be(0);
        with.IncomeRecorded.Should().Be(1);
        added.Should().ContainSingle().Which.InvoiceId.Should().Be(paid.Id);
    }

    [Fact]
    public async Task DryRun_ReportsButSavesAndAddsNothing()
    {
        var paid = new Invoice(_company.Id, "Client", [("Work", 300m)], DateTime.UtcNow, DateTime.UtcNow, InvoiceStatus.Paid, "ZAR", 1m, null);
        Records([], [paid]);

        var result = await Handler().Handle(new RepairCurrencyDataCommand(true, true), CancellationToken.None);

        result.IncomeRecorded.Should().Be(1);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Repair_ReportsCompaniesItCouldNotFix()
    {
        var kes = new Transaction(_company.Id, "Supplier", TransactionCategory.OperatingExpense, -1000m, DateTime.UtcNow,
            TransactionStatus.Completed, "KES", -1000m, 1m);
        Records([kes], []);

        var result = await Handler().Handle(new RepairCurrencyDataCommand(false, false), CancellationToken.None);

        result.Companies.Should().ContainSingle().Which.Problem.Should().Contain("KES");
        kes.AmountInReportingCurrency.Should().Be(-1000m);
    }
}
