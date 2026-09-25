using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Queries.GetCompanyLogo;

public class GetCompanyLogoQueryHandler(IRepository<Company> companyRepository) : IQueryHandler<GetCompanyLogoQuery, CompanyLogoResult>
{
    public async ValueTask<CompanyLogoResult> Handle(GetCompanyLogoQuery query, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(query.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        return new CompanyLogoResult(company.LogoData, company.LogoContentType);
    }
}
