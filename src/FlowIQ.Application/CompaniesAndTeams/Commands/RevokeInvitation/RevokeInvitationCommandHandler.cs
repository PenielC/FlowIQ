using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.RevokeInvitation;

public class RevokeInvitationCommandHandler(
    IInvitationRepository invitationRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RevokeInvitationCommand>
{
    public async ValueTask<Unit> Handle(RevokeInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByIdAsync(command.InvitationId, cancellationToken);
        if (invitation is null || invitation.CompanyId != command.CompanyId)
        {
            throw new DomainException("Invitation not found.");
        }

        invitation.Revoke();
        invitationRepository.Update(invitation);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
