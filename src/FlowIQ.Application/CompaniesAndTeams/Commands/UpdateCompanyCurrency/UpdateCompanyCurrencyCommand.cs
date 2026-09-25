using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;

public record UpdateCompanyCurrencyCommand(Guid CompanyId, string Currency) : ICommand;
