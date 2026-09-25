using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.BankTransactions.TransactionImport.Commands.ConfirmTransactionImport;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.BankTransactions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.BankTransactions;

public class ConfirmTransactionImportCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ConfirmTransactionImportCommandHandler CreateHandler() => new(_transactionRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_BulkCreatesAllRows_WithOneSaveChangesCall()
    {
        var companyId = Guid.NewGuid();
        var rows = new List<ImportRowInput>
        {
            new(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), "Office supplies", -50m, TransactionCategory.OperatingExpense),
            new(new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), "Client payment", 500m, TransactionCategory.Sales),
            new(new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc), "Wholesale order", 1200m, TransactionCategory.Sales),
        };
        var command = new ConfirmTransactionImportCommand(companyId, rows, "USD", 1m);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(3);
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AppliesThePassedThroughRateUniformly_ComputingReportingAmountFromIt()
    {
        var companyId = Guid.NewGuid();
        Transaction? captured = null;
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        var rows = new List<ImportRowInput>
        {
            new(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), "Export sale", 100m, TransactionCategory.Sales),
        };
        var command = new ConfirmTransactionImportCommand(companyId, rows, "EUR", 1.1m);

        await CreateHandler().Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Currency.Should().Be("EUR");
        captured.AmountInReportingCurrency.Should().Be(110m);
        captured.ExchangeRateToReportingCurrency.Should().Be(1.1m);
    }
}
