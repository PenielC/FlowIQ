using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UploadCompanyLogo;

public class UploadCompanyLogoCommandHandler(
    IRepository<Company> companyRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UploadCompanyLogoCommand>
{
    public async ValueTask<Unit> Handle(UploadCompanyLogoCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        company.SetLogo(command.Data, command.ContentType);
        companyRepository.Update(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
