using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Blog;
using FlowIQ.Domain.Exceptions;
using FluentValidation;
using Mediator;

namespace FlowIQ.Application.Blog;

public interface IBlogRepository : IRepository<BlogPost>
{
    /// <summary>Any post with this slug, draft or published.</summary>
    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> SlugTakenAsync(string slug, Guid? exceptId, CancellationToken cancellationToken = default);

    /// <summary>Published posts, newest first, optionally in one category or with one tag.</summary>
    Task<(List<BlogPost> Posts, int Total)> ListPublishedAsync(string? category, string? tag, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Every post, drafts included, newest first (for the admin page).</summary>
    Task<List<BlogPost>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<List<BlogPost>> ListRelatedAsync(BlogPost post, int take, CancellationToken cancellationToken = default);

    /// <summary>Tags in use on published posts, with how many posts carry each.</summary>
    Task<List<(string Tag, int Posts)>> PublishedTagsAsync(CancellationToken cancellationToken = default);

    Task AddImageAsync(BlogImage image, CancellationToken cancellationToken = default);

    Task<BlogImage?> GetImageAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ImageExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds one to the counter straight in the database, so simultaneous readers don't overwrite each other.</summary>
    Task IncrementViewsAsync(Guid postId, CancellationToken cancellationToken = default);

    Task IncrementCtaClicksAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Businesses that signed up from each blog source ("blog:{slug}").</summary>
    Task<Dictionary<string, int>> SignupsBySourceAsync(CancellationToken cancellationToken = default);
}

// ---------------------------------------------------------------- results

public record BlogPostSummary(
    Guid Id, string Title, string Slug, string Summary, string Category, IReadOnlyList<string> Tags, string AuthorName,
    Guid? CoverImageId, string? CoverImageAlt, DateTime? PublishedAtUtc, int ReadingMinutes)
{
    public static BlogPostSummary From(BlogPost p) =>
        new(p.Id, p.Title, p.Slug, p.Summary, p.Category, p.Tags, p.AuthorName, p.CoverImageId, p.CoverImageAlt, p.PublishedAtUtc, p.ReadingMinutes);
}

public record BlogPostResult(BlogPostSummary Post, string Body, IReadOnlyList<BlogPostSummary> Related);

public record BlogIndexResult(
    IReadOnlyList<BlogPostSummary> Posts, int Page, int PageCount, BlogCategory? Category, string? Tag, IReadOnlyList<(string Tag, int Posts)> Tags);

/// <param name="Signups">Businesses that registered after clicking this post's "Try FinFlow free".</param>
public record BlogPostAdminResult(
    Guid Id, string Title, string Slug, string Summary, string Body, string Category, IReadOnlyList<string> Tags, string AuthorName,
    Guid? CoverImageId, string? CoverImageAlt, DateTime? PublishedAtUtc, DateTime CreatedAtUtc, int ReadingMinutes,
    int Views, int CtaClicks, int Signups)
{
    public static BlogPostAdminResult From(BlogPost p, int signups) =>
        new(p.Id, p.Title, p.Slug, p.Summary, p.Body, p.Category, p.Tags, p.AuthorName, p.CoverImageId, p.CoverImageAlt, p.PublishedAtUtc,
            p.CreatedAtUtc, p.ReadingMinutes, p.ViewCount, p.CtaClickCount, signups);
}

// ---------------------------------------------------------------- public blog

public record GetBlogIndexQuery(string? Category, string? Tag, int Page) : IQuery<BlogIndexResult>;

public class GetBlogIndexQueryHandler(IBlogRepository blog) : IQueryHandler<GetBlogIndexQuery, BlogIndexResult>
{
    public const int PageSize = 9;

