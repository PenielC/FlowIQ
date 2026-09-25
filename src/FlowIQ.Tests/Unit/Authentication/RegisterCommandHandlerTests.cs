using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.Authentication.Commands.Register;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Authentication;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RegisterCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _companyRepository.Object,
        _refreshTokenRepository.Object,
        _passwordHasher.Object,
        _jwtTokenGenerator.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithNewEmail_CreatesCompanyAndUserAndReturnsTokens()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _jwtTokenGenerator.Setup(j => j.GenerateAccessToken(It.IsAny<Domain.Authentication.User>()))
            .Returns(("access-token", DateTime.UtcNow.AddHours(1)));
        _jwtTokenGenerator.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwtTokenGenerator.Setup(j => j.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));

        var command = new RegisterCommand("Acme Trading Co.", "John", "Doe", "john@acme.com", "Password123!");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.Email.Should().Be("john@acme.com");
        result.CompanyName.Should().Be("Acme Trading Co.");

        _companyRepository.Verify(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<Domain.Authentication.User>(), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<Domain.Authentication.RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ThrowsDomainException()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RegisterCommand("Acme Trading Co.", "John", "Doe", "john@acme.com", "Password123!");

        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _companyRepository.Verify(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
