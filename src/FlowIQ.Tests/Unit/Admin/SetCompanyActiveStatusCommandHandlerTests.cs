using AwesomeAssertions;
using FlowIQ.Application.Admin.Commands.SetCompanyActiveStatus;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Admin;

public class SetCompanyActiveStatusCommandHandlerTests
{
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SetCompanyActiveStatusCommandHandler CreateHandler() => new(_companyRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithIsActiveFalse_DeactivatesCompany()
    {
        var company = new Company("Acme Trading Co.");
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

        await CreateHandler().Handle(new SetCompanyActiveStatusCommand(company.Id, false), CancellationToken.None);

        company.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsActiveTrue_ReactivatesCompany()
    {
        var company = new Company("Acme Trading Co.");
        company.Deactivate();
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

        await CreateHandler().Handle(new SetCompanyActiveStatusCommand(company.Id, true), CancellationToken.None);

        company.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithUnknownCompany_ThrowsDomainException()
    {
        _companyRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Company?)null);

        var act = () => CreateHandler().Handle(new SetCompanyActiveStatusCommand(Guid.NewGuid(), true), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
