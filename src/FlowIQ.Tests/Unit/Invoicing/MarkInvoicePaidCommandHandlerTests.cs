using AwesomeAssertions;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Application.Invoicing.Commands.MarkInvoicePaid;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.Invoicing;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Invoicing;

public class MarkInvoicePaidCommandHandlerTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private static readonly DateTime PaidAt = new(2026, 9, 29, 10, 0, 0, DateTimeKind.Utc);

    public MarkInvoicePaidCommandHandlerTests() => _clock.Setup(c => c.UtcNow).Returns(PaidAt);

    private MarkInvoicePaidCommandHandler CreateHandler() =>
        new(_invoiceRepository.Object, _transactionRepository.Object, _clock.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_RecordsThePaymentAsLinkedIncomeInTheInvoicesCurrencyAndRate()
    {
        var companyId = Guid.NewGuid();
        var invoice = new Invoice(
            companyId, "Global Media", [("Consulting", 1000m)], DateTime.UtcNow, DateTime.UtcNow.AddDays(10),
            InvoiceStatus.Sent, "ZAR", 0.055m, null);
        _invoiceRepository.Setup(r => r.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
        Transaction? income = null;
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => income = t);

        await CreateHandler().Handle(new MarkInvoicePaidCommand(companyId, invoice.Id), CancellationToken.None);

        income.Should().NotBeNull();
        income!.InvoiceId.Should().Be(invoice.Id);
        income.CompanyId.Should().Be(companyId);
        income.Amount.Should().Be(1000m);
        income.Currency.Should().Be("ZAR");
        income.AmountInReportingCurrency.Should().Be(55m);
        income.Category.Should().Be(TransactionCategory.Sales);
        income.Status.Should().Be(TransactionStatus.Completed);
        income.TransactionDateUtc.Should().Be(PaidAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAlreadyPaidInvoice_RecordsNoSecondIncome()
    {
        var companyId = Guid.NewGuid();
        var invoice = new Invoice(
            companyId, "Global Media", [("Consulting", 100m)], DateTime.UtcNow, DateTime.UtcNow.AddDays(10),
            InvoiceStatus.Paid, "USD", 1m, null);
        _invoiceRepository.Setup(r => r.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

        var act = () => CreateHandler().Handle(new MarkInvoicePaidCommand(companyId, invoice.Id), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithOwnedUnpaidInvoice_MarksItPaid()
    {
        var companyId = Guid.NewGuid();
        var invoice = new Invoice(
            companyId, "Global Media", [("Services rendered", 2800m)], DateTime.UtcNow, DateTime.UtcNow.AddDays(10),
            InvoiceStatus.Sent, "USD", 1m, null);

        _invoiceRepository.Setup(r => r.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

        var result = await CreateHandler().Handle(new MarkInvoicePaidCommand(companyId, invoice.Id), CancellationToken.None);

        result.Status.Should().Be(InvoiceStatus.Paid);
        _invoiceRepository.Verify(r => r.Update(invoice), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvoiceBelongingToAnotherCompany_ThrowsDomainException()
    {
        var invoice = new Invoice(
            Guid.NewGuid(), "Someone Else's Customer", [("Services rendered", 100m)], DateTime.UtcNow, DateTime.UtcNow.AddDays(5),
            InvoiceStatus.Sent, "USD", 1m, null);
        _invoiceRepository.Setup(r => r.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

        var act = () => CreateHandler().Handle(new MarkInvoicePaidCommand(Guid.NewGuid(), invoice.Id), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _invoiceRepository.Verify(r => r.Update(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownInvoiceId_ThrowsDomainException()
    {
        _invoiceRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Invoice?)null);

        var act = () => CreateHandler().Handle(new MarkInvoicePaidCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
