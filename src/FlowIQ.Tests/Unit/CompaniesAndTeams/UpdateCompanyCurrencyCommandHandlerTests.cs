using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Invoicing;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.CompaniesAndTeams;

public class UpdateCompanyCurrencyCommandHandlerTests
{
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
    private readonly Mock<IExchangeRateProvider> _rates = new();

    private ReportingCurrencyConverter Converter() => new(_transactionRepository.Object, _invoiceRepository.Object, _rates.Object);

    private void SetUpRecords(Guid companyId, List<Transaction> transactions, List<Invoice>? invoices = null)
    {
        _transactionRepository.Setup(r => r.ListByCompanyAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(transactions);
        _invoiceRepository.Setup(r => r.ListByCompanyAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(invoices ?? []);
    }

    private UpdateCompanyCurrencyCommandHandler CreateHandler() => new(_companyRepository.Object, Converter(), _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithOwnedCompany_SetsCurrency()
    {
        var company = new Company("Acme Trading");
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        SetUpRecords(company.Id, []);

        await CreateHandler().Handle(new UpdateCompanyCurrencyCommand(company.Id, "KES"), CancellationToken.None);

        company.Currency.Should().Be("KES");
        _companyRepository.Verify(r => r.Update(company), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RestatesRecordsInsteadOfRelabellingThem()
    {
        // The reported bug: an $89 expense read "R89" after switching USD -> ZAR.
        var company = new Company("Acme Trading"); // USD
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        var usdExpense = new Transaction(company.Id, "Hosting", TransactionCategory.Utilities, -89m,
            new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc), TransactionStatus.Completed, "USD", -89m, 1m);
        SetUpRecords(company.Id, [usdExpense]);
        _rates.Setup(p => p.GetHistoricalRatesAsync("USD", "ZAR", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, decimal> { [new DateOnly(2026, 9, 10)] = 17.5m });

        await CreateHandler().Handle(new UpdateCompanyCurrencyCommand(company.Id, "ZAR"), CancellationToken.None);

        company.Currency.Should().Be("ZAR");
        usdExpense.AmountInReportingCurrency.Should().Be(-1557.5m);
        usdExpense.ExchangeRateToReportingCurrency.Should().Be(17.5m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSameCurrency_ChangesNothing()
    {
        var company = new Company("Acme Trading"); // USD
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

        await CreateHandler().Handle(new UpdateCompanyCurrencyCommand(company.Id, "usd"), CancellationToken.None);

        _transactionRepository.Verify(r => r.ListByCompanyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenACurrencyHasNoRate_KeepsTheOldCurrencyAndSavesNothing()
    {
        var company = new Company("Acme Trading"); // USD
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        SetUpRecords(company.Id, [new Transaction(company.Id, "Sale", TransactionCategory.Sales, 500m, DateTime.UtcNow,
            TransactionStatus.Completed, "USD", 500m, 1m)]);

        var act = () => CreateHandler().Handle(new UpdateCompanyCurrencyCommand(company.Id, "KES"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>().WithMessage("*USD*KES*");
        company.Currency.Should().Be("USD");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownCompanyId_ThrowsDomainException()
    {
        _companyRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Company?)null);

        var act = () => CreateHandler().Handle(new UpdateCompanyCurrencyCommand(Guid.NewGuid(), "KES"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _companyRepository.Verify(r => r.Update(It.IsAny<Company>()), Times.Never);
    }
}
