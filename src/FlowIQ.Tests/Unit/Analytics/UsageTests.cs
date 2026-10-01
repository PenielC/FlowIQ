using AwesomeAssertions;
using FlowIQ.Application.Analytics;
using FlowIQ.Application.Common.Interfaces;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Analytics;

public class UsageTests
{
    private static readonly DateTime Now = new(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);
    private readonly Mock<IUsageStore> _store = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public UsageTests() => _clock.Setup(c => c.UtcNow).Returns(Now);

    [Fact]
    public async Task Record_KnownFeature_IsStored_UnknownIsQuietlyIgnored()
    {
        var handler = new RecordUsageCommandHandler(_store.Object, _clock.Object);
        var (company, user) = (Guid.NewGuid(), Guid.NewGuid());

        await handler.Handle(new RecordUsageCommand(company, user, "forecasting"), CancellationToken.None);
        await handler.Handle(new RecordUsageCommand(company, user, "drop table"), CancellationToken.None);

        _store.Verify(s => s.RecordAsync(company, user, "forecasting", Now, It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(s => s.RecordAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), "drop table", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Overview_CountsDistinctBusinessesAndUsers_In7And30Days_AndWeeklyTrend()
    {
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());
        var (u1, u2, u3) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _store.Setup(s => s.RowsSinceAsync(It.IsAny<DateOnly>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new(a, u1, "forecasting", Today, 3, Now),
            new(a, u2, "forecasting", Today.AddDays(-1), 1, Now),
            new(b, u3, "forecasting", Today.AddDays(-20), 2, Now),
            new(b, u3, "forecasting", Today.AddDays(-70), 1, Now),
        ]);
        _store.Setup(s => s.FeatureSwitchesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new FeatureSwitch("Payment reminders switched on", 4)]);

        var result = await new GetUsageOverviewQueryHandler(_store.Object, _clock.Object).Handle(new GetUsageOverviewQuery(), CancellationToken.None);

        var f = result.Features.Single(x => x.Key == "forecasting");
        f.Companies7.Should().Be(1);
        f.Companies30.Should().Be(2);
        f.Users7.Should().Be(2);
        f.Users30.Should().Be(3);
        f.Uses30.Should().Be(6);
        f.WeeklyCompanies.Should().HaveCount(12);
        f.WeeklyCompanies[^1].Should().Be(1);   // this week: business A
        f.WeeklyCompanies[^3].Should().Be(1);   // ~20 days ago: business B
        f.WeeklyCompanies[1].Should().Be(1);    // ~70 days ago
        f.WeeklyCompanies.Sum().Should().Be(3);
        result.ActiveCompanies30.Should().Be(2);
        result.WeekStarts[^1].Should().Be(Today.AddDays(-6));
        result.Switches.Should().ContainSingle(s => s.Companies == 4);
        result.Features.Single(x => x.Key == "invoices").Companies30.Should().Be(0);
    }

    [Fact]
    public async Task CompanyUsage_ShowsLastUseAndActiveDays()
    {
        var company = Guid.NewGuid();
        var user = Guid.NewGuid();
        _store.Setup(s => s.RowsSinceAsync(It.IsAny<DateOnly>(), company, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new(company, user, "invoices", Today, 2, Now),
            new(company, user, "invoices", Today.AddDays(-3), 1, Now.AddDays(-3)),
        ]);
        _store.Setup(s => s.LastUseByFeatureAsync(company, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DateTime> { ["invoices"] = Now, ["reports"] = Now.AddDays(-60) });

        var result = await new GetCompanyUsageQueryHandler(_store.Object, _clock.Object).Handle(new GetCompanyUsageQuery(company), CancellationToken.None);

        result.LastActiveUtc.Should().Be(Now);
        result.ActiveUsers30.Should().Be(1);
        var invoices = result.Features.Single(f => f.Key == "invoices");
        invoices.DaysActive30.Should().Be(2);
        invoices.Uses30.Should().Be(3);
        result.Features.Single(f => f.Key == "reports").LastUsedUtc.Should().Be(Now.AddDays(-60));
        result.Features.Single(f => f.Key == "settings").LastUsedUtc.Should().BeNull();
    }
}
