using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Application.StripeSubscriptions.Commands.ProcessStripeWebhook;
using FlowIQ.Domain.StripeSubscriptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.StripeSubscriptions;

public class ProcessStripeWebhookCommandHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IStripeGateway> _stripeGateway = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime Now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    public ProcessStripeWebhookCommandHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(Now);
    }

    private ProcessStripeWebhookCommandHandler CreateHandler() =>
        new(_subscriptionRepository.Object, _stripeGateway.Object, _unitOfWork.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task Handle_DuplicateEvent_ReturnsNotProcessed_AndDoesNotTouchSubscriptions()
    {
        _stripeGateway.Setup(g => g.ConstructEvent("body", "sig"))
            .Returns(new StripeWebhookEvent("evt_1", "customer.subscription.updated", "cus_1", "sub_1", "flowiq_starter_monthly", SubscriptionStatus.Active, null));
        _subscriptionRepository.Setup(r => r.HasProcessedEventAsync("evt_1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateHandler().Handle(new ProcessStripeWebhookCommand("body", "sig"), CancellationToken.None);

        result.Processed.Should().BeFalse();
        _subscriptionRepository.Verify(r => r.GetByStripeCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewEvent_NoMatchingLocalSubscription_StillMarksEventProcessed()
    {
        _stripeGateway.Setup(g => g.ConstructEvent("body", "sig"))
            .Returns(new StripeWebhookEvent("evt_2", "customer.subscription.updated", "cus_unknown", "sub_1", "flowiq_starter_monthly", SubscriptionStatus.Active, null));
        _subscriptionRepository.Setup(r => r.HasProcessedEventAsync("evt_2", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _subscriptionRepository.Setup(r => r.GetByStripeCustomerIdAsync("cus_unknown", It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);

        var result = await CreateHandler().Handle(new ProcessStripeWebhookCommand("body", "sig"), CancellationToken.None);

        result.Processed.Should().BeTrue();
        _subscriptionRepository.Verify(r => r.MarkEventProcessedAsync("evt_2", Now, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NewEvent_UpdatesMatchingSubscription_StatusPlanAndPeriodEnd()
    {
        var companyId = Guid.NewGuid();
        var subscription = new Subscription(companyId, "standard", "cus_1");
        var periodEnd = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc);

        _stripeGateway.Setup(g => g.ConstructEvent("body", "sig"))
            .Returns(new StripeWebhookEvent("evt_3", "customer.subscription.updated", "cus_1", "sub_abc", "flowiq_standard_monthly", SubscriptionStatus.Active, periodEnd));
        _subscriptionRepository.Setup(r => r.HasProcessedEventAsync("evt_3", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _subscriptionRepository.Setup(r => r.GetByStripeCustomerIdAsync("cus_1", It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new ProcessStripeWebhookCommand("body", "sig"), CancellationToken.None);

        result.Processed.Should().BeTrue();
        subscription.StripeSubscriptionId.Should().Be("sub_abc");
        subscription.PlanKey.Should().Be("standard");
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.CurrentPeriodEndUtc.Should().Be(periodEnd);
        _subscriptionRepository.Verify(r => r.Update(subscription), Times.Once);
    }

    [Fact]
    public async Task Handle_EventWithoutSubscriptionData_JustMarksProcessed_NoLookup()
    {
        _stripeGateway.Setup(g => g.ConstructEvent("body", "sig"))
            .Returns(new StripeWebhookEvent("evt_4", "invoice.paid", null, null, null, null, null));
        _subscriptionRepository.Setup(r => r.HasProcessedEventAsync("evt_4", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateHandler().Handle(new ProcessStripeWebhookCommand("body", "sig"), CancellationToken.None);

        result.Processed.Should().BeTrue();
        _subscriptionRepository.Verify(r => r.GetByStripeCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
