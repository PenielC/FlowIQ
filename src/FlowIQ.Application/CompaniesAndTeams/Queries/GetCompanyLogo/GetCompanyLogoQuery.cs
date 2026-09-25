using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Queries.GetCompanyLogo;

public record GetCompanyLogoQuery(Guid CompanyId) : IQuery<CompanyLogoResult>;

public record CompanyLogoResult(byte[]? Data, string? ContentType);
