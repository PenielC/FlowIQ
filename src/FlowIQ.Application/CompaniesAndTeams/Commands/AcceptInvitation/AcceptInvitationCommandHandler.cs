using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;
using DomainRefreshToken = FlowIQ.Domain.Authentication.RefreshToken;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.AcceptInvitation;

public class AcceptInvitationCommandHandler(
    IInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IRepository<Company> companyRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IUnitOfWork unitOfWork) : ICommandHandler<AcceptInvitationCommand, AuthResult>
{
    public async ValueTask<AuthResult> Handle(AcceptInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenAsync(command.Token, cancellationToken);
        if (invitation is null || !invitation.IsValid)
        {
            throw new DomainException("This invitation is invalid or has expired.");
        }

        if (await userRepository.ExistsByEmailAsync(invitation.Email, cancellationToken))
        {
            throw new DomainException("A user with this email already exists.");
        }

        var company = await companyRepository.GetByIdAsync(invitation.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = new User(invitation.CompanyId, invitation.Email, passwordHash, command.FirstName, command.LastName, invitation.Role);
        await userRepository.AddAsync(user, cancellationToken);

        invitation.Accept();
        invitationRepository.Update(invitation);

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
