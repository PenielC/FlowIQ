using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.CompaniesAndTeams.Commands.UploadCompanyLogo;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.CompaniesAndTeams;

public class UploadCompanyLogoCommandHandlerTests
{
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UploadCompanyLogoCommandHandler CreateHandler() => new(_companyRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithOwnedCompany_SetsLogo()
    {
        var company = new Company("Acme Trading");
        var data = new byte[] { 1, 2, 3, 4 };

        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

        await CreateHandler().Handle(new UploadCompanyLogoCommand(company.Id, data, "image/png"), CancellationToken.None);

        company.LogoData.Should().BeEquivalentTo(data);
        company.LogoContentType.Should().Be("image/png");
        _companyRepository.Verify(r => r.Update(company), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownCompanyId_ThrowsDomainException()
    {
        _companyRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Company?)null);

        var act = () => CreateHandler().Handle(new UploadCompanyLogoCommand(Guid.NewGuid(), [1, 2, 3], "image/png"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _companyRepository.Verify(r => r.Update(It.IsAny<Company>()), Times.Never);
    }
}