    public async ValueTask<BlogIndexResult> Handle(GetBlogIndexQuery query, CancellationToken cancellationToken)
    {
        var category = query.Category is null ? null : BlogCategories.Find(query.Category) ?? throw new KeyNotFoundException("No such category.");
        var tag = string.IsNullOrWhiteSpace(query.Tag) ? null : query.Tag.Trim().ToLowerInvariant();
        var page = Math.Max(1, query.Page);

        var (posts, total) = await blog.ListPublishedAsync(category?.Key, tag, (page - 1) * PageSize, PageSize, cancellationToken);
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        if (page > pageCount || (tag is not null && total == 0)) throw new KeyNotFoundException("No posts here.");

        return new BlogIndexResult(
            posts.Select(BlogPostSummary.From).ToList(), page, pageCount, category, tag, await blog.PublishedTagsAsync(cancellationToken));
    }
}

/// <summary>A published post by its address. Drafts are "not found" here, so they can't leak.</summary>
public record GetBlogPostQuery(string Slug) : IQuery<BlogPostResult>;

public class GetBlogPostQueryHandler(IBlogRepository blog) : IQueryHandler<GetBlogPostQuery, BlogPostResult>
{
    public async ValueTask<BlogPostResult> Handle(GetBlogPostQuery query, CancellationToken cancellationToken)
    {
        var post = await blog.GetBySlugAsync(query.Slug.ToLowerInvariant(), cancellationToken);
        if (post is not { IsPublished: true }) throw new KeyNotFoundException("Post not found.");
        var related = await blog.ListRelatedAsync(post, 3, cancellationToken);
        return new BlogPostResult(BlogPostSummary.From(post), post.Body, related.Select(BlogPostSummary.From).ToList());
    }
}

/// <summary>Every published post, newest first: for the RSS feed and sitemap.</summary>
public record GetBlogFeedQuery : IQuery<IReadOnlyList<BlogPostSummary>>;

public class GetBlogFeedQueryHandler(IBlogRepository blog) : IQueryHandler<GetBlogFeedQuery, IReadOnlyList<BlogPostSummary>>
{
    public async ValueTask<IReadOnlyList<BlogPostSummary>> Handle(GetBlogFeedQuery query, CancellationToken cancellationToken) =>
        (await blog.ListPublishedAsync(null, null, 0, 1000, cancellationToken)).Posts.Select(BlogPostSummary.From).ToList();
}

public record RecordBlogViewCommand(Guid PostId) : ICommand;

public class RecordBlogViewCommandHandler(IBlogRepository blog) : ICommandHandler<RecordBlogViewCommand>
{
    public async ValueTask<Unit> Handle(RecordBlogViewCommand command, CancellationToken cancellationToken)
    {
        await blog.IncrementViewsAsync(command.PostId, cancellationToken);
        return Unit.Value;
    }
}

/// <summary>A reader clicked "Try FinFlow free": counts it and returns the sign-up source to carry to registration.</summary>
public record RecordBlogCtaClickCommand(string Slug) : ICommand<string>;

public class RecordBlogCtaClickCommandHandler(IBlogRepository blog) : ICommandHandler<RecordBlogCtaClickCommand, string>
{
    public async ValueTask<string> Handle(RecordBlogCtaClickCommand command, CancellationToken cancellationToken)
    {
        var post = await blog.GetBySlugAsync(command.Slug.ToLowerInvariant(), cancellationToken);
        if (post is not { IsPublished: true }) return "blog";
        await blog.IncrementCtaClicksAsync(post.Id, cancellationToken);
        return post.SignupSource;
    }
}

public record BlogImageFile(byte[] Data, string ContentType);

public record GetBlogImageQuery(Guid Id) : IQuery<BlogImageFile>;

public class GetBlogImageQueryHandler(IBlogRepository blog) : IQueryHandler<GetBlogImageQuery, BlogImageFile>
{
    public async ValueTask<BlogImageFile> Handle(GetBlogImageQuery query, CancellationToken cancellationToken)
    {
        var image = await blog.GetImageAsync(query.Id, cancellationToken) ?? throw new KeyNotFoundException("Image not found.");
        return new BlogImageFile(image.Data, image.ContentType);
    }
}

// ---------------------------------------------------------------- admin

public record ListBlogPostsQuery : IQuery<IReadOnlyList<BlogPostAdminResult>>;

public class ListBlogPostsQueryHandler(IBlogRepository blog) : IQueryHandler<ListBlogPostsQuery, IReadOnlyList<BlogPostAdminResult>>
{
    public async ValueTask<IReadOnlyList<BlogPostAdminResult>> Handle(ListBlogPostsQuery query, CancellationToken cancellationToken)
    {
        var signups = await blog.SignupsBySourceAsync(cancellationToken);
        return (await blog.ListAllAsync(cancellationToken))
            .Select(p => BlogPostAdminResult.From(p, signups.GetValueOrDefault(p.SignupSource)))
            .ToList();
    }
}

public record SaveBlogPostCommand(
    Guid? Id,
    string Title,
    string? Slug,
    string Summary,
    string Body,
    string Category,
    IReadOnlyList<string>? Tags,
    string? AuthorName,
    Guid? CoverImageId,
    string? CoverImageAlt) : ICommand<BlogPostAdminResult>;

public class SaveBlogPostCommandHandler(IBlogRepository blog, IUnitOfWork unitOfWork) : ICommandHandler<SaveBlogPostCommand, BlogPostAdminResult>
{
    public async ValueTask<BlogPostAdminResult> Handle(SaveBlogPostCommand command, CancellationToken cancellationToken)
    {
        if (command.CoverImageId is { } imageId && !await blog.ImageExistsAsync(imageId, cancellationToken))
        {
            throw new DomainException("The cover image wasn't found. Upload it again.");
        }

        BlogPost post;
        if (command.Id is { } id)
        {
            post = await blog.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Post not found.");
            post.Edit(command.Title, command.Summary, command.Body, command.Category, command.Tags, command.AuthorName);
            var slug = BlogPost.CleanSlug(command.Slug, command.Title);
            if (slug != post.Slug)
            {
                await EnsureFreeAsync(slug, post.Id, cancellationToken);
                post.ChangeSlug(slug);
            }
        }
        else
        {
            // New posts start as drafts: nobody sees them until they're published.
            post = new BlogPost(command.Title, command.Slug ?? string.Empty, command.Summary, command.Body, command.Category, command.Tags, command.AuthorName);
            await EnsureFreeAsync(post.Slug, null, cancellationToken);
            await blog.AddAsync(post, cancellationToken);
        }

        post.SetCover(command.CoverImageId, command.CoverImageAlt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var signups = await blog.SignupsBySourceAsync(cancellationToken);
        return BlogPostAdminResult.From(post, signups.GetValueOrDefault(post.SignupSource));
    }

    private async Task EnsureFreeAsync(string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await blog.SlugTakenAsync(slug, exceptId, cancellationToken))
        {
            throw new DomainException($"Another post already uses the address /blog/{slug}. Choose a different one.");
        }
    }
}

public class SaveBlogPostCommandValidator : AbstractValidator<SaveBlogPostCommand>
{
    public SaveBlogPostCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(140);
        RuleFor(x => x.Slug).MaximumLength(120);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(40);
        RuleFor(x => x.AuthorName).MaximumLength(80);
        RuleFor(x => x.CoverImageAlt).MaximumLength(200);
    }
}

