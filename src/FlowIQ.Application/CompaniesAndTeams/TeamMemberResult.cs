using FlowIQ.Domain.Authentication;

namespace FlowIQ.Application.CompaniesAndTeams;

public record TeamMemberResult(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    DateTime JoinedAtUtc);
