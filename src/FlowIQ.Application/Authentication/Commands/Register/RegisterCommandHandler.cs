using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;
using DomainRefreshToken = FlowIQ.Domain.Authentication.RefreshToken;

namespace FlowIQ.Application.Authentication.Commands.Register;

public class RegisterCommandHandler(
    IUserRepository userRepository,
    IRepository<Company> companyRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IPlatformAdminChecker platformAdminChecker,
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterCommand, AuthResult>
{
    public async ValueTask<AuthResult> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
        {
            throw new DomainException("A user with this email already exists.");
        }

        var company = new Company(command.CompanyName);
        await companyRepository.AddAsync(company, cancellationToken);

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = new User(company.Id, command.Email, passwordHash, command.FirstName, command.LastName, UserRole.Owner);
        await userRepository.AddAsync(user, cancellationToken);

        var (accessToken, expiresAtUtc) = jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();
        var refreshToken = new DomainRefreshToken(user.Id, refreshTokenValue, DateTime.UtcNow.Add(jwtTokenGenerator.RefreshTokenLifetime), command.Platform);
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
            company.Currency,
            platformAdminChecker.IsPlatformAdmin(user.Email));
    }
}
