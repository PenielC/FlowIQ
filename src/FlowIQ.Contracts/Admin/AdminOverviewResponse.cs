using FlowIQ.Contracts.Common;

namespace FlowIQ.Contracts.Admin;

public record AdminOverviewResponse(
    int TotalCompanies,
    int NewCompaniesThisMonth,
    int TotalSubscriptions,
    int NewSubscriptionsThisMonth,
    IReadOnlyDictionary<string, int> UsageByPlatform,
    IReadOnlyDictionary<string, int> StatusCounts,
    PagedResult<AdminCompanyRowResponse> Companies,
    int ReportYear,
    int ReportMonth,
    IReadOnlyCollection<AdminMonthlyTrendPointResponse> MonthlyTrend);

public record AdminMonthlyTrendPointResponse(int Year, int Month, int NewCompanies, int NewSubscriptions);

public record AdminCompanyRowResponse(
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
