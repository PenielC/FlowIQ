using Mediator;

namespace FlowIQ.Application.Admin.Queries.GetAdminOverview;

public record GetAdminOverviewQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? StatusFilter = null,
    int? ReportYear = null,
    int? ReportMonth = null) : IQuery<AdminOverviewResult>;

public record AdminOverviewResult(
    int TotalCompanies,
    int NewCompaniesThisMonth,
    int TotalSubscriptions,
    int NewSubscriptionsThisMonth,
    IReadOnlyDictionary<string, int> UsageByPlatform,
    IReadOnlyDictionary<string, int> StatusCounts,
    IReadOnlyCollection<AdminCompanyRow> Companies,
    int FilteredCount,
    int PageNumber,
    int PageSize,
    int ReportYear,
    int ReportMonth,
    IReadOnlyCollection<AdminMonthlyTrendPoint> MonthlyTrend);

public record AdminMonthlyTrendPoint(int Year, int Month, int NewCompanies, int NewSubscriptions);

public record AdminCompanyRow(
    Guid Id,
    string Name,
    string Currency,
    DateTime CreatedAtUtc,
    string? OwnerName,
    string? OwnerEmail,
    string SubscriptionStatus,
    string? PlanKey,
    bool IsActive,
    DateTime? LastActiveAtUtc = null);
