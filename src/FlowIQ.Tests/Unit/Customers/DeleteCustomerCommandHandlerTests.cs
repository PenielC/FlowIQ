using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Customers;
using FlowIQ.Application.Customers.Commands.DeleteCustomer;
using FlowIQ.Domain.Customers;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Customers;

public class DeleteCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteCustomerCommandHandler CreateHandler() => new(_customerRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithOwnedCustomer_RemovesIt()
    {
        var companyId = Guid.NewGuid();
        var customer = new Customer(companyId, "Acme Retailers", null, null, null);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        await CreateHandler().Handle(new DeleteCustomerCommand(companyId, customer.Id), CancellationToken.None);

        _customerRepository.Verify(r => r.Remove(customer), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomerBelongingToAnotherCompany_ThrowsDomainException()
    {
        var customer = new Customer(Guid.NewGuid(), "Someone Else's Customer", null, null, null);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var act = () => CreateHandler().Handle(new DeleteCustomerCommand(Guid.NewGuid(), customer.Id), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _customerRepository.Verify(r => r.Remove(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownCustomerId_ThrowsDomainException()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var act = () => CreateHandler().Handle(new DeleteCustomerCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
