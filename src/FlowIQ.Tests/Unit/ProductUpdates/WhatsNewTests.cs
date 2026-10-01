using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.ProductUpdates;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.ProductUpdates;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.ProductUpdates;

public class WhatsNewTests
{
    private static readonly DateTime Now = new(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

    private static ProductUpdate Published(string title, DateTime at, ProductUpdateAudience audience = ProductUpdateAudience.Everyone, bool card = false)
    {
        var u = new ProductUpdate(title, "What it does for you.", "/settings", "Try it", audience, card);
        u.Publish(at);
        return u;
    }

    [Theory]
    [InlineData("/settings")]
    [InlineData("https://flowiqfinance.com/blog/reminders")]
    [InlineData(null)]
    public void Links_InTheAppOrHttps_AreAccepted(string? link)
    {
        var act = () => new ProductUpdate("Title", "Summary", link, "Open", ProductUpdateAudience.Everyone, false);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.example")]
    public void OtherLinks_AreRefused(string link)
    {
        var act = () => new ProductUpdate("Title", "Summary", link, "Open", ProductUpdateAudience.Everyone, false);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Republishing_KeepsTheFirstDate()
    {
        var u = Published("A", Now.AddDays(-10));
        u.Publish(Now);
        u.PublishedAtUtc.Should().Be(Now.AddDays(-10));
        u.Unpublish();
        u.IsPublished.Should().BeFalse();
    }

    private static (GetWhatsNewQueryHandler Handler, Mock<IProductUpdateRepository> Repo) Handler(User user, List<ProductUpdate> published, HashSet<Guid>? dismissed = null)
    {
        var repo = new Mock<IProductUpdateRepository>();
        repo.Setup(r => r.ListPublishedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(published);
        repo.Setup(r => r.DismissedIdsAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dismissed ?? []);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        return (new GetWhatsNewQueryHandler(repo.Object, users.Object, clock.Object), repo);
    }

    [Fact]
    public async Task OnlyUpdatesAfterTheLastVisit_AreUnread()
    {
        var user = new User(Guid.NewGuid(), "m@x.com", "h", "M", "M", UserRole.Member);
        user.MarkWhatsNewSeen(Now.AddDays(-5));
        var (handler, _) = Handler(user, [Published("new", Now.AddDays(-1)), Published("old", Now.AddDays(-30))]);

        var result = await handler.Handle(new GetWhatsNewQuery(user.Id), CancellationToken.None);

        result.UnreadCount.Should().Be(1);
        result.Updates.Single(u => u.Title == "new").IsUnread.Should().BeTrue();
        result.Updates.Single(u => u.Title == "old").IsUnread.Should().BeFalse();
    }

    [Fact]
    public async Task MembersDontSeeOwnerOnlyUpdates_OwnersDo()
    {
        var updates = new List<ProductUpdate> { Published("owners", Now.AddDays(-1), ProductUpdateAudience.OwnersAndAdmins, card: true), Published("all", Now.AddDays(-2)) };
        var member = new User(Guid.NewGuid(), "m@x.com", "h", "M", "M", UserRole.Member);
        var owner = new User(Guid.NewGuid(), "o@x.com", "h", "O", "O", UserRole.Owner);

        var forMember = await Handler(member, updates).Handler.Handle(new GetWhatsNewQuery(member.Id), CancellationToken.None);
        var forOwner = await Handler(owner, updates).Handler.Handle(new GetWhatsNewQuery(owner.Id), CancellationToken.None);

        forMember.Updates.Select(u => u.Title).Should().Equal("all");
        forMember.DashboardCards.Should().BeEmpty();
        forOwner.Updates.Select(u => u.Title).Should().Equal("owners", "all");
        forOwner.DashboardCards.Select(c => c.Title).Should().Equal("owners");
    }

    [Fact]
    public async Task Cards_SkipDismissedAndOldOnes_AtMostTwo()
    {
        var owner = new User(Guid.NewGuid(), "o@x.com", "h", "O", "O", UserRole.Owner);
        var dismissedOne = Published("dismissed", Now.AddDays(-1), card: true);
        var updates = new List<ProductUpdate>
        {
            dismissedOne,
            Published("a", Now.AddDays(-2), card: true),
            Published("b", Now.AddDays(-3), card: true),
            Published("c", Now.AddDays(-4), card: true),
            Published("ancient", Now.AddDays(-120), card: true),
        };

        var result = await Handler(owner, updates, [dismissedOne.Id]).Handler.Handle(new GetWhatsNewQuery(owner.Id), CancellationToken.None);

        result.DashboardCards.Select(c => c.Title).Should().Equal("a", "b");
    }

    [Fact]
    public void NewAccounts_StartWithNothingUnread()
    {
        var user = new User(Guid.NewGuid(), "n@x.com", "h", "N", "N", UserRole.Owner);
        user.WhatsNewSeenAtUtc.Should().NotBeNull();
    }
}
