using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;
using DomainRefreshToken = FlowIQ.Domain.Authentication.RefreshToken;

namespace FlowIQ.Application.Authentication.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IRepository<Company> companyRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IUnitOfWork unitOfWork) : ICommandHandler<LoginCommand, AuthResult>
{
    public async ValueTask<AuthResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new DomainException("Invalid email or password.");
        }

        var company = await companyRepository.GetByIdAsync(user.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found for this user.");

        var (accessToken, expiresAtUtc) = jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();
        var refreshToken = new DomainRefreshToken(user.Id, refreshTokenValue, DateTime.UtcNow.Add(jwtTokenGenerator.RefreshTokenLifetime));
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            accessToken,
            expiresAtUtc,
            refreshTokenValue,
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            company.Id,
            company.Name,
            company.Currency);
    }
}
