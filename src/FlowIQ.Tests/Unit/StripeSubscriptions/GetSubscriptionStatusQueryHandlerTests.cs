using AwesomeAssertions;
using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Application.StripeSubscriptions.Queries.GetSubscriptionStatus;
using FlowIQ.Domain.StripeSubscriptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.StripeSubscriptions;

public class GetSubscriptionStatusQueryHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();

    private GetSubscriptionStatusQueryHandler CreateHandler() => new(_subscriptionRepository.Object);

    [Fact]
    public async Task Handle_NoSubscription_ReturnsHasSubscriptionFalse()
    {
        var companyId = Guid.NewGuid();
        _subscriptionRepository.Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(companyId), CancellationToken.None);

        result.HasSubscription.Should().BeFalse();
        result.PlanKey.Should().BeNull();
        result.Status.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithSubscription_ReturnsMappedPlanDetails()
    {
        var companyId = Guid.NewGuid();
        var subscription = new Subscription(companyId, "standard", "cus_1");
        var periodEnd = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc);
        subscription.ApplyStripeUpdate("sub_1", "standard", SubscriptionStatus.Active, periodEnd);
        _subscriptionRepository.Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(companyId), CancellationToken.None);

        result.HasSubscription.Should().BeTrue();
        result.PlanKey.Should().Be("standard");
        result.PlanDisplayName.Should().Be("Standard");
        result.MonthlyPriceUsd.Should().Be(10m);
        result.Status.Should().Be(SubscriptionStatus.Active);
        result.CurrentPeriodEndUtc.Should().Be(periodEnd);
    }
}
