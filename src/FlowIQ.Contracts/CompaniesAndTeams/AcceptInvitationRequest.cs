namespace FlowIQ.Contracts.CompaniesAndTeams;

public record AcceptInvitationRequest(string Token, string FirstName, string LastName, string Password);
