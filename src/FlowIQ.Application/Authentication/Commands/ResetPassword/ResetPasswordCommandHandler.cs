using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Authentication.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : ICommandHandler<ResetPasswordCommand>
{
    public async ValueTask<Unit> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var resetToken = await passwordResetTokenRepository.GetByTokenAsync(command.Token, cancellationToken);
        if (resetToken is null || !resetToken.IsValid)
        {
            throw new DomainException("This reset link is invalid or has expired.");
        }

        var user = await userRepository.GetByIdAsync(resetToken.UserId, cancellationToken)
            ?? throw new DomainException("This reset link is invalid or has expired.");

        user.SetPasswordHash(passwordHasher.Hash(command.NewPassword));
        userRepository.Update(user);

        resetToken.MarkUsed();
        passwordResetTokenRepository.Update(resetToken);

        var activeSessions = await refreshTokenRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
        foreach (var session in activeSessions)
        {
            session.Revoke();
            refreshTokenRepository.Update(session);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
