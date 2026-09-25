namespace FlowIQ.Contracts.CompaniesAndTeams;

public record InvitationResponse(
    Guid Id,
    string Email,
    string Role,
    string Token,
    string Status,
    DateTime ExpiresAtUtc);
