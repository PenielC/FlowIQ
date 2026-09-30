using AwesomeAssertions;
using FlowIQ.Application.Admin.Commands.UpdateCompany;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Invoicing;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Admin;

public class UpdateCompanyCommandHandlerTests
{
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
    private readonly Mock<IExchangeRateProvider> _rates = new();

    private ReportingCurrencyConverter Converter() => new(_transactionRepository.Object, _invoiceRepository.Object, FlowIQ.Tests.Unit.Common.NoPlannedDraws.Repository(), _rates.Object);

    private void SetUpRecords(Guid companyId, List<Transaction> transactions, List<Invoice>? invoices = null)
    {
        _transactionRepository.Setup(r => r.ListByCompanyAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(transactions);
        _invoiceRepository.Setup(r => r.ListByCompanyAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(invoices ?? []);
    }

    private UpdateCompanyCommandHandler CreateHandler() => new(_companyRepository.Object, Converter(), _unitOfWork.Object);

    [Fact]
    public async Task Handle_RenamesAndChangesCurrency()
    {
        var company = new Company("Old Name");
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        SetUpRecords(company.Id, []);

        await CreateHandler().Handle(new UpdateCompanyCommand(company.Id, "New Name", "ZAR"), CancellationToken.None);

        company.Name.Should().Be("New Name");
        company.Currency.Should().Be("ZAR");
        _companyRepository.Verify(r => r.Update(company), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CurrencyChangeFromTheAdminConsole_AlsoRestatesRecords()
    {
        var company = new Company("Acme"); // USD
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        var sale = new Transaction(company.Id, "Sale", TransactionCategory.Sales, 100m, DateTime.UtcNow,
            TransactionStatus.Completed, "USD", 100m, 1m);
        SetUpRecords(company.Id, [sale]);
        _rates.Setup(p => p.GetRateAsync("USD", "ZAR", It.IsAny<CancellationToken>())).ReturnsAsync(18m);

        await CreateHandler().Handle(new UpdateCompanyCommand(company.Id, "Acme", "ZAR"), CancellationToken.None);

        sale.AmountInReportingCurrency.Should().Be(1800m);
    }

    [Fact]
    public async Task Handle_WithUnknownCompany_ThrowsDomainException()
    {
        _companyRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Company?)null);

        var act = () => CreateHandler().Handle(new UpdateCompanyCommand(Guid.NewGuid(), "Name", "USD"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
