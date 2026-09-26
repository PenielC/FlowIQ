using AwesomeAssertions;
using FlowIQ.Application.Admin.Queries.GetAdminOverview;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.StripeSubscriptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Admin;

public class GetAdminOverviewQueryHandlerTests
{
    private readonly Mock<IRepository<Company>> _companyRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();

    public GetAdminOverviewQueryHandlerTests()
    {
        _refreshTokenRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private GetAdminOverviewQueryHandler CreateHandler() => new(
        _companyRepository.Object,
        _subscriptionRepository.Object,
        _userRepository.Object,
        _refreshTokenRepository.Object);

    [Fact]
    public async Task Handle_BucketsCompaniesWithNoSubscriptionRow_AsNoSubscription()
    {
        var subscribedCompany = new Company("Acme Trading Co.") { CreatedAtUtc = DateTime.UtcNow.AddDays(-10) };
        var unsubscribedCompany = new Company("Fresh Signup Co.") { CreatedAtUtc = DateTime.UtcNow.AddDays(-1) };

        var subscribedOwner = new User(subscribedCompany.Id, "owner@acme.com", "hash", "Jane", "Owner", UserRole.Owner);
        var unsubscribedOwner = new User(unsubscribedCompany.Id, "owner@fresh.com", "hash", "Sam", "Fresh", UserRole.Owner);

        var subscription = new Subscription(subscribedCompany.Id, "standard", "cus_123");
        subscription.ApplyStripeUpdate("sub_123", "standard", SubscriptionStatus.Active, DateTime.UtcNow.AddDays(20));

        _companyRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([subscribedCompany, unsubscribedCompany]);
        _subscriptionRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([subscription]);
        _userRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([subscribedOwner, unsubscribedOwner]);

        var result = await CreateHandler().Handle(new GetAdminOverviewQuery(), CancellationToken.None);

        result.TotalCompanies.Should().Be(2);
        result.StatusCounts["Active"].Should().Be(1);
        result.StatusCounts["NoSubscription"].Should().Be(1);

        var subscribedRow = result.Companies.Single(c => c.Id == subscribedCompany.Id);
        subscribedRow.SubscriptionStatus.Should().Be("Active");
        subscribedRow.PlanKey.Should().Be("standard");
        subscribedRow.OwnerEmail.Should().Be("owner@acme.com");
        subscribedRow.OwnerName.Should().Be("Jane Owner");

        var unsubscribedRow = result.Companies.Single(c => c.Id == unsubscribedCompany.Id);
        unsubscribedRow.SubscriptionStatus.Should().Be("NoSubscription");
        unsubscribedRow.PlanKey.Should().BeNull();

        subscribedRow.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsIsActiveFalse_ForDeactivatedCompany()
    {
        var company = new Company("Deactivated Co.");
        company.Deactivate();

        _companyRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([company]);
        _subscriptionRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _userRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetAdminOverviewQuery(), CancellationToken.None);

        result.Companies.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PaginatesCompanies_WhileKeepingAggregatesGlobal()
    {
        var companies = Enumerable.Range(0, 3)
            .Select(i => new Company($"Co {i}") { CreatedAtUtc = DateTime.UtcNow.AddDays(-i) })
            .ToList();

        _companyRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(companies);
        _subscriptionRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _userRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetAdminOverviewQuery(PageNumber: 1, PageSize: 2), CancellationToken.None);

        result.TotalCompanies.Should().Be(3);
        result.StatusCounts["NoSubscription"].Should().Be(3);
        result.Companies.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithCompanyHavingNoOwner_ReturnsNullOwnerFields()
    {
        var company = new Company("Ownerless Co.");

        _companyRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([company]);
        _subscriptionRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _userRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetAdminOverviewQuery(), CancellationToken.None);

        var row = result.Companies.Single();
        row.OwnerName.Should().BeNull();
        row.OwnerEmail.Should().BeNull();
        row.SubscriptionStatus.Should().Be("NoSubscription");
    }

    [Fact]
    public async Task Handle_CountsOnlyCompaniesAndSubscriptionsCreatedInTheReportMonth()
    {
        var reportMonthStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var inMonthCompany = new Company("In Month Co.") { CreatedAtUtc = reportMonthStart.AddDays(5) };
        var priorMonthCompany = new Company("Prior Month Co.") { CreatedAtUtc = reportMonthStart.AddMonths(-1) };

        var inMonthSubscription = new Subscription(inMonthCompany.Id, "standard", "cus_in_month") { CreatedAtUtc = reportMonthStart.AddDays(6) };
        var priorMonthSubscription = new Subscription(priorMonthCompany.Id, "standard", "cus_prior") { CreatedAtUtc = reportMonthStart.AddMonths(-1) };

        _companyRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([inMonthCompany, priorMonthCompany]);
        _subscriptionRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([inMonthSubscription, priorMonthSubscription]);
        _userRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetAdminOverviewQuery(ReportYear: 2026, ReportMonth: 6), CancellationToken.None);

        result.TotalCompanies.Should().Be(2);
        result.NewCompaniesThisMonth.Should().Be(1);
        result.TotalSubscriptions.Should().Be(2);
        result.NewSubscriptionsThisMonth.Should().Be(1);
        result.ReportYear.Should().Be(2026);
        result.ReportMonth.Should().Be(6);

        result.MonthlyTrend.Should().HaveCount(6);
        var june = result.MonthlyTrend.Single(t => t.Year == 2026 && t.Month == 6);
        june.NewCompanies.Should().Be(1);
        june.NewSubscriptions.Should().Be(1);
        var may = result.MonthlyTrend.Single(t => t.Year == 2026 && t.Month == 5);
        may.NewCompanies.Should().Be(1);
        may.NewSubscriptions.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MonthlyTrend_ExcludesCompaniesOutsideTheSixMonthWindow()
    {
        var reportMonthStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var tooOldCompany = new Company("Too Old Co.") { CreatedAtUtc = reportMonthStart.AddMonths(-8) };

        _companyRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([tooOldCompany]);
        _subscriptionRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _userRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetAdminOverviewQuery(ReportYear: 2026, ReportMonth: 6), CancellationToken.None);

        result.TotalCompanies.Should().Be(1);
        result.MonthlyTrend.Should().HaveCount(6);
        result.MonthlyTrend.Sum(t => t.NewCompanies).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_NarrowsCompaniesButKeepsGlobalAggregates()
    {
        var activeCompany = new Company("Active Co.");
        var freeCompany1 = new Company("Free Co. 1");
        var freeCompany2 = new Company("Free Co. 2");

        var subscription = new Subscription(activeCompany.Id, "standard", "cus_active");
        subscription.ApplyStripeUpdate("sub_active", "standard", SubscriptionStatus.Active, DateTime.UtcNow.AddDays(20));

        _companyRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([activeCompany, freeCompany1, freeCompany2]);
        _subscriptionRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([subscription]);
        _userRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetAdminOverviewQuery(StatusFilter: "Active"), CancellationToken.None);

        result.TotalCompanies.Should().Be(3);
        result.StatusCounts["Active"].Should().Be(1);
        result.StatusCounts["NoSubscription"].Should().Be(2);
        result.Companies.Should().ContainSingle();
        result.Companies.Single().Id.Should().Be(activeCompany.Id);
        result.FilteredCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CountsDistinctActiveUsersPerPlatform_WithinReportMonthOnly()
    {
        var reportMonthStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var webUserId = Guid.NewGuid();
        var mobileUserId = Guid.NewGuid();

        var webToken1 = new RefreshToken(webUserId, "web-token-1", reportMonthStart.AddDays(10), "web") { CreatedAtUtc = reportMonthStart.AddDays(2) };
        var webToken2 = new RefreshToken(webUserId, "web-token-2", reportMonthStart.AddDays(10), "web") { CreatedAtUtc = reportMonthStart.AddDays(3) };
        var priorMonthMobileToken = new RefreshToken(mobileUserId, "mobile-token-old", reportMonthStart, "mobile") { CreatedAtUtc = reportMonthStart.AddMonths(-1) };
        var unknownPlatformToken = new RefreshToken(Guid.NewGuid(), "unknown-token", reportMonthStart.AddDays(10), null) { CreatedAtUtc = reportMonthStart.AddDays(1) };

        _companyRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _subscriptionRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _userRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _refreshTokenRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([webToken1, webToken2, priorMonthMobileToken, unknownPlatformToken]);

        var result = await CreateHandler().Handle(new GetAdminOverviewQuery(ReportYear: 2026, ReportMonth: 6), CancellationToken.None);

        result.UsageByPlatform["web"].Should().Be(1);
        result.UsageByPlatform["mobile"].Should().Be(0);
    }
}
