using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Customers;
using FlowIQ.Application.Customers.Commands.CreateCustomer;
using FlowIQ.Domain.Customers;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Customers;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateCustomerCommandHandler CreateHandler() => new(_customerRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithValidCommand_CreatesCustomer()
    {
        var companyId = Guid.NewGuid();
        var command = new CreateCustomerCommand(companyId, "Acme Retailers", "hello@acme.test", "+254700000000", "Prefers email.");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Name.Should().Be("Acme Retailers");
        result.Email.Should().Be("hello@acme.test");
        result.Phone.Should().Be("+254700000000");
        result.Notes.Should().Be("Prefers email.");

        _customerRepository.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyName_ThrowsDomainException()
    {
        var command = new CreateCustomerCommand(Guid.NewGuid(), " ", null, null, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<FlowIQ.Domain.Exceptions.DomainException>();
    }
}
