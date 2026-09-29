using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Admin.Commands.UpdateCompany;

public class UpdateCompanyCommandHandler(
    IRepository<Company> companyRepository,
    ReportingCurrencyConverter converter,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCompanyCommand>
{
    public async ValueTask<Unit> Handle(UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        company.Rename(command.Name);
        if (!string.Equals(company.Currency, command.Currency, StringComparison.OrdinalIgnoreCase))
        {
            // Same rule as the owner's own currency change: records are restated, never relabelled.
            await converter.RestateCompanyAsync(company.Id, command.Currency, null, cancellationToken);
            company.SetCurrency(command.Currency);
        }
        companyRepository.Update(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
