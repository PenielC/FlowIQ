using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Queries.GetPendingInvitations;

public class GetPendingInvitationsQueryHandler(IInvitationRepository invitationRepository)
    : IQueryHandler<GetPendingInvitationsQuery, IReadOnlyCollection<InvitationResult>>
{
    public async ValueTask<IReadOnlyCollection<InvitationResult>> Handle(GetPendingInvitationsQuery query, CancellationToken cancellationToken)
    {
        var invitations = await invitationRepository.GetPendingByCompanyAsync(query.CompanyId, cancellationToken);

        return invitations
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => new InvitationResult(i.Id, i.Email, i.Role, i.Token, i.Status, i.ExpiresAtUtc))
            .ToList();
    }
}
