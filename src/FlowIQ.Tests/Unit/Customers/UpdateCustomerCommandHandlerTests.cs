using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Customers;
using FlowIQ.Application.Customers.Commands.UpdateCustomer;
using FlowIQ.Domain.Customers;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Customers;

public class UpdateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateCustomerCommandHandler CreateHandler() => new(_customerRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithOwnedCustomer_UpdatesDetails()
    {
        var companyId = Guid.NewGuid();
        var customer = new Customer(companyId, "Old Name", null, null, null);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var command = new UpdateCustomerCommand(companyId, customer.Id, "New Name", "new@example.com", "+1", "note");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Name.Should().Be("New Name");
        result.Email.Should().Be("new@example.com");
        _customerRepository.Verify(r => r.Update(customer), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomerBelongingToAnotherCompany_ThrowsDomainException()
    {
        var customer = new Customer(Guid.NewGuid(), "Someone Else's Customer", null, null, null);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var command = new UpdateCustomerCommand(Guid.NewGuid(), customer.Id, "New Name", null, null, null);
        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _customerRepository.Verify(r => r.Update(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownCustomerId_ThrowsDomainException()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var command = new UpdateCustomerCommand(Guid.NewGuid(), Guid.NewGuid(), "New Name", null, null, null);
        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
