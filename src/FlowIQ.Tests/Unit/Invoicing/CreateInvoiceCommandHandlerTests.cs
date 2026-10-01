using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Customers;
using FlowIQ.Application.Invoicing;
using FlowIQ.Application.Invoicing.Commands.CreateInvoice;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Invoicing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Invoicing;

public class CreateInvoiceCommandHandlerTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IExchangeRateProvider> _exchangeRateProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateInvoiceCommandHandler CreateHandler() => new(
        _invoiceRepository.Object,
        _customerRepository.Object,
        _companyRepository.Object,
        _exchangeRateProvider.Object,
        _unitOfWork.Object,
        NullLogger<CreateInvoiceCommandHandler>.Instance);

    private void SetUpCompany(Company company) =>
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

    private static IReadOnlyCollection<InvoiceLineItemInput> SingleItem(decimal amount) =>
        [new InvoiceLineItemInput("Services rendered", amount)];

    [Fact]
    public async Task Handle_WithValidCommand_CreatesInvoiceAsSent()
    {
        var company = new Company("Acme Trading");
        SetUpCompany(company);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(
            company.Id, "Tech Solutions Ltd", SingleItem(1500m), issueDate, issueDate.AddDays(14), "USD", null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.CustomerName.Should().Be("Tech Solutions Ltd");
        result.Amount.Should().Be(1500m);
        result.Status.Should().Be(InvoiceStatus.Sent);

        _invoiceRepository.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleLineItems_SumsThemIntoTheTotal()
    {
        var company = new Company("Acme Trading");
        SetUpCompany(company);
        var issueDate = DateTime.UtcNow;
        var lineItems = new List<InvoiceLineItemInput>
        {
            new("Design work", 400m),
            new("Development", 900m),
            new("Hosting (1 month)", 25m),
        };
        var command = new CreateInvoiceCommand(
            company.Id, "Tech Solutions Ltd", lineItems, issueDate, issueDate.AddDays(14), "USD", null, "Net 14, thanks!");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Amount.Should().Be(1325m);
        result.LineItems.Should().HaveCount(3);
        result.Notes.Should().Be("Net 14, thanks!");
    }

    [Fact]
    public async Task Handle_WithDueDateBeforeIssueDate_ThrowsDomainException()
    {
        var company = new Company("Acme Trading");
        SetUpCompany(company);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(
            company.Id, "Bad Invoice", SingleItem(500m), issueDate, issueDate.AddDays(-1), "USD", null, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<FlowIQ.Domain.Exceptions.DomainException>();
    }

    [Fact]
    public async Task Handle_WithSameCurrencyAsCompany_UsesRateOfOneWithoutCallingProvider()
    {
        var company = new Company("Acme Trading"); // USD
        SetUpCompany(company);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(
            company.Id, "Local Co", SingleItem(100m), issueDate, issueDate.AddDays(14), "USD", null, null);

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
        var command = new CreateInvoiceCommand(
            company.Id, "Export Co", SingleItem(100m), issueDate, issueDate.AddDays(14), "EUR", null, null);

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
        var command = new CreateInvoiceCommand(
            company.Id, "Export Co", SingleItem(100m), issueDate, issueDate.AddDays(14), "EUR", 1.2m, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AmountInReportingCurrency.Should().Be(120m);
        _exchangeRateProvider.Verify(
            p => p.GetRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDifferentCurrencyAndNoRateAvailable_AsksForARateAndSavesNothing()
    {
        var company = new Company("Acme Trading"); // USD
        SetUpCompany(company);
        _exchangeRateProvider
            .Setup(p => p.GetRateAsync("EUR", "USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);
        var issueDate = DateTime.UtcNow;
        var command = new CreateInvoiceCommand(
            company.Id, "Export Co", SingleItem(100m), issueDate, issueDate.AddDays(14), "EUR", null, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>().WithMessage("*enter the exchange rate*");
        _invoiceRepository.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
