using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.RemoveCompanyLogo;

public class RemoveCompanyLogoCommandHandler(
    IRepository<Company> companyRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveCompanyLogoCommand>
{
    public async ValueTask<Unit> Handle(RemoveCompanyLogoCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        company.RemoveLogo();
        companyRepository.Update(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
