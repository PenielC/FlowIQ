using FlowIQ.Application.CompaniesAndTeams;
using FlowIQ.Application.CompaniesAndTeams.Commands.AcceptInvitation;
using FlowIQ.Application.CompaniesAndTeams.Commands.CreateInvitation;
using FlowIQ.Application.CompaniesAndTeams.Commands.RemoveTeamMember;
using FlowIQ.Application.CompaniesAndTeams.Commands.RevokeInvitation;
using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateTeamMemberRole;
using FlowIQ.Application.CompaniesAndTeams.Queries.GetPendingInvitations;
using FlowIQ.Application.CompaniesAndTeams.Queries.GetTeamMembers;
using FlowIQ.Contracts.Authentication;
using FlowIQ.Contracts.CompaniesAndTeams;
using FlowIQ.Contracts.Common;
using FlowIQ.Domain.Authentication;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/team")]
public class TeamController(ISender sender) : ControllerBase
{
    [HttpGet("members")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TeamMemberResponse>>>> GetMembers(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTeamMembersQuery(CurrentCompanyId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<TeamMemberResponse>>.Ok(result.Select(ToResponse).ToList()));
    }

    [HttpGet("invitations")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InvitationResponse>>>> GetInvitations(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPendingInvitationsQuery(CurrentCompanyId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<InvitationResponse>>.Ok(result.Select(ToResponse).ToList()));
    }

    [HttpPost("invitations")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ApiResponse<InvitationResponse>>> CreateInvitation(CreateInvitationRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest(ApiResponse<InvitationResponse>.Fail(
                "Validation failed.", new Dictionary<string, string[]> { ["Role"] = [$"'{request.Role}' is not a valid role."] }));
        }

        var result = await sender.Send(new CreateInvitationCommand(CurrentCompanyId, request.Email, role), cancellationToken);
        return Ok(ApiResponse<InvitationResponse>.Ok(ToResponse(result)));
    }

    [HttpPost("invitations/{id:guid}/revoke")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeInvitation(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new RevokeInvitationCommand(CurrentCompanyId, id), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("accept-invitation")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> AcceptInvitation(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AcceptInvitationCommand(request.Token, request.FirstName, request.LastName, request.Password), cancellationToken);

        var response = new AuthResponse(
            result.AccessToken,
            result.AccessTokenExpiresAtUtc,
            result.RefreshToken,
            new UserResponse(
                result.UserId, result.Email, result.FirstName, result.LastName, result.Role.ToString(), result.CompanyId, result.CompanyName, result.CompanyCurrency));

        return Ok(ApiResponse<AuthResponse>.Ok(response));
    }

    [HttpPatch("members/{userId:guid}/role")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<TeamMemberResponse>>> UpdateRole(Guid userId, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest(ApiResponse<TeamMemberResponse>.Fail(
                "Validation failed.", new Dictionary<string, string[]> { ["Role"] = [$"'{request.Role}' is not a valid role."] }));
        }

        var result = await sender.Send(new UpdateTeamMemberRoleCommand(CurrentCompanyId, CurrentUserId, userId, role), cancellationToken);
        return Ok(ApiResponse<TeamMemberResponse>.Ok(ToResponse(result)));
    }

    [HttpDelete("members/{userId:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveMember(Guid userId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveTeamMemberCommand(CurrentCompanyId, CurrentUserId, userId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    private Guid CurrentCompanyId => Guid.Parse(User.FindFirst("company_id")!.Value);
    private Guid CurrentUserId => Guid.Parse(User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!.Value);

    private static TeamMemberResponse ToResponse(TeamMemberResult result) => new(
        result.Id, result.Email, result.FirstName, result.LastName, result.Role.ToString(), result.JoinedAtUtc);

    private static InvitationResponse ToResponse(InvitationResult result) => new(
        result.Id, result.Email, result.Role.ToString(), result.Token, result.Status.ToString(), result.ExpiresAtUtc);
}
