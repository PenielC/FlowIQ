using Mediator;

namespace FlowIQ.Application.Authentication.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IQuery<CurrentUserResult>;

public record CurrentUserResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid CompanyId,
    string CompanyName,
    string CompanyCurrency);
