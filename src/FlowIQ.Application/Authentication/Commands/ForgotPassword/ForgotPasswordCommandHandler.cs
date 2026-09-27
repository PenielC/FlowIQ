using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using Mediator;

namespace FlowIQ.Application.Authentication.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IAppUrlProvider appUrlProvider,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork) : ICommandHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async ValueTask<Unit> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            // Deliberately no-op: revealing whether an email is registered would let an attacker enumerate accounts.
            return Unit.Value;
        }

        var tokenValue = jwtTokenGenerator.GenerateRefreshToken();
        var resetToken = new PasswordResetToken(user.Id, tokenValue, DateTime.UtcNow.Add(TokenLifetime));
        await passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var resetLink = $"{appUrlProvider.WebBaseUrl}/reset-password?token={tokenValue}";

        var html = $"""
            <p>Hi {user.FirstName},</p>
            <p>We received a request to reset your FlowIQ password. Click the link below to choose a new one:</p>
            <p><a href="{resetLink}">{resetLink}</a></p>
            <p>This link expires in 1 hour. If you didn't request this, you can safely ignore this email.</p>
            <p>— FlowIQ</p>
            """;

        await emailSender.SendAsync(user.Email, "Reset your FlowIQ password", html, cancellationToken);

        return Unit.Value;
    }
}
