using Mediator;

namespace FlowIQ.Application.Admin.Commands.SetCompanyActiveStatus;

public record SetCompanyActiveStatusCommand(Guid CompanyId, bool IsActive) : ICommand;
