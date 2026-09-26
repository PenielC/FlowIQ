using AwesomeAssertions;
using FlowIQ.Application.Admin.Commands.UpdateCompany;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Admin;

public class UpdateCompanyCommandHandlerTests
{
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateCompanyCommandHandler CreateHandler() => new(_companyRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_RenamesAndChangesCurrency()
    {
        var company = new Company("Old Name");
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

        await CreateHandler().Handle(new UpdateCompanyCommand(company.Id, "New Name", "ZAR"), CancellationToken.None);

        company.Name.Should().Be("New Name");
        company.Currency.Should().Be("ZAR");
        _companyRepository.Verify(r => r.Update(company), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownCompany_ThrowsDomainException()
    {
        _companyRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Company?)null);

        var act = () => CreateHandler().Handle(new UpdateCompanyCommand(Guid.NewGuid(), "Name", "USD"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
