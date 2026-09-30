using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;

namespace FlowIQ.Application.CashFlowForecasting;

public enum ForecastEventKind
{
    OwnerDraw = 0,
    InvoiceDue = 1,
}

/// <summary>A dated amount the forecast knows about in advance (negative is money going out).</summary>
public record ForecastEvent(DateTime DateUtc, string Label, decimal Amount, ForecastEventKind Kind);

public record CashFlowPoint(DateTime DateUtc, decimal? Actual, decimal? Forecast);

public record CashFlowForecast(
    IReadOnlyList<CashFlowPoint> Points,
    IReadOnlyList<ForecastEvent> Events,
    decimal CurrentBalance,
    decimal? LowestBalance,
    DateTime? LowestBalanceDateUtc);

/// <summary>
/// Deterministic cash-flow forecast (not a call to an external AI/ML service). The projected balance is built from:
/// <list type="bullet">
/// <item>the everyday trend: the average daily net of the history window, leaving out owner drawings and
/// contributions and invoice payments, which are forecast on their own below;</item>
/// <item>planned owner draws on their own dates (school fees, home rent), so a big payment shows up as a dip on the
/// day it happens instead of being averaged away. With nothing planned, past drawings stay in the trend so the
/// forecast is not made rosier by the owner simply not having set them up;</item>
/// <item>pending invoices (sent, not yet overdue) on their due dates. Overdue ones are left out so late payers can't
/// flatter the forecast. Beyond the invoice horizon (30 days, or the last pending due date if later), the average
/// daily invoice income of the history window takes over, standing in for invoices not issued yet.</item>
/// </list>
/// </summary>
public class CashFlowForecaster(
    ITransactionRepository transactionRepository,
    IInvoiceRepository invoiceRepository,
    IPlannedOwnerDrawRepository plannedOwnerDrawRepository,
    IDateTimeProvider dateTimeProvider)
{
    public const int InvoiceHorizonDays = 30;

    public async Task<CashFlowForecast> ForecastAsync(Guid companyId, int historyDays, int forecastDays, CancellationToken cancellationToken)
    {
        historyDays = Math.Max(1, historyDays);
        forecastDays = Math.Max(0, forecastDays);
        var today = dateTimeProvider.UtcNow.Date;
        var historyStart = today.AddDays(-(historyDays - 1));
        var forecastEnd = today.AddDays(forecastDays);

        var baseline = await transactionRepository.GetBalanceBeforeDateAsync(companyId, historyStart, cancellationToken);
        var transactions = await transactionRepository.GetInDateRangeAsync(companyId, historyStart, today.AddDays(1), cancellationToken);
        var draws = await plannedOwnerDrawRepository.ListByCompanyAsync(companyId, cancellationToken);
        var pendingInvoices = await invoiceRepository.GetPendingDueBetweenAsync(companyId, today, forecastEnd, cancellationToken);

        // ---- actual history
        var dailyNet = transactions
            .GroupBy(t => t.TransactionDateUtc.Date)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.AmountInReportingCurrency));

        var points = new List<CashFlowPoint>();
        var balance = baseline;
        for (var day = historyStart; day <= today; day = day.AddDays(1))
        {
            balance += dailyNet.GetValueOrDefault(day, 0m);
            // Today also carries a forecast value equal to itself, so the dashed line joins the solid one.
            points.Add(new CashFlowPoint(day, balance, day == today ? balance : null));
        }

        var currentBalance = balance;

        // ---- daily rates from the history window
        var everyday = transactions.Where(t => !t.Category.IsOwnerEquity() && t.InvoiceId is null).Sum(t => t.AmountInReportingCurrency);
        var invoiceIncome = transactions.Where(t => t.InvoiceId is not null).Sum(t => t.AmountInReportingCurrency);
        var pastDrawings = transactions.Where(t => t.Category == TransactionCategory.OwnerDrawings).Sum(t => t.AmountInReportingCurrency);

        var dailyTrend = everyday / historyDays;
        if (draws.Count == 0)
        {
            dailyTrend += pastDrawings / historyDays;
        }

        var dailyInvoiceIncome = invoiceIncome / historyDays;

        // ---- dated events
        var events = new List<ForecastEvent>();
        var firstForecastDay = today.AddDays(1);
        foreach (var draw in draws)
        {
            events.AddRange(draw.OccurrencesBetween(firstForecastDay, forecastEnd)
                .Select(date => new ForecastEvent(date, draw.Name, -draw.AmountInReportingCurrency, ForecastEventKind.OwnerDraw)));
        }

        foreach (var invoice in pendingInvoices)
        {
            // Due today still counts, it just lands on the first forecast day.
            var date = invoice.DueDateUtc.Date < firstForecastDay ? firstForecastDay : invoice.DueDateUtc.Date;
            events.Add(new ForecastEvent(date, invoice.CustomerName, invoice.AmountInReportingCurrency, ForecastEventKind.InvoiceDue));
        }

        events.Sort((a, b) => a.DateUtc.CompareTo(b.DateUtc));
        var eventTotals = events.GroupBy(e => e.DateUtc).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var invoiceHorizon = today.AddDays(InvoiceHorizonDays);
        if (pendingInvoices.Count > 0)
        {
            var lastDue = pendingInvoices.Max(i => i.DueDateUtc.Date);
            if (lastDue > invoiceHorizon) invoiceHorizon = lastDue;
        }

        // ---- project forward
        decimal? lowest = null;
        DateTime? lowestDate = null;
        for (var day = firstForecastDay; day <= forecastEnd; day = day.AddDays(1))
        {
            balance += dailyTrend + eventTotals.GetValueOrDefault(day, 0m);
            if (day > invoiceHorizon)
            {
                balance += dailyInvoiceIncome;
            }

            points.Add(new CashFlowPoint(day, null, Math.Round(balance, 2)));
            if (lowest is null || balance < lowest)
            {
                lowest = Math.Round(balance, 2);
                lowestDate = day;
            }
        }

        return new CashFlowForecast(points, events, currentBalance, lowest, lowestDate);
    }
}
