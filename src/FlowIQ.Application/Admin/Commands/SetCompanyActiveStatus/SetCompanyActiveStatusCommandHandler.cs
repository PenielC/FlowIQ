using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Admin.Commands.SetCompanyActiveStatus;

public class SetCompanyActiveStatusCommandHandler(
    IRepository<Company> companyRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SetCompanyActiveStatusCommand>
{
    public async ValueTask<Unit> Handle(SetCompanyActiveStatusCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        if (command.IsActive)
        {
            company.Activate();
        }
        else
        {
            company.Deactivate();
        }

        companyRepository.Update(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
