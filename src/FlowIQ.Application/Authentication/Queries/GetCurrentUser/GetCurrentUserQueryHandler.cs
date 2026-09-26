using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Authentication.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(
    IUserRepository userRepository,
    IRepository<Company> companyRepository,
    IPlatformAdminChecker platformAdminChecker) : IQueryHandler<GetCurrentUserQuery, CurrentUserResult>
{
    public async ValueTask<CurrentUserResult> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken)
            ?? throw new DomainException("User not found.");

        var company = await companyRepository.GetByIdAsync(user.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found for this user.");

        return new CurrentUserResult(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            company.Id,
            company.Name,
            company.Currency,
            platformAdminChecker.IsPlatformAdmin(user.Email));
    }
}
