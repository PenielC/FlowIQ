using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;

public class UpdateCompanyCurrencyCommandHandler(
    IRepository<Company> companyRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCompanyCurrencyCommand>
{
    public async ValueTask<Unit> Handle(UpdateCompanyCurrencyCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        company.SetCurrency(command.Currency);
        companyRepository.Update(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
