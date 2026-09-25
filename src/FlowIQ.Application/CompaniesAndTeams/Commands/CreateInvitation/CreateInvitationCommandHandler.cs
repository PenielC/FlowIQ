using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.CreateInvitation;

public class CreateInvitationCommandHandler(
    IInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateInvitationCommand, InvitationResult>
{
    public async ValueTask<InvitationResult> Handle(CreateInvitationCommand command, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
        {
            throw new DomainException("A user with this email already exists.");
        }

        var pending = await invitationRepository.GetPendingByCompanyAsync(command.CompanyId, cancellationToken);
        if (pending.Any(i => i.Email == command.Email.Trim().ToLowerInvariant()))
        {
            throw new DomainException("There is already a pending invitation for this email.");
        }

        var token = jwtTokenGenerator.GenerateRefreshToken();
        var invitation = new Invitation(command.CompanyId, command.Email, command.Role, token, DateTime.UtcNow.AddDays(7));

        await invitationRepository.AddAsync(invitation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InvitationResult(invitation.Id, invitation.Email, invitation.Role, invitation.Token, invitation.Status, invitation.ExpiresAtUtc);
    }
}
