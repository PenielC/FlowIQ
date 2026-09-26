using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.CompaniesAndTeams;
using Mediator;

namespace FlowIQ.Application.Admin.Queries.GetAdminOverview;

public class GetAdminOverviewQueryHandler(
    IRepository<Company> companyRepository,
    ISubscriptionRepository subscriptionRepository,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository) : IQueryHandler<GetAdminOverviewQuery, AdminOverviewResult>
{
    private const string NoSubscriptionStatus = "NoSubscription";
    private const int MonthlyTrendMonths = 6;

    public async ValueTask<AdminOverviewResult> Handle(GetAdminOverviewQuery query, CancellationToken cancellationToken)
    {
        var companies = await companyRepository.ListAsync(cancellationToken);
        var subscriptions = await subscriptionRepository.ListAsync(cancellationToken);
        var users = await userRepository.ListAsync(cancellationToken);
        var refreshTokens = await refreshTokenRepository.ListAsync(cancellationToken);

        var year = query.ReportYear ?? DateTime.UtcNow.Year;
        var month = query.ReportMonth ?? DateTime.UtcNow.Month;
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEndExclusive = monthStart.AddMonths(1);

        var subscriptionsByCompanyId = subscriptions.ToDictionary(s => s.CompanyId);
        var owners = users
            .Where(u => u.Role == UserRole.Owner)
            .GroupBy(u => u.CompanyId)
            .ToDictionary(g => g.Key, g => g.First());

        var rows = companies
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(company =>
            {
                subscriptionsByCompanyId.TryGetValue(company.Id, out var subscription);
                owners.TryGetValue(company.Id, out var owner);

                return new AdminCompanyRow(
                    company.Id,
                    company.Name,
                    company.Currency,
                    company.CreatedAtUtc,
                    owner is null ? null : $"{owner.FirstName} {owner.LastName}",
                    owner?.Email,
                    subscription is null ? NoSubscriptionStatus : subscription.Status.ToString(),
                    subscription?.PlanKey,
                    company.IsActive);
            })
            .ToList();

        var statusCounts = rows
            .GroupBy(r => r.SubscriptionStatus)
            .ToDictionary(g => g.Key, g => g.Count());

        var filteredRows = query.StatusFilter is null
            ? rows
            : rows.Where(r => r.SubscriptionStatus == query.StatusFilter).ToList();

        var page = filteredRows
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var newCompaniesThisMonth = companies.Count(c => c.CreatedAtUtc >= monthStart && c.CreatedAtUtc < monthEndExclusive);
        var newSubscriptionsThisMonth = subscriptions.Count(s => s.CreatedAtUtc >= monthStart && s.CreatedAtUtc < monthEndExclusive);

        var usageByPlatform = new Dictionary<string, int> { ["web"] = 0, ["mobile"] = 0 };
        var platformActivity = refreshTokens
            .Where(t => t.Platform is not null && t.CreatedAtUtc >= monthStart && t.CreatedAtUtc < monthEndExclusive)
            .GroupBy(t => t.Platform!)
            .Select(g => (Platform: g.Key, ActiveUsers: g.Select(t => t.UserId).Distinct().Count()));

        foreach (var (platform, activeUsers) in platformActivity)
        {
            usageByPlatform[platform] = activeUsers;
        }

        var monthlyTrend = new List<AdminMonthlyTrendPoint>();
        for (var i = MonthlyTrendMonths - 1; i >= 0; i--)
        {
            var trendMonthStart = monthStart.AddMonths(-i);
            var trendMonthEndExclusive = trendMonthStart.AddMonths(1);

            var trendNewCompanies = companies.Count(c => c.CreatedAtUtc >= trendMonthStart && c.CreatedAtUtc < trendMonthEndExclusive);
            var trendNewSubscriptions = subscriptions.Count(s => s.CreatedAtUtc >= trendMonthStart && s.CreatedAtUtc < trendMonthEndExclusive);

            monthlyTrend.Add(new AdminMonthlyTrendPoint(trendMonthStart.Year, trendMonthStart.Month, trendNewCompanies, trendNewSubscriptions));
        }

        return new AdminOverviewResult(
            companies.Count,
            newCompaniesThisMonth,
            subscriptions.Count,
            newSubscriptionsThisMonth,
            usageByPlatform,
            statusCounts,
            page,
            filteredRows.Count,
            query.PageNumber,
            query.PageSize,
            year,
            month,
            monthlyTrend);
    }
}
