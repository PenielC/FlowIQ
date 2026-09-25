using System.Text;
using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.BankTransactions.TransactionImport;
using FlowIQ.Application.BankTransactions.TransactionImport.Queries.PreviewTransactionImport;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CompaniesAndTeams;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.BankTransactions;

public class PreviewTransactionImportQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IExchangeRateProvider> _exchangeRateProvider = new();

    private static readonly CsvColumnMapping Mapping = new(
        DateColumnIndex: 0, DescriptionColumnIndex: 1, AmountColumnIndex: 2, DateFormat: "yyyy-MM-dd", HasHeaderRow: true);

    private PreviewTransactionImportQueryHandler CreateHandler() => new(
        _transactionRepository.Object, _companyRepository.Object, _exchangeRateProvider.Object, NullLogger<PreviewTransactionImportQueryHandler>.Instance);

    private void SetUpCompany(Company company) =>
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

    private static byte[] Csv(string content) => Encoding.UTF8.GetBytes(content);

    [Fact]
    public async Task Handle_ParsesRowsAndAppliesCategorySuggestion()
    {
        var company = new Company("Acme Trading"); // USD
        SetUpCompany(company);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(company.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var csv = Csv("Date,Description,Amount\n2026-09-01,Office supplies,-50.00\n2026-09-02,Client payment,500.00\n");
        var query = new PreviewTransactionImportQuery(company.Id, csv, Mapping, "USD", null);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Rows.Should().HaveCount(2);
        var first = result.Rows.ElementAt(0);
        first.Amount.Should().Be(-50.00m);
        first.Description.Should().Be("Office supplies");
        first.SuggestedCategory.Should().Be(TransactionCategory.OperatingExpense);
        first.ParseError.Should().BeNull();

        var second = result.Rows.ElementAt(1);
        second.Amount.Should().Be(500.00m);
        second.SuggestedCategory.Should().Be(TransactionCategory.Sales);

        result.ImportableCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_RowWithUnparseableDate_SetsParseErrorAndExcludesFromImportableCount()
    {
        var company = new Company("Acme Trading");
        SetUpCompany(company);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(company.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var csv = Csv("Date,Description,Amount\nnot-a-date,Mystery row,100.00\n2026-09-02,Client payment,500.00\n");
        var query = new PreviewTransactionImportQuery(company.Id, csv, Mapping, "USD", null);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        var badRow = result.Rows.ElementAt(0);
        badRow.ParseError.Should().NotBeNull();
        badRow.TransactionDateUtc.Should().BeNull();
        result.ImportableCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RowMatchingExistingTransaction_IsFlaggedAsDuplicate()
    {
        var company = new Company("Acme Trading");
        SetUpCompany(company);
        var existing = new Transaction(
            company.Id, "Client payment", TransactionCategory.Sales, 500.00m, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
            TransactionStatus.Completed, "USD", 500.00m, 1m);
        _transactionRepository
            .Setup(r => r.GetInDateRangeAsync(company.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        var csv = Csv("Date,Description,Amount\n2026-09-02,Client payment,500.00\n");
        var query = new PreviewTransactionImportQuery(company.Id, csv, Mapping, "USD", null);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Rows.Single().IsDuplicate.Should().BeTrue();
        result.ImportableCount.Should().Be(0);
    }
}
