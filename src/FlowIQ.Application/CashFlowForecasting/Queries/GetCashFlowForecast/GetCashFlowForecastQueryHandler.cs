using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common.Interfaces;
using Mediator;

namespace FlowIQ.Application.CashFlowForecasting.Queries.GetCashFlowForecast;

/// <summary>
/// Deterministic trend-based forecast: projects the average daily net cash flow over the
/// history window forward. Not a call to an external AI/ML service.
/// </summary>
public class GetCashFlowForecastQueryHandler(
    ITransactionRepository transactionRepository,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetCashFlowForecastQuery, CashFlowForecastResult>
{
    public async ValueTask<CashFlowForecastResult> Handle(GetCashFlowForecastQuery query, CancellationToken cancellationToken)
    {
        var today = dateTimeProvider.UtcNow.Date;
        var historyStart = today.AddDays(-(query.HistoryDays - 1));

        var baseline = await transactionRepository.GetBalanceBeforeDateAsync(query.CompanyId, historyStart, cancellationToken);
        var transactions = await transactionRepository.GetInDateRangeAsync(
            query.CompanyId, historyStart, today.AddDays(1), cancellationToken);

        var dailyNet = transactions
            .GroupBy(t => t.TransactionDateUtc.Date)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.AmountInReportingCurrency));

        var points = new List<CashFlowPoint>();
        var runningBalance = baseline;

        for (var day = historyStart; day <= today; day = day.AddDays(1))
        {
            runningBalance += dailyNet.GetValueOrDefault(day, 0m);
            var isLastActualDay = day == today;
            // The last actual day also carries a forecast value equal to itself, so the
            // dashed forecast line visually connects to the solid actual line with no gap.
            points.Add(new CashFlowPoint(day, runningBalance, isLastActualDay ? runningBalance : null));
        }

        var netChangeOverHistory = runningBalance - baseline;
        var avgDailyChange = query.HistoryDays > 0 ? netChangeOverHistory / query.HistoryDays : 0m;

        var forecastBalance = runningBalance;
        for (var i = 1; i <= query.ForecastDays; i++)
        {
            forecastBalance += avgDailyChange;
            points.Add(new CashFlowPoint(today.AddDays(i), null, forecastBalance));
        }

        return new CashFlowForecastResult(points);
    }
}
