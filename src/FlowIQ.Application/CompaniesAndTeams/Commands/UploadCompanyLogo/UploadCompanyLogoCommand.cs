using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UploadCompanyLogo;

public record UploadCompanyLogoCommand(Guid CompanyId, byte[] Data, string ContentType) : ICommand;
