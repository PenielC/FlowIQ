using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Queries.GetPendingInvitations;

public record GetPendingInvitationsQuery(Guid CompanyId) : IQuery<IReadOnlyCollection<InvitationResult>>;
