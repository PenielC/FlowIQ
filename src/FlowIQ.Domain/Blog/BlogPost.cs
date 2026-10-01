using System.Text.RegularExpressions;
using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.Blog;

/// <summary>A blog topic. The key is what appears in the address (/blog/category/cash-flow).</summary>
public record BlogCategory(string Key, string Label);

public static class BlogCategories
{
    public static readonly IReadOnlyList<BlogCategory> All =
    [
        new("cash-flow", "Cash flow"),
        new("getting-paid", "Getting paid"),
        new("guides", "Guides"),
        new("product", "Product news"),
        new("stories", "Customer stories"),
    ];

    public static BlogCategory? Find(string? key) => All.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// A FinFlow blog post written by a platform admin in Markdown. Drafts (no <see cref="PublishedAtUtc"/>) are
/// invisible on the public blog. The slug is the post's address and is fixed once published, so shared links keep working.
/// </summary>
public partial class BlogPost : BaseAuditableEntity, IAggregateRoot
{
    public const int MaxTags = 5;
    public const string DefaultAuthor = "The FinFlow team";

    private BlogPost() { }

    public BlogPost(string title, string slug, string summary, string body, string category, IEnumerable<string>? tags, string? authorName)
    {
        Id = Guid.NewGuid();
        Slug = CleanSlug(slug, title);
        Edit(title, summary, body, category, tags, authorName);
    }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    /// <summary>One or two sentences: shown on the post card and as the search/social description.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>The post itself, in Markdown.</summary>
    public string Body { get; private set; } = string.Empty;

    public string Category { get; private set; } = "guides";
    public List<string> Tags { get; private set; } = [];
    public string AuthorName { get; private set; } = DefaultAuthor;
    public Guid? CoverImageId { get; private set; }
    public string? CoverImageAlt { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    /// <summary>Times the post was read by a person (link-preview bots and crawlers aren't counted).</summary>
    public int ViewCount { get; private set; }

    /// <summary>Clicks on the post's "Try FinFlow free" button.</summary>
    public int CtaClickCount { get; private set; }

    public bool IsPublished => PublishedAtUtc is not null;

    /// <summary>What a sign-up that came from this post records as its source.</summary>
    public string SignupSource => SignupSourceFor(Slug);

    public static string SignupSourceFor(string slug) => $"blog:{slug}";

    public void Edit(string title, string summary, string body, string category, IEnumerable<string>? tags, string? authorName)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("A title is required.");
        if (string.IsNullOrWhiteSpace(summary)) throw new DomainException("Add a short summary: it's what Google and link previews show.");
        if (string.IsNullOrWhiteSpace(body)) throw new DomainException("The post has no text yet.");
        if (BlogCategories.Find(category) is not { } cat) throw new DomainException("Choose one of the blog's categories.");

        var cleanTags = (tags ?? [])
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();
        if (cleanTags.Count > MaxTags) throw new DomainException($"Use at most {MaxTags} tags.");
        if (cleanTags.Any(t => t.Length > 30 || !TagPattern().IsMatch(t)))
        {
            throw new DomainException("Tags can use letters, numbers, spaces and dashes, up to 30 characters each.");
        }

        Title = title.Trim();
        Summary = summary.Trim();
        Body = body.Trim();
        Category = cat.Key;
        Tags = cleanTags;
        AuthorName = string.IsNullOrWhiteSpace(authorName) ? DefaultAuthor : authorName.Trim();
    }

    /// <summary>Changes the address. Only drafts: a published post's links are already out there.</summary>
    public void ChangeSlug(string slug)
    {
        var clean = CleanSlug(slug, Title);
        if (clean == Slug) return;
        if (IsPublished) throw new DomainException("A published post's address can't change, or links to it would break. Unpublish it first.");
        Slug = clean;
    }

    public void SetCover(Guid? imageId, string? alt)
    {
        CoverImageId = imageId;
        CoverImageAlt = imageId is null || string.IsNullOrWhiteSpace(alt) ? null : alt.Trim();
    }

    /// <summary>Publishing again keeps the first date, so an old post doesn't jump to the top of the blog.</summary>
    public void Publish(DateTime nowUtc) => PublishedAtUtc ??= nowUtc;

    public void Unpublish() => PublishedAtUtc = null;

    /// <summary>Minutes to read at about 220 words a minute, never less than one.</summary>
    public int ReadingMinutes => Math.Max(1, (int)Math.Round(WordSplit().Split(Body).Count(w => w.Length > 0) / 220.0));

    /// <summary>
    /// The slug as given, or made from the title: lowercase letters, digits and single dashes, e.g.
    /// "Owner drawings & your forecast" becomes "owner-drawings-your-forecast".
    /// </summary>
    public static string CleanSlug(string? slug, string title)
    {
        var source = string.IsNullOrWhiteSpace(slug) ? title : slug;
        var clean = NonSlugChars().Replace(source.Trim().ToLowerInvariant(), "-").Trim('-');
        if (clean.Length > 80) clean = clean[..80].TrimEnd('-');
        if (clean.Length < 3) throw new DomainException("The post's address needs at least 3 letters or numbers.");
        return clean;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();

    [GeneratedRegex(@"^[a-z0-9][a-z0-9 \-]*$")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WordSplit();
}

/// <summary>An image uploaded for the blog (a cover, or one placed in a post's text). Served at /blog/images/{id}.</summary>
public class BlogImage : BaseEntity
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public static readonly string[] AllowedTypes = ["image/png", "image/jpeg", "image/webp", "image/gif"];

    private BlogImage() { }

    public BlogImage(byte[] data, string contentType, string fileName, DateTime uploadedAtUtc)
    {
        if (data.Length == 0) throw new DomainException("The image is empty.");
        if (data.Length > MaxBytes) throw new DomainException("Images must be 2 MB or smaller.");
        if (!AllowedTypes.Contains(contentType)) throw new DomainException("Use a PNG, JPEG, WebP or GIF image.");

        Id = Guid.NewGuid();
        Data = data;
        ContentType = contentType;
        FileName = fileName.Length > 120 ? fileName[..120] : fileName;
        UploadedAtUtc = uploadedAtUtc;
    }

    public byte[] Data { get; private set; } = [];
    public string ContentType { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public DateTime UploadedAtUtc { get; private set; }
}
