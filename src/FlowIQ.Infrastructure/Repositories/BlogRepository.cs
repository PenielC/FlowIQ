using FlowIQ.Application.Blog;
using FlowIQ.Domain.Blog;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowIQ.Infrastructure.Repositories;

public class BlogRepository(ApplicationDbContext dbContext) : EfRepository<BlogPost>(dbContext), IBlogRepository
{
    private IQueryable<BlogPost> Published => DbContext.BlogPosts.AsNoTracking().Where(p => p.PublishedAtUtc != null);

    public Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        DbContext.BlogPosts.FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

    public Task<bool> SlugTakenAsync(string slug, Guid? exceptId, CancellationToken cancellationToken = default) =>
        DbContext.BlogPosts.AnyAsync(p => p.Slug == slug && p.Id != exceptId, cancellationToken);

    public async Task<(List<BlogPost> Posts, int Total)> ListPublishedAsync(
        string? category, string? tag, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = Published;
        if (category is not null) query = query.Where(p => p.Category == category);
        if (tag is not null) query = query.Where(p => p.Tags.Contains(tag));

        var total = await query.CountAsync(cancellationToken);
        var posts = await query.OrderByDescending(p => p.PublishedAtUtc).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (posts, total);
    }

    public Task<List<BlogPost>> ListAllAsync(CancellationToken cancellationToken = default) =>
        DbContext.BlogPosts.AsNoTracking()
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    /// <summary>Same category first, then the newest of the rest.</summary>
    public Task<List<BlogPost>> ListRelatedAsync(BlogPost post, int take, CancellationToken cancellationToken = default) =>
        Published
            .Where(p => p.Id != post.Id)
            .OrderBy(p => p.Category == post.Category ? 0 : 1)
            .ThenByDescending(p => p.PublishedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<List<(string Tag, int Posts)>> PublishedTagsAsync(CancellationToken cancellationToken = default) =>
        (await Published.Select(p => p.Tags).ToListAsync(cancellationToken))
            .SelectMany(t => t)
            .GroupBy(t => t)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(t => t.Item2).ThenBy(t => t.Key)
            .ToList();

    public async Task AddImageAsync(BlogImage image, CancellationToken cancellationToken = default) =>
        await DbContext.BlogImages.AddAsync(image, cancellationToken);

    public Task<BlogImage?> GetImageAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbContext.BlogImages.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<bool> ImageExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbContext.BlogImages.AnyAsync(i => i.Id == id, cancellationToken);

    public Task IncrementViewsAsync(Guid postId, CancellationToken cancellationToken = default) =>
        DbContext.BlogPosts.Where(p => p.Id == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), cancellationToken);

    public Task IncrementCtaClicksAsync(Guid postId, CancellationToken cancellationToken = default) =>
        DbContext.BlogPosts.Where(p => p.Id == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CtaClickCount, p => p.CtaClickCount + 1), cancellationToken);

    public async Task<Dictionary<string, int>> SignupsBySourceAsync(CancellationToken cancellationToken = default) =>
        await DbContext.Companies.AsNoTracking()
            .Where(c => c.SignupSource != null && c.SignupSource.StartsWith("blog:"))
            .GroupBy(c => c.SignupSource!)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
}
