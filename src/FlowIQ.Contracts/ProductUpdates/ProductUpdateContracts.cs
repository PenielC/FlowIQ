namespace FlowIQ.Contracts.ProductUpdates;

/// <param name="Audience">"Everyone" or "OwnersAndAdmins".</param>
public record ProductUpdateResponse(
    Guid Id,
    string Title,
    string Summary,
    string? LinkUrl,
    string? LinkLabel,
    string Audience,
    bool ShowOnDashboard,
    DateTime? PublishedAtUtc,
    bool IsUnread);

public record WhatsNewResponse(IReadOnlyList<ProductUpdateResponse> Updates, int UnreadCount, IReadOnlyList<ProductUpdateResponse> DashboardCards);

public record SaveProductUpdateRequest(string Title, string Summary, string? LinkUrl, string? LinkLabel, string Audience, bool ShowOnDashboard);

public record SetPublishedRequest(bool Published);
