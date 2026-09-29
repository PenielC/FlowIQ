using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Queries.GetCurrencyChangePreview;

/// <summary>What changing the reporting currency would convert, and which currencies still need a rate from the user.</summary>
public class GetCurrencyChangePreviewQueryHandler(
    IRepository<Company> companyRepository,
    ReportingCurrencyConverter converter) : IQueryHandler<GetCurrencyChangePreviewQuery, CurrencyChangePreview>
{
    public async ValueTask<CurrencyChangePreview> Handle(GetCurrencyChangePreviewQuery query, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(query.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        return await converter.PreviewAsync(company.Id, company.Currency, query.Currency, cancellationToken);
    }
}
