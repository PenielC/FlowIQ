using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Application.Invoicing.Commands.CreateInvoice;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Invoicing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Invoicing;

public class CreateInvoiceCommandHandlerTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IExchangeRateProvider> _exchangeRateProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateInvoiceCommandHandler CreateHandler() => new(
        _invoiceRepository.Object,
        _companyRepository.Object,
        _exchangeRateProvider.Object,
        _unitOfWork.Object,
        NullLogger<CreateInvoiceCommandHandler>.Instance);

    private void SetUpCompany(Company company) =>
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

    [Fact]
    public async Task Handle_WithValidCommand_CreatesInvoiceAsSent()
    {
        var company = new Company("Acme Trading");
        SetUpCompany(company);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(company.Id, "Tech Solutions Ltd", 1500m, issueDate, issueDate.AddDays(14), "USD", null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.CustomerName.Should().Be("Tech Solutions Ltd");
        result.Amount.Should().Be(1500m);
        result.Status.Should().Be(InvoiceStatus.Sent);

        _invoiceRepository.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDueDateBeforeIssueDate_ThrowsDomainException()
    {
        var company = new Company("Acme Trading");
        SetUpCompany(company);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(company.Id, "Bad Invoice", 500m, issueDate, issueDate.AddDays(-1), "USD", null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<FlowIQ.Domain.Exceptions.DomainException>();
    }

    [Fact]
    public async Task Handle_WithSameCurrencyAsCompany_UsesRateOfOneWithoutCallingProvider()
    {
        var company = new Company("Acme Trading"); // USD
        SetUpCompany(company);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(company.Id, "Local Co", 100m, issueDate, issueDate.AddDays(14), "USD", null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Currency.Should().Be("USD");
        result.AmountInReportingCurrency.Should().Be(100m);
        _exchangeRateProvider.Verify(
            p => p.GetRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDifferentCurrencyAndNoManualRate_FetchesLiveRateAndComputesConvertedAmount()
    {
        var company = new Company("Acme Trading"); // USD
        SetUpCompany(company);
        _exchangeRateProvider
            .Setup(p => p.GetRateAsync("EUR", "USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.1m);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(company.Id, "Export Co", 100m, issueDate, issueDate.AddDays(14), "EUR", null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AmountInReportingCurrency.Should().Be(110m);
        _exchangeRateProvider.Verify(p => p.GetRateAsync("EUR", "USD", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentCurrencyAndManualRate_UsesManualRateWithoutCallingProvider()
    {
        var company = new Company("Acme Trading"); // USD
        SetUpCompany(company);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(company.Id, "Export Co", 100m, issueDate, issueDate.AddDays(14), "EUR", 1.2m);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AmountInReportingCurrency.Should().Be(120m);
        _exchangeRateProvider.Verify(
            p => p.GetRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDifferentCurrencyAndProviderUnavailable_FallsBackToRateOfOneAndStillSaves()
    {
        var company = new Company("Acme Trading"); // USD
        SetUpCompany(company);
        _exchangeRateProvider
            .Setup(p => p.GetRateAsync("EUR", "USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(company.Id, "Export Co", 100m, issueDate, issueDate.AddDays(14), "EUR", null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AmountInReportingCurrency.Should().Be(100m);
        _invoiceRepository.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
