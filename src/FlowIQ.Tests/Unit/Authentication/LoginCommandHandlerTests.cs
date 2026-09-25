using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.Authentication.Commands.Login;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Authentication;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LoginCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _companyRepository.Object,
        _refreshTokenRepository.Object,
        _passwordHasher.Object,
        _jwtTokenGenerator.Object,
        _unitOfWork.Object);

    private static User CreateUser(Guid companyId) =>
        new(companyId, "john@acme.com", "hashed-password", "John", "Doe", UserRole.Owner);

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokens()
    {
        var company = new Company("Acme Trading Co.");
        var user = CreateUser(company.Id);

        _userRepository.Setup(r => r.GetByEmailAsync("john@acme.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("Password123!", "hashed-password")).Returns(true);
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _jwtTokenGenerator.Setup(j => j.GenerateAccessToken(user)).Returns(("access-token", DateTime.UtcNow.AddHours(1)));
        _jwtTokenGenerator.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwtTokenGenerator.Setup(j => j.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));

        var result = await CreateHandler().Handle(new LoginCommand("john@acme.com", "Password123!"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.CompanyName.Should().Be("Acme Trading Co.");
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsDomainException()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new LoginCommand("nobody@acme.com", "whatever"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsDomainException()
    {
        var company = new Company("Acme Trading Co.");
        var user = CreateUser(company.Id);

        _userRepository.Setup(r => r.GetByEmailAsync("john@acme.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var act = () => CreateHandler().Handle(new LoginCommand("john@acme.com", "WrongPassword"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
