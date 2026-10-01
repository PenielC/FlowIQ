namespace FlowIQ.Domain.Analytics;

/// <summary>
/// How often one user used one part of FinFlow on one day: a page they opened ("invoices") or an action
/// they completed ("invoice.emailed"). One row per user, feature and day, with a running count, so the
/// table stays small however much people click. No content is recorded, only that the feature was used.
/// </summary>
public class UsageDay
{
    private UsageDay() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Feature { get; private set; } = string.Empty;
    public DateOnly Day { get; private set; }
    public int Count { get; private set; }
    public DateTime LastAtUtc { get; private set; }
}
