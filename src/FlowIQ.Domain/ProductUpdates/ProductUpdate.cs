using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.ProductUpdates;

public enum ProductUpdateAudience
{
    Everyone = 0,

    /// <summary>Only owners and admins: for features only they can switch on (settings, billing).</summary>
    OwnersAndAdmins = 1,
}

/// <summary>
/// A "What's new" entry written by a platform admin: a title, one line on what it does for the user, and
/// where to try it. Drafts (no <see cref="PublishedAtUtc"/>) are invisible to users. With
/// <see cref="ShowOnDashboard"/> it also appears as a card on the dashboard until the user dismisses it.
/// </summary>
public class ProductUpdate : BaseAuditableEntity, IAggregateRoot
{
    private ProductUpdate() { }

    public ProductUpdate(string title, string summary, string? linkUrl, string? linkLabel, ProductUpdateAudience audience, bool showOnDashboard)
    {
        Id = Guid.NewGuid();
        Edit(title, summary, linkUrl, linkLabel, audience, showOnDashboard);
    }

    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;

    /// <summary>An in-app path such as "/settings", or an https:// link.</summary>
    public string? LinkUrl { get; private set; }

    public string? LinkLabel { get; private set; }
    public ProductUpdateAudience Audience { get; private set; }
    public bool ShowOnDashboard { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    public bool IsPublished => PublishedAtUtc is not null;

    public void Edit(string title, string summary, string? linkUrl, string? linkLabel, ProductUpdateAudience audience, bool showOnDashboard)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("A title is required.");
        if (string.IsNullOrWhiteSpace(summary)) throw new DomainException("Say in one line what it does for the user.");

        var link = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();
        if (link is not null && !(link.StartsWith('/') && !link.StartsWith("//")) && !link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("The link must be a page in the app (starting with /) or an https:// address.");
        }

        Title = title.Trim();
        Summary = summary.Trim();
        LinkUrl = link;
        LinkLabel = link is null || string.IsNullOrWhiteSpace(linkLabel) ? null : linkLabel.Trim();
        Audience = audience;
        ShowOnDashboard = showOnDashboard;
    }

    /// <summary>Publishing again keeps the first date, so re-publishing doesn't make an old entry "new".</summary>
    public void Publish(DateTime nowUtc) => PublishedAtUtc ??= nowUtc;

    public void Unpublish() => PublishedAtUtc = null;

    public bool IsFor(bool isOwnerOrAdmin) => Audience == ProductUpdateAudience.Everyone || isOwnerOrAdmin;
}

/// <summary>A user closed (or followed) an update's dashboard card; it stays gone on every device.</summary>
public class ProductUpdateDismissal : BaseEntity
{
    private ProductUpdateDismissal() { }

    public ProductUpdateDismissal(Guid userId, Guid productUpdateId, DateTime atUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ProductUpdateId = productUpdateId;
        DismissedAtUtc = atUtc;
    }

    public Guid UserId { get; private set; }
    public Guid ProductUpdateId { get; private set; }
    public DateTime DismissedAtUtc { get; private set; }
}
