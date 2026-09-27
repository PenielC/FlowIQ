using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Feedback.Commands.SendFeedback;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Feedback;

public class SendFeedbackCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<IFeedbackSettings> _feedbackSettings = new();
    private readonly Mock<IEmailSender> _emailSender = new();

    private SendFeedbackCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _companyRepository.Object,
        _feedbackSettings.Object,
        _emailSender.Object);

    [Fact]
    public async Task Handle_WithValidUser_SendsEmailToConfiguredRecipient()
    {
        var company = new Company("Acme Trading Co.");
        var user = new User(company.Id, "jane@acme.com", "hash", "Jane", "Doe", UserRole.Owner);

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _feedbackSettings.Setup(s => s.RecipientEmail).Returns("founder@flowiq.example");

        await CreateHandler().Handle(new SendFeedbackCommand(user.Id, "This is great, but I'd love a dark mode."), CancellationToken.None);

        _emailSender.Verify(
            e => e.SendAsync(
                "founder@flowiq.example",
                It.Is<string>(subject => subject.Contains("Jane") && subject.Contains("Doe")),
                It.Is<string>(html => html.Contains("Acme Trading Co.") && html.Contains("dark mode")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ThrowsDomainException()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new SendFeedbackCommand(Guid.NewGuid(), "Hello"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithNoRecipientConfigured_ThrowsInvalidOperationException()
    {
        var company = new Company("Acme Trading Co.");
        var user = new User(company.Id, "jane@acme.com", "hash", "Jane", "Doe", UserRole.Owner);

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _feedbackSettings.Setup(s => s.RecipientEmail).Returns(string.Empty);

        var act = () => CreateHandler().Handle(new SendFeedbackCommand(user.Id, "Hello"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
