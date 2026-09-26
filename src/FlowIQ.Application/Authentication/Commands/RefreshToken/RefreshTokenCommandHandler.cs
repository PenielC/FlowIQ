using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;
using DomainRefreshToken = FlowIQ.Domain.Authentication.RefreshToken;

namespace FlowIQ.Application.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IRepository<Company> companyRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IPlatformAdminChecker platformAdminChecker,
    IUnitOfWork unitOfWork) : ICommandHandler<RefreshTokenCommand, AuthResult>
{
    public async ValueTask<AuthResult> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var existingToken = await refreshTokenRepository.GetByTokenAsync(command.RefreshToken, cancellationToken);
        if (existingToken is null || !existingToken.IsActive)
        {
            throw new DomainException("Invalid or expired refresh token.");
        }

        var user = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken)
            ?? throw new DomainException("User not found.");

        var company = await companyRepository.GetByIdAsync(user.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found for this user.");

        if (!company.IsActive)
        {
            throw new DomainException("This company account has been deactivated. Contact support.");
        }

        existingToken.Revoke();
        refreshTokenRepository.Update(existingToken);

        var (accessToken, expiresAtUtc) = jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshToken = new DomainRefreshToken(user.Id, newRefreshTokenValue, DateTime.UtcNow.Add(jwtTokenGenerator.RefreshTokenLifetime), command.Platform);
        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            accessToken,
            expiresAtUtc,
            newRefreshTokenValue,
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            company.Id,
            company.Name,
            company.Currency,
            platformAdminChecker.IsPlatformAdmin(user.Email));
    }
}
