using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;

namespace FlowIQ.Application.CompaniesAndTeams;

public record InvitationResult(
    Guid Id,
    string Email,
    UserRole Role,
    string Token,
    InvitationStatus Status,
    DateTime ExpiresAtUtc);
