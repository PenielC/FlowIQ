using System.IdentityModel.Tokens.Jwt;
using FlowIQ.Api.Common;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.Authentication.Commands.ForgotPassword;
using FlowIQ.Application.Authentication.Commands.Login;
using FlowIQ.Application.Authentication.Commands.RefreshToken;
using FlowIQ.Application.Authentication.Commands.Register;
using FlowIQ.Application.Authentication.Commands.ResetPassword;
using FlowIQ.Application.Authentication.Queries.GetCurrentUser;
using FlowIQ.Contracts.Authentication;
using FlowIQ.Contracts.Common;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterCommand(
                request.CompanyName, request.FirstName, request.LastName, request.Email, request.Password, ClientPlatform.Read(Request), request.Source),
            cancellationToken);

        return Ok(ApiResponse<AuthResponse>.Ok(ToResponse(result)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password, ClientPlatform.Read(Request)), cancellationToken);

        return Ok(ApiResponse<AuthResponse>.Ok(ToResponse(result)));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken, ClientPlatform.Read(Request)), cancellationToken);

        return Ok(ApiResponse<AuthResponse>.Ok(ToResponse(result)));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Me(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var result = await sender.Send(new GetCurrentUserQuery(userId), cancellationToken);

        return Ok(ApiResponse<UserResponse>.Ok(new UserResponse(
            result.UserId, result.Email, result.FirstName, result.LastName, result.Role, result.CompanyId, result.CompanyName, result.CompanyCurrency,
            result.IsPlatformAdmin)));
    }

    private static AuthResponse ToResponse(AuthResult result) => new(
        result.AccessToken,
        result.AccessTokenExpiresAtUtc,
        result.RefreshToken,
        new UserResponse(
            result.UserId, result.Email, result.FirstName, result.LastName, result.Role.ToString(), result.CompanyId, result.CompanyName, result.CompanyCurrency,
            result.IsPlatformAdmin));
}
