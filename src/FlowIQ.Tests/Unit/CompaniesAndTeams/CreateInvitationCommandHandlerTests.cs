using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.CompaniesAndTeams;
using FlowIQ.Application.CompaniesAndTeams.Commands.CreateInvitation;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.CompaniesAndTeams;

public class CreateInvitationCommandHandlerTests
{
    private readonly Mock<IInvitationRepository> _invitationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateInvitationCommandHandler CreateHandler() =>
        new(_invitationRepository.Object, _userRepository.Object, _jwtTokenGenerator.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithNewEmail_CreatesInvitation()
    {
        var companyId = Guid.NewGuid();
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _invitationRepository.Setup(r => r.GetPendingByCompanyAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _jwtTokenGenerator.Setup(j => j.GenerateRefreshToken()).Returns("invite-token");

        var result = await CreateHandler().Handle(
            new CreateInvitationCommand(companyId, "new@acme.com", UserRole.Member), CancellationToken.None);

        result.Email.Should().Be("new@acme.com");
        result.Token.Should().Be("invite-token");
        _invitationRepository.Verify(r => r.AddAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAlreadyRegisteredEmail_ThrowsDomainException()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(
            new CreateInvitationCommand(Guid.NewGuid(), "taken@acme.com", UserRole.Member), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithExistingPendingInvitationForSameEmail_ThrowsDomainException()
    {
        var companyId = Guid.NewGuid();
        var existing = new Invitation(companyId, "pending@acme.com", UserRole.Member, "tok", DateTime.UtcNow.AddDays(7));

        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _invitationRepository.Setup(r => r.GetPendingByCompanyAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync([existing]);

        var act = () => CreateHandler().Handle(
            new CreateInvitationCommand(companyId, "pending@acme.com", UserRole.Member), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
