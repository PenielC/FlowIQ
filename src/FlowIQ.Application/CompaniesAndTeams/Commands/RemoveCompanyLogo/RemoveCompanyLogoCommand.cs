using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.RemoveCompanyLogo;

public record RemoveCompanyLogoCommand(Guid CompanyId) : ICommand;