public record SetBlogPostPublishedCommand(Guid Id, bool Published) : ICommand<BlogPostAdminResult>;

public class SetBlogPostPublishedCommandHandler(IBlogRepository blog, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    : ICommandHandler<SetBlogPostPublishedCommand, BlogPostAdminResult>
{
    public async ValueTask<BlogPostAdminResult> Handle(SetBlogPostPublishedCommand command, CancellationToken cancellationToken)
    {
        var post = await blog.GetByIdAsync(command.Id, cancellationToken) ?? throw new KeyNotFoundException("Post not found.");
        if (command.Published) post.Publish(clock.UtcNow);
        else post.Unpublish();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var signups = await blog.SignupsBySourceAsync(cancellationToken);
        return BlogPostAdminResult.From(post, signups.GetValueOrDefault(post.SignupSource));
    }
}

public record DeleteBlogPostCommand(Guid Id) : ICommand;

public class DeleteBlogPostCommandHandler(IBlogRepository blog, IUnitOfWork unitOfWork) : ICommandHandler<DeleteBlogPostCommand>
{
    public async ValueTask<Unit> Handle(DeleteBlogPostCommand command, CancellationToken cancellationToken)
    {
        var post = await blog.GetByIdAsync(command.Id, cancellationToken) ?? throw new KeyNotFoundException("Post not found.");
        blog.Remove(post);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record UploadBlogImageCommand(byte[] Data, string ContentType, string FileName) : ICommand<Guid>;

public class UploadBlogImageCommandHandler(IBlogRepository blog, IDateTimeProvider clock, IUnitOfWork unitOfWork) : ICommandHandler<UploadBlogImageCommand, Guid>
{
    public async ValueTask<Guid> Handle(UploadBlogImageCommand command, CancellationToken cancellationToken)
    {
        var image = new BlogImage(command.Data, command.ContentType, command.FileName, clock.UtcNow);
        await blog.AddImageAsync(image, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return image.Id;
    }
}
