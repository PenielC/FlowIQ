using System.Text.Json.Serialization;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Exceptions;
using Mediator;

namespace FlowIQ.Application.Analytics;

/// <summary>Serialized by name ("Page", "Action"), as the web app expects.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UsageKind
{
    Page = 0,
    Action = 1,
}

public record UsageFeature(string Key, string Label, UsageKind Kind);

/// <summary>The parts of FinFlow whose use is counted. Anything else sent to the API is ignored.</summary>
public static class UsageFeatures
{
    public static readonly IReadOnlyList<UsageFeature> All =
    [
        new("dashboard", "Dashboard", UsageKind.Page),
        new("transactions", "Transactions", UsageKind.Page),
        new("import", "Bank statement import", UsageKind.Page),
        new("invoices", "Invoices", UsageKind.Page),
        new("customers", "Customers", UsageKind.Page),
        new("forecasting", "Forecasting", UsageKind.Page),
        new("reports", "Reports", UsageKind.Page),
        new("subscriptions", "Subscriptions", UsageKind.Page),
        new("settings", "Settings", UsageKind.Page),
        new("transaction.added", "Added a transaction", UsageKind.Action),
        new("statement.imported", "Imported a bank statement", UsageKind.Action),
        new("invoice.created", "Created an invoice", UsageKind.Action),
        new("invoice.emailed", "Emailed an invoice", UsageKind.Action),
        new("report.exported", "Exported transactions", UsageKind.Action),
    ];

    private static readonly Dictionary<string, UsageFeature> ByKey = All.ToDictionary(f => f.Key);

    public static UsageFeature? Find(string key) => ByKey.GetValueOrDefault(key);
}

public interface IUsageStore
{
    /// <summary>Adds one use to today's row for this user and feature (creating it if needed).</summary>
    Task RecordAsync(Guid companyId, Guid userId, string feature, DateTime atUtc, CancellationToken cancellationToken = default);

