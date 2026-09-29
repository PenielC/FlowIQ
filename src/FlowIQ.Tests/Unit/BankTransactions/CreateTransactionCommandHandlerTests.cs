using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.BankTransactions.Commands.CreateTransaction;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.BankTransactions;

public class CreateTransactionCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IExchangeRateProvider> _exchangeRateProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateTransactionCommandHandler CreateHandler() => new(
        _transactionRepository.Object,
        _companyRepository.Object,
        _exchangeRateProvider.Object,
        _unitOfWork.Object,
        NullLogger<CreateTransactionCommandHandler>.Instance);

    private void SetUpCompany(Company company) =>
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

    [Fact]
    public async Task Handle_WithValidCommand_CreatesTransactionAndSaves()
    {
        var company = new Company("Acme Trading");
        SetUpCompany(company);
        var command = new CreateTransactionCommand(
            company.Id, "Stripe Payment", TransactionCategory.Sales, 2400m, DateTime.UtcNow, TransactionStatus.Completed, "USD", null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Description.Should().Be("Stripe Payment");
        result.Amount.Should().Be(2400m);
        result.Category.Should().Be(TransactionCategory.Sales);

        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithZeroAmount_ThrowsDomainException()
    {
        var company = new Company("Acme Trading");
        SetUpCompany(company);
        var command = new CreateTransactionCommand(
            company.Id, "Bad transaction", TransactionCategory.Other, 0m, DateTime.UtcNow, TransactionStatus.Completed, "USD", null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithSameCurrencyAsCompany_UsesRateOfOneWithoutCallingProvider()
    {
        var company = new Company("Acme Trading"); // defaults to USD
        SetUpCompany(company);
        var command = new CreateTransactionCommand(
            company.Id, "Local sale", TransactionCategory.Sales, 100m, DateTime.UtcNow, TransactionStatus.Completed, "USD", null);

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
        var command = new CreateTransactionCommand(
            company.Id, "Export sale", TransactionCategory.Sales, 100m, DateTime.UtcNow, TransactionStatus.Completed, "EUR", null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AmountInReportingCurrency.Should().Be(110m);
        _exchangeRateProvider.Verify(p => p.GetRateAsync("EUR", "USD", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentCurrencyAndManualRate_UsesManualRateWithoutCallingProvider()
    {
        var company = new Company("Acme Trading"); // USD
        SetUpCompany(company);
        var command = new CreateTransactionCommand(
            company.Id, "Export sale", TransactionCategory.Sales, 100m, DateTime.UtcNow, TransactionStatus.Completed, "EUR", 1.2m);

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
        var command = new CreateTransactionCommand(
            company.Id, "Export sale", TransactionCategory.Sales, 100m, DateTime.UtcNow, TransactionStatus.Completed, "EUR", null);

        // It used to save at a silent 1:1 rate, which is how R89 became $89.
        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>().WithMessage("*enter the exchange rate*");
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
