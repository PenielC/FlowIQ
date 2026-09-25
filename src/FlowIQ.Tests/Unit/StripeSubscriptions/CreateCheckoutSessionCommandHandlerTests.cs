using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Application.StripeSubscriptions.Commands.CreateCheckoutSession;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.StripeSubscriptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.StripeSubscriptions;

public class CreateCheckoutSessionCommandHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IStripeGateway> _stripeGateway = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAppUrlProvider> _appUrlProvider = new();

    public CreateCheckoutSessionCommandHandlerTests()
    {
        _appUrlProvider.Setup(p => p.WebBaseUrl).Returns("http://localhost:5173");
    }

    private CreateCheckoutSessionCommandHandler CreateHandler() =>
        new(_subscriptionRepository.Object, _stripeGateway.Object, _unitOfWork.Object, _appUrlProvider.Object);

    [Fact]
    public async Task Handle_UnknownPlan_ThrowsDomainException()
    {
        var command = new CreateCheckoutSessionCommand(Guid.NewGuid(), "a@b.com", "Acme", "not-a-real-plan");

        var act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_NoExistingSubscription_CreatesStripeCustomerAndLocalSubscriptionRow()
    {
        var companyId = Guid.NewGuid();
        _subscriptionRepository.Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);
        _stripeGateway.Setup(g => g.GetOrCreateCustomerAsync(companyId, "a@b.com", "Acme", It.IsAny<CancellationToken>()))
            .ReturnsAsync("cus_123");
        _stripeGateway
            .Setup(g => g.CreateCheckoutSessionAsync("cus_123", "flowiq_starter_monthly", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionInfo("https://checkout.stripe.com/session123"));

        var result = await CreateHandler().Handle(
            new CreateCheckoutSessionCommand(companyId, "a@b.com", "Acme", "starter"), CancellationToken.None);

        result.CheckoutUrl.Should().Be("https://checkout.stripe.com/session123");
        _subscriptionRepository.Verify(r => r.AddAsync(It.Is<Subscription>(s => s.CompanyId == companyId && s.StripeCustomerId == "cus_123"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingSubscription_ReusesStripeCustomerId_DoesNotCreateANewOne()
    {
        var companyId = Guid.NewGuid();
        var existing = new Subscription(companyId, "starter", "cus_existing");
        _subscriptionRepository.Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _stripeGateway
            .Setup(g => g.CreateCheckoutSessionAsync("cus_existing", "flowiq_growth_monthly", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionInfo("https://checkout.stripe.com/session456"));

        var result = await CreateHandler().Handle(
            new CreateCheckoutSessionCommand(companyId, "a@b.com", "Acme", "growth"), CancellationToken.None);

        result.CheckoutUrl.Should().Be("https://checkout.stripe.com/session456");
        _stripeGateway.Verify(g => g.GetOrCreateCustomerAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BuildsSuccessAndCancelUrlsFromAppUrlProvider()
    {
        var companyId = Guid.NewGuid();
        _subscriptionRepository.Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);
        _stripeGateway.Setup(g => g.GetOrCreateCustomerAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cus_1");

        string? capturedSuccessUrl = null;
        string? capturedCancelUrl = null;
        _stripeGateway
            .Setup(g => g.CreateCheckoutSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, CancellationToken>((_, _, success, cancel, _) =>
            {
                capturedSuccessUrl = success;
                capturedCancelUrl = cancel;
            })
            .ReturnsAsync(new CheckoutSessionInfo("https://checkout.stripe.com/x"));

        await CreateHandler().Handle(new CreateCheckoutSessionCommand(companyId, "a@b.com", "Acme", "starter"), CancellationToken.None);

        capturedSuccessUrl.Should().StartWith("http://localhost:5173/subscriptions");
        capturedCancelUrl.Should().StartWith("http://localhost:5173/subscriptions");
    }
}
