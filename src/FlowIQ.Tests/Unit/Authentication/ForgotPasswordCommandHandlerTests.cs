using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.Authentication.Commands.ForgotPassword;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Authentication;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IAppUrlProvider> _appUrlProvider = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ForgotPasswordCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _passwordResetTokenRepository.Object,
        _jwtTokenGenerator.Object,
        _appUrlProvider.Object,
        _emailSender.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithKnownEmail_CreatesTokenAndSendsEmail()
    {
        var user = new User(Guid.NewGuid(), "jane@acme.com", "hashed", "Jane", "Doe", UserRole.Owner);
        _userRepository.Setup(r => r.GetByEmailAsync("jane@acme.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _jwtTokenGenerator.Setup(j => j.GenerateRefreshToken()).Returns("reset-token-value");
        _appUrlProvider.Setup(p => p.WebBaseUrl).Returns("https://app.flowiq.example");

        await CreateHandler().Handle(new ForgotPasswordCommand("jane@acme.com"), CancellationToken.None);

        _passwordResetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(
            e => e.SendAsync("jane@acme.com", It.IsAny<string>(), It.Is<string>(html => html.Contains("reset-token-value")), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_DoesNothingAndDoesNotThrow()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new ForgotPasswordCommand("nobody@acme.com"), CancellationToken.None).AsTask();

        await act.Should().NotThrowAsync();
        _passwordResetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
