namespace FlowIQ.Contracts.CompaniesAndTeams;

public record TeamMemberResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime JoinedAtUtc);