    /// <summary>Every usage row on or after the day: (company, user, feature, day, count, last use).</summary>
    Task<List<UsageRow>> RowsSinceAsync(DateOnly fromDay, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>Most recent use of anything, per company.</summary>
    Task<Dictionary<Guid, DateTime>> LastActiveByCompanyAsync(CancellationToken cancellationToken = default);

    /// <summary>How many businesses have taken up each feature, from FinFlow's own records (so it covers the past too).</summary>
    Task<IReadOnlyList<FeatureSwitch>> FeatureSwitchesAsync(CancellationToken cancellationToken = default);

    /// <summary>Last time each feature was used by the company, over all time.</summary>
    Task<Dictionary<string, DateTime>> LastUseByFeatureAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public record UsageRow(Guid CompanyId, Guid UserId, string Feature, DateOnly Day, int Count, DateTime LastAtUtc);

public record FeatureSwitch(string Label, int Companies);

// ---------------------------------------------------------------- recording

public record RecordUsageCommand(Guid CompanyId, Guid UserId, string Feature) : ICommand;

public class RecordUsageCommandHandler(IUsageStore store, IDateTimeProvider clock) : ICommandHandler<RecordUsageCommand>
{
    public async ValueTask<Unit> Handle(RecordUsageCommand command, CancellationToken cancellationToken)
    {
        // Unknown names are dropped rather than refused: an old app version must never see an error for this.
        if (UsageFeatures.Find(command.Feature) is not null)
        {
            await store.RecordAsync(command.CompanyId, command.UserId, command.Feature, clock.UtcNow, cancellationToken);
        }

        return Unit.Value;
    }
}

// ---------------------------------------------------------------- admin: overview

/// <param name="WeeklyCompanies">Businesses that used it in each of the last 12 weeks, oldest first.</param>
public record FeatureUsage(
    string Key, string Label, UsageKind Kind,
    int Companies7, int Companies30, int Users7, int Users30, int Uses30,
    IReadOnlyList<int> WeeklyCompanies);

public record UsageOverviewResult(
    int ActiveCompanies7,
    int ActiveCompanies30,
    IReadOnlyList<DateOnly> WeekStarts,
    IReadOnlyList<FeatureUsage> Features,
    IReadOnlyList<FeatureSwitch> Switches);

public record GetUsageOverviewQuery : IQuery<UsageOverviewResult>;

public class GetUsageOverviewQueryHandler(IUsageStore store, IDateTimeProvider clock) : IQueryHandler<GetUsageOverviewQuery, UsageOverviewResult>
{
    public const int Weeks = 12;

    public async ValueTask<UsageOverviewResult> Handle(GetUsageOverviewQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow);
        var from = today.AddDays(-(Weeks * 7 - 1));
        var rows = await store.RowsSinceAsync(from, null, cancellationToken);
        var d7 = today.AddDays(-6);
        var d30 = today.AddDays(-29);

        // Week 0 is the oldest; each "week" is a rolling 7 days ending on the last one, so the latest is complete.
        int WeekOf(DateOnly day) => Weeks - 1 - (today.DayNumber - day.DayNumber) / 7;
        var weekStarts = Enumerable.Range(0, Weeks).Select(w => today.AddDays(-((Weeks - 1 - w) * 7 + 6))).ToList();

        var features = UsageFeatures.All.Select(f =>
        {
            var mine = rows.Where(r => r.Feature == f.Key).ToList();
            var weekly = new int[Weeks];
            foreach (var g in mine.GroupBy(r => WeekOf(r.Day)))
            {
                weekly[g.Key] = g.Select(r => r.CompanyId).Distinct().Count();
            }

            return new FeatureUsage(
                f.Key, f.Label, f.Kind,
                mine.Where(r => r.Day >= d7).Select(r => r.CompanyId).Distinct().Count(),
                mine.Where(r => r.Day >= d30).Select(r => r.CompanyId).Distinct().Count(),
                mine.Where(r => r.Day >= d7).Select(r => r.UserId).Distinct().Count(),
                mine.Where(r => r.Day >= d30).Select(r => r.UserId).Distinct().Count(),
                mine.Where(r => r.Day >= d30).Sum(r => r.Count),
                weekly);
        }).ToList();

        return new UsageOverviewResult(
            rows.Where(r => r.Day >= d7).Select(r => r.CompanyId).Distinct().Count(),
            rows.Where(r => r.Day >= d30).Select(r => r.CompanyId).Distinct().Count(),
            weekStarts,
            features,
            await store.FeatureSwitchesAsync(cancellationToken));
    }
}

// ---------------------------------------------------------------- admin: one business

public record CompanyFeatureUsage(string Key, string Label, UsageKind Kind, DateTime? LastUsedUtc, int DaysActive30, int Uses30);

public record CompanyUsageResult(DateTime? LastActiveUtc, int ActiveUsers30, IReadOnlyList<CompanyFeatureUsage> Features);

public record GetCompanyUsageQuery(Guid CompanyId) : IQuery<CompanyUsageResult>;

public class GetCompanyUsageQueryHandler(IUsageStore store, IDateTimeProvider clock) : IQueryHandler<GetCompanyUsageQuery, CompanyUsageResult>
{
    public async ValueTask<CompanyUsageResult> Handle(GetCompanyUsageQuery query, CancellationToken cancellationToken)
    {
        if (query.CompanyId == Guid.Empty) throw new DomainException("Company not found.");
        var today = DateOnly.FromDateTime(clock.UtcNow);
        var rows = await store.RowsSinceAsync(today.AddDays(-29), query.CompanyId, cancellationToken);
        var lastUse = await store.LastUseByFeatureAsync(query.CompanyId, cancellationToken);

        var features = UsageFeatures.All.Select(f =>
        {
            var mine = rows.Where(r => r.Feature == f.Key).ToList();
            return new CompanyFeatureUsage(
                f.Key, f.Label, f.Kind,
                lastUse.TryGetValue(f.Key, out var at) ? at : null,
                mine.Select(r => r.Day).Distinct().Count(),
                mine.Sum(r => r.Count));
        }).ToList();

        return new CompanyUsageResult(
            lastUse.Count == 0 ? null : lastUse.Values.Max(),
            rows.Select(r => r.UserId).Distinct().Count(),
            features);
    }
}
