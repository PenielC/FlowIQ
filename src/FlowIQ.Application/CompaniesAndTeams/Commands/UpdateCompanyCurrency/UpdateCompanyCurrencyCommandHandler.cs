using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;

/// <summary>
/// Changes the reporting currency and restates every transaction and invoice into it in the same save, so totals
/// never show old-currency figures under the new currency's symbol.
/// </summary>
public class UpdateCompanyCurrencyCommandHandler(
    IRepository<Company> companyRepository,
    ReportingCurrencyConverter converter,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCompanyCurrencyCommand>
{
    public async ValueTask<Unit> Handle(UpdateCompanyCurrencyCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        if (string.Equals(company.Currency, command.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return Unit.Value;
        }

        await converter.RestateCompanyAsync(company.Id, command.Currency, command.ManualRates, cancellationToken);
        company.SetCurrency(command.Currency);
        companyRepository.Update(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
