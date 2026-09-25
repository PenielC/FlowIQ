using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Application.StripeSubscriptions.Commands.CreateBillingPortalSession;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.StripeSubscriptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.StripeSubscriptions;

public class CreateBillingPortalSessionCommandHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IStripeGateway> _stripeGateway = new();
    private readonly Mock<IAppUrlProvider> _appUrlProvider = new();

    public CreateBillingPortalSessionCommandHandlerTests()
    {
        _appUrlProvider.Setup(p => p.WebBaseUrl).Returns("http://localhost:5173");
    }

    private CreateBillingPortalSessionCommandHandler CreateHandler() =>
        new(_subscriptionRepository.Object, _stripeGateway.Object, _appUrlProvider.Object);

    [Fact]
    public async Task Handle_NoSubscription_ThrowsDomainException()
    {
        var companyId = Guid.NewGuid();
        _subscriptionRepository.Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);

        var act = () => CreateHandler().Handle(new CreateBillingPortalSessionCommand(companyId), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithSubscription_ReturnsPortalUrl()
    {
        var companyId = Guid.NewGuid();
        var subscription = new Subscription(companyId, "starter", "cus_123");
        _subscriptionRepository.Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _stripeGateway.Setup(g => g.CreateBillingPortalSessionAsync("cus_123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortalSessionInfo("https://billing.stripe.com/portal123"));

        var result = await CreateHandler().Handle(new CreateBillingPortalSessionCommand(companyId), CancellationToken.None);

        result.PortalUrl.Should().Be("https://billing.stripe.com/portal123");
    }
}
