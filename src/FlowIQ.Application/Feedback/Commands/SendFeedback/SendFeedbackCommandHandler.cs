using System.Net;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Feedback.Commands.SendFeedback;

public class SendFeedbackCommandHandler(
    IUserRepository userRepository,
    IRepository<Company> companyRepository,
    IFeedbackSettings feedbackSettings,
    IEmailSender emailSender) : ICommandHandler<SendFeedbackCommand>
{
    public async ValueTask<Unit> Handle(SendFeedbackCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new DomainException("User not found.");

        var company = await companyRepository.GetByIdAsync(user.CompanyId, cancellationToken);

        var recipient = feedbackSettings.RecipientEmail;
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new InvalidOperationException("Feedback recipient email is not configured.");
        }

        var encodedMessage = WebUtility.HtmlEncode(command.Message).Replace("\n", "<br>");

        var html = $"""
            <p><strong>{WebUtility.HtmlEncode(user.FirstName)} {WebUtility.HtmlEncode(user.LastName)}</strong> ({WebUtility.HtmlEncode(user.Email)}) from <strong>{WebUtility.HtmlEncode(company?.Name ?? "Unknown company")}</strong> sent feedback:</p>
            <p>{encodedMessage}</p>
            """;

        await emailSender.SendAsync(recipient, $"FlowIQ Feedback from {user.FirstName} {user.LastName}", html, cancellationToken);

        return Unit.Value;
    }
}
