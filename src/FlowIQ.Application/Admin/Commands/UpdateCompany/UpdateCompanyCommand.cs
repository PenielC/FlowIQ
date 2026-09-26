using Mediator;

namespace FlowIQ.Application.Admin.Commands.UpdateCompany;

public record UpdateCompanyCommand(Guid CompanyId, string Name, string Currency) : ICommand;
