using FlowIQ.Application.Invoicing.Emails;
using FlowIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Api.BackgroundJobs;

/// <summary>
/// Runs the invoice reminder pass inside the API: every hour (Reminders:IntervalMinutes) during the sending
/// window (Reminders:WindowStartUtcHour..WindowEndUtcHour, 06:00-18:00 UTC = 08:00-20:00 in Harare by default),
/// so customers aren't emailed at night. Running often rather than once a day means a host that was asleep
/// at 8am still sends that day; the run itself never sends the same reminder twice. A Postgres advisory lock
/// keeps two API instances from running it at the same moment. Off when Reminders:Enabled is "false".
/// </summary>
public class InvoiceReminderBackgroundService(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<InvoiceReminderBackgroundService> logger) : BackgroundService
{
    // Arbitrary but fixed: the key every instance locks on.
    private const long LockKey = 7_224_190_301;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (configuration["Reminders:Enabled"] == "false")
        {
            logger.LogInformation("Invoice reminders are switched off (Reminders:Enabled=false).");
            return;
        }

        var interval = TimeSpan.FromMinutes(int.TryParse(configuration["Reminders:IntervalMinutes"], out var m) && m > 0 ? m : 60);
        var startHour = int.TryParse(configuration["Reminders:WindowStartUtcHour"], out var s) ? s : 6;
        var endHour = int.TryParse(configuration["Reminders:WindowEndUtcHour"], out var e) ? e : 18;

        // A short pause so start-up (migrations, warm-up) settles first.
        await Task.Delay(TimeSpan.FromSeconds(30), timeProvider, stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        while (!stoppingToken.IsCancellationRequested)
        {
            var hour = timeProvider.GetUtcNow().Hour;
            if (hour >= startHour && hour < endHour)
            {
                await RunOnceAsync(stoppingToken);
            }

            try
            {
                await Task.Delay(interval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                var locked = await db.Database
                    .SqlQuery<bool>($"SELECT pg_try_advisory_lock({LockKey}) AS \"Value\"")
                    .SingleAsync(cancellationToken);
                if (!locked)
                {
                    logger.LogDebug("Another instance is running invoice reminders; skipping this round.");
                    return;
                }

                try
                {
                    var runner = scope.ServiceProvider.GetRequiredService<InvoiceReminderRunner>();
                    var summary = await runner.RunAsync(null, cancellationToken);
                    if (summary.MarkedOverdue + summary.Sent + summary.Failed + summary.Skipped > 0)
                    {
                        logger.LogInformation(
                            "Invoice reminders: {Overdue} marked overdue, {Sent} sent, {Failed} failed, {Skipped} skipped.",
                            summary.MarkedOverdue, summary.Sent, summary.Failed, summary.Skipped);
                    }
                }
                finally
                {
                    await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({LockKey})", CancellationToken.None);
                }
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // Never let one bad run stop the service; the next round tries again.
            logger.LogError(ex, "The invoice reminder run failed.");
        }
    }
}
