using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveTeamMemberCommand>
{
    public async ValueTask<Unit> Handle(RemoveTeamMemberCommand command, CancellationToken cancellationToken)
    {
        if (command.TargetUserId == command.RequestedByUserId)
        {
            throw new DomainException("You cannot remove yourself from the team.");
        }

        var user = await userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (user is null || user.CompanyId != command.CompanyId)
        {
            throw new DomainException("Team member not found.");
        }

        if (user.Role == UserRole.Owner)
        {
            var ownerCount = await userRepository.CountByCompanyAndRoleAsync(command.CompanyId, UserRole.Owner, cancellationToken);
            if (ownerCount <= 1)
            {
                throw new DomainException("A company must have at least one owner.");
            }
        }

        userRepository.Remove(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
