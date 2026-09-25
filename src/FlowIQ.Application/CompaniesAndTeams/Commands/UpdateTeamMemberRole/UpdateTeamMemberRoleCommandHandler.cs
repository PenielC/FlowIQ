using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UpdateTeamMemberRole;

public class UpdateTeamMemberRoleCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateTeamMemberRoleCommand, TeamMemberResult>
{
    public async ValueTask<TeamMemberResult> Handle(UpdateTeamMemberRoleCommand command, CancellationToken cancellationToken)
    {
        if (command.TargetUserId == command.RequestedByUserId)
        {
            throw new DomainException("You cannot change your own role.");
        }

        var user = await userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (user is null || user.CompanyId != command.CompanyId)
        {
            throw new DomainException("Team member not found.");
        }

        if (user.Role == UserRole.Owner && command.NewRole != UserRole.Owner)
        {
            var ownerCount = await userRepository.CountByCompanyAndRoleAsync(command.CompanyId, UserRole.Owner, cancellationToken);
            if (ownerCount <= 1)
            {
                throw new DomainException("A company must have at least one owner.");
            }
        }

        user.ChangeRole(command.NewRole);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TeamMemberResult(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.CreatedAtUtc);
    }
}
