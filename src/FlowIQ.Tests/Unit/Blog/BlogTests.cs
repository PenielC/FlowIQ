using AwesomeAssertions;
using FlowIQ.Application.Blog;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Blog;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Moq;

namespace FlowIQ.Tests.Unit.Blog;

public class BlogPostTests
{
    private static BlogPost Post(string slug = "", string title = "Owner drawings & your forecast", IEnumerable<string>? tags = null) =>
        new(title, slug, "A summary.", "Some words here.", "cash-flow", tags, null);

    [Fact]
    public void Slug_IsMadeFromTheTitle_WhenNoneIsGiven()
    {
        Post().Slug.Should().Be("owner-drawings-your-forecast");
    }

    [Fact]
    public void Slug_IsCleaned_WhenGiven()
    {
        Post("  Get PAID -- on time!! ").Slug.Should().Be("get-paid-on-time");
    }

    [Fact]
    public void Slug_TooShort_IsRefused()
    {
        var act = () => Post("!!", "?");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Tags_AreLowercasedTrimmedAndDeduplicated()
    {
        Post(tags: [" Invoicing", "invoicing", "Late payments ", ""]).Tags.Should().Equal("invoicing", "late payments");
    }

    [Fact]
    public void MoreThanFiveTags_AreRefused()
    {
        var act = () => Post(tags: ["a1", "b2", "c3", "d4", "e5", "f6"]);
        act.Should().Throw<DomainException>().WithMessage("*at most 5*");
    }

    [Fact]
    public void UnknownCategory_IsRefused()
    {
        var act = () => new BlogPost("Title here", "", "Summary", "Body", "gossip", null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Author_DefaultsToTheTeam()
    {
        Post().AuthorName.Should().Be(BlogPost.DefaultAuthor);
    }

    [Fact]
    public void PublishingAgain_KeepsTheFirstDate()
    {
        var post = Post();
        var first = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc);
        post.Publish(first);
        post.Unpublish();
        post.IsPublished.Should().BeFalse();

        post.Publish(first);
        post.Publish(first.AddDays(3));
        post.PublishedAtUtc.Should().Be(first);
    }

    [Fact]
    public void PublishedPost_KeepsItsAddress()
    {
        var post = Post();
        post.Publish(DateTime.UtcNow);
        var act = () => post.ChangeSlug("something-else");
        act.Should().Throw<DomainException>().WithMessage("*links*");

        post.Unpublish();
        post.ChangeSlug("something-else");
        post.Slug.Should().Be("something-else");
    }

    [Fact]
    public void ReadingTime_IsAboutTwoHundredTwentyWordsAMinute()
    {
        var post = new BlogPost("Title here", "", "Summary", string.Join(' ', Enumerable.Repeat("word", 1100)), "guides", null, null);
        post.ReadingMinutes.Should().Be(5);
        Post().ReadingMinutes.Should().Be(1);
    }

    [Fact]
    public void SignupSource_NamesThePost()
    {
        Post("my-post").SignupSource.Should().Be("blog:my-post");
    }

    [Fact]
    public void Image_MustBeASupportedTypeAndSize()
    {
        var png = () => new BlogImage([1, 2, 3], "image/png", "a.png", DateTime.UtcNow);
        png.Should().NotThrow();

        var svg = () => new BlogImage([1, 2, 3], "image/svg+xml", "a.svg", DateTime.UtcNow);
        svg.Should().Throw<DomainException>();

        var huge = () => new BlogImage(new byte[BlogImage.MaxBytes + 1], "image/png", "a.png", DateTime.UtcNow);
        huge.Should().Throw<DomainException>().WithMessage("*2 MB*");
    }

    [Fact]
    public void Company_RecordsWhereItSignedUpFrom()
    {
        var company = new Company("Acme");
        company.RecordSignupSource("  blog:owner-drawings ");
        company.SignupSource.Should().Be("blog:owner-drawings");

        company.RecordSignupSource("   ");
        company.SignupSource.Should().BeNull();
    }
}

public class BlogHandlerTests
{
    private readonly Mock<IBlogRepository> _blog = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public BlogHandlerTests()
    {
        _blog.Setup(b => b.SignupsBySourceAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, int>());
    }

    private static BlogPost Published(string slug)
    {
        var post = new BlogPost("A title", slug, "Summary", "Body text", "guides", null, null);
        post.Publish(DateTime.UtcNow);
        return post;
    }

    [Fact]
    public async Task Draft_IsNotFoundOnThePublicBlog()
    {
        var draft = new BlogPost("A title", "secret-draft", "Summary", "Body", "guides", null, null);
        _blog.Setup(b => b.GetBySlugAsync("secret-draft", It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var act = async () => await new GetBlogPostQueryHandler(_blog.Object).Handle(new GetBlogPostQuery("secret-draft"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task PublishedPost_IsFound_ByAnyCaseOfItsAddress()
    {
        var post = Published("get-paid");
        _blog.Setup(b => b.GetBySlugAsync("get-paid", It.IsAny<CancellationToken>())).ReturnsAsync(post);
        _blog.Setup(b => b.ListRelatedAsync(post, 3, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await new GetBlogPostQueryHandler(_blog.Object).Handle(new GetBlogPostQuery("Get-Paid"), CancellationToken.None);

        result.Post.Slug.Should().Be("get-paid");
    }

    [Fact]
    public async Task CtaClick_IsCounted_AndCarriesThePostAsSignupSource()
    {
        var post = Published("get-paid");
        _blog.Setup(b => b.GetBySlugAsync("get-paid", It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var source = await new RecordBlogCtaClickCommandHandler(_blog.Object).Handle(new RecordBlogCtaClickCommand("get-paid"), CancellationToken.None);

        source.Should().Be("blog:get-paid");
        _blog.Verify(b => b.IncrementCtaClicksAsync(post.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CtaClick_OnUnknownPost_StillLeadsToSignup_WithoutCounting()
    {
        var source = await new RecordBlogCtaClickCommandHandler(_blog.Object).Handle(new RecordBlogCtaClickCommand("nope"), CancellationToken.None);

        source.Should().Be("blog");
        _blog.Verify(b => b.IncrementCtaClicksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NewPost_WithATakenAddress_IsRefused()
    {
        _blog.Setup(b => b.SlugTakenAsync("get-paid", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new SaveBlogPostCommandHandler(_blog.Object, _unitOfWork.Object);

        var act = async () => await handler.Handle(
            new SaveBlogPostCommand(null, "Get paid", null, "Summary", "Body", "getting-paid", null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already uses*");
        _blog.Verify(b => b.AddAsync(It.IsAny<BlogPost>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NewPost_IsSavedAsADraft_WithItsSignupCount()
    {
        _blog.Setup(b => b.SignupsBySourceAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, int> { ["blog:get-paid"] = 4 });
        var handler = new SaveBlogPostCommandHandler(_blog.Object, _unitOfWork.Object);

        var result = await handler.Handle(
            new SaveBlogPostCommand(null, "Get paid", null, "Summary", "Body", "getting-paid", ["Invoicing"], null, null, null), CancellationToken.None);

        result.PublishedAtUtc.Should().BeNull();
        result.Slug.Should().Be("get-paid");
        result.Tags.Should().Equal("invoicing");
        result.Signups.Should().Be(4);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_WithMissingCoverImage_IsRefused()
    {
        var handler = new SaveBlogPostCommandHandler(_blog.Object, _unitOfWork.Object);

        var act = async () => await handler.Handle(
            new SaveBlogPostCommand(null, "Get paid", null, "Summary", "Body", "getting-paid", null, null, Guid.NewGuid(), "alt"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*cover image*");
    }

    [Fact]
    public async Task Index_UnknownCategory_IsNotFound()
    {
        var act = async () => await new GetBlogIndexQueryHandler(_blog.Object).Handle(new GetBlogIndexQuery("gossip", null, 1), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Index_EmptyBlog_ShowsAnEmptyFirstPage()
    {
        _blog.Setup(b => b.ListPublishedAsync(null, null, 0, GetBlogIndexQueryHandler.PageSize, It.IsAny<CancellationToken>())).ReturnsAsync(([], 0));
        _blog.Setup(b => b.PublishedTagsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await new GetBlogIndexQueryHandler(_blog.Object).Handle(new GetBlogIndexQuery(null, null, 1), CancellationToken.None);

        result.Posts.Should().BeEmpty();
        result.PageCount.Should().Be(1);
    }
}
