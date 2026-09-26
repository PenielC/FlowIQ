using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.CompaniesAndTeams;
using FlowIQ.Application.CompaniesAndTeams.Commands.AcceptInvitation;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.CompaniesAndTeams;

public class AcceptInvitationCommandHandlerTests
{
    private readonly Mock<IInvitationRepository> _invitationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IPlatformAdminChecker> _platformAdminChecker = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AcceptInvitationCommandHandler CreateHandler() => new(
        _invitationRepository.Object,
        _userRepository.Object,
        _companyRepository.Object,
        _refreshTokenRepository.Object,
        _passwordHasher.Object,
        _jwtTokenGenerator.Object,
        _platformAdminChecker.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithValidInvitation_CreatesUserAndReturnsTokens()
    {
        var companyId = Guid.NewGuid();
        var company = new Company("Acme Trading Co.");
        var invitation = new Invitation(companyId, "invitee@acme.com", UserRole.Admin, "valid-token", DateTime.UtcNow.AddDays(7));

        _invitationRepository.Setup(r => r.GetByTokenAsync("valid-token", It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _companyRepository.Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _jwtTokenGenerator.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns(("access-token", DateTime.UtcNow.AddHours(1)));
        _jwtTokenGenerator.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwtTokenGenerator.Setup(j => j.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));

        var result = await CreateHandler().Handle(
            new AcceptInvitationCommand("valid-token", "New", "Member", "Password123!"), CancellationToken.None);

        result.Role.Should().Be(UserRole.Admin);
        result.CompanyName.Should().Be("Acme Trading Co.");
        invitation.Status.Should().Be(InvitationStatus.Accepted);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExpiredInvitation_ThrowsDomainException()
    {
        var invitation = new Invitation(Guid.NewGuid(), "invitee@acme.com", UserRole.Member, "expired-token", DateTime.UtcNow.AddDays(-1));
        _invitationRepository.Setup(r => r.GetByTokenAsync("expired-token", It.IsAny<CancellationToken>())).ReturnsAsync(invitation);

        var act = () => CreateHandler().Handle(
            new AcceptInvitationCommand("expired-token", "New", "Member", "Password123!"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsDomainException()
    {
        _invitationRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Invitation?)null);

        var act = () => CreateHandler().Handle(
            new AcceptInvitationCommand("nonexistent", "New", "Member", "Password123!"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
