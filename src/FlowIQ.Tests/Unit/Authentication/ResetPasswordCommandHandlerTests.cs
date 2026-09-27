using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.Authentication.Commands.ResetPassword;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Authentication;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ResetPasswordCommandHandler CreateHandler() => new(
        _passwordResetTokenRepository.Object,
        _userRepository.Object,
        _refreshTokenRepository.Object,
        _passwordHasher.Object,
        _unitOfWork.Object);

    private static User CreateUser() => new(Guid.NewGuid(), "jane@acme.com", "old-hash", "Jane", "Doe", UserRole.Owner);

    [Fact]
    public async Task Handle_WithValidToken_ChangesPasswordMarksTokenUsedAndRevokesActiveSessions()
    {
        var user = CreateUser();
        var resetToken = new PasswordResetToken(user.Id, "valid-token", DateTime.UtcNow.AddHours(1));
        var activeSession = new RefreshToken(user.Id, "active-refresh", DateTime.UtcNow.AddDays(7));

        _passwordResetTokenRepository.Setup(r => r.GetByTokenAsync("valid-token", It.IsAny<CancellationToken>())).ReturnsAsync(resetToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Hash("NewPassword123!")).Returns("new-hash");
        _refreshTokenRepository.Setup(r => r.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync([activeSession]);

        await CreateHandler().Handle(new ResetPasswordCommand("valid-token", "NewPassword123!"), CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
        resetToken.IsValid.Should().BeFalse();
        activeSession.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsDomainException()
    {
        _passwordResetTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken?)null);

        var act = () => CreateHandler().Handle(new ResetPasswordCommand("nonexistent", "NewPassword123!"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsDomainException()
    {
        var expiredToken = new PasswordResetToken(Guid.NewGuid(), "expired-token", DateTime.UtcNow.AddHours(-1));
        _passwordResetTokenRepository.Setup(r => r.GetByTokenAsync("expired-token", It.IsAny<CancellationToken>())).ReturnsAsync(expiredToken);

        var act = () => CreateHandler().Handle(new ResetPasswordCommand("expired-token", "NewPassword123!"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithAlreadyUsedToken_ThrowsDomainException()
    {
        var usedToken = new PasswordResetToken(Guid.NewGuid(), "used-token", DateTime.UtcNow.AddHours(1));
        usedToken.MarkUsed();
        _passwordResetTokenRepository.Setup(r => r.GetByTokenAsync("used-token", It.IsAny<CancellationToken>())).ReturnsAsync(usedToken);

        var act = () => CreateHandler().Handle(new ResetPasswordCommand("used-token", "NewPassword123!"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
