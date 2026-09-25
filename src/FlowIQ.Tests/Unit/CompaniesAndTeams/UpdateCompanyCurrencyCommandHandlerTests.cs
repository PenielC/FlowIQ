using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.CompaniesAndTeams;

public class UpdateCompanyCurrencyCommandHandlerTests
{
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateCompanyCurrencyCommandHandler CreateHandler() => new(_companyRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithOwnedCompany_SetsCurrency()
    {
        var company = new Company("Acme Trading");
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

        await CreateHandler().Handle(new UpdateCompanyCurrencyCommand(company.Id, "KES"), CancellationToken.None);

        company.Currency.Should().Be("KES");
        _companyRepository.Verify(r => r.Update(company), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownCompanyId_ThrowsDomainException()
    {
        _companyRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Company?)null);

        var act = () => CreateHandler().Handle(new UpdateCompanyCurrencyCommand(Guid.NewGuid(), "KES"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _companyRepository.Verify(r => r.Update(It.IsAny<Company>()), Times.Never);
    }
}
