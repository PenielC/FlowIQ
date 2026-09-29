using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Invoicing;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.Invoicing;
using Mediator;

namespace FlowIQ.Application.Admin.Commands.RepairCurrencyData;

public class RepairCurrencyDataCommandHandler(
    IRepository<Company> companyRepository,
    ITransactionRepository transactionRepository,
    IInvoiceRepository invoiceRepository,
    ReportingCurrencyConverter converter,
    IUnitOfWork unitOfWork) : ICommandHandler<RepairCurrencyDataCommand, RepairCurrencyDataResult>
{
    public async ValueTask<RepairCurrencyDataResult> Handle(RepairCurrencyDataCommand command, CancellationToken cancellationToken)
    {
        var rows = new List<CompanyRepairRow>();
        foreach (var company in await companyRepository.ListAsync(cancellationToken))
        {
            var transactions = await transactionRepository.ListByCompanyAsync(company.Id, cancellationToken);
            var invoices = await invoiceRepository.ListByCompanyAsync(company.Id, cancellationToken);

            // A foreign-currency record at exactly 1:1 is the signature of both old bugs (silent fallback, relabelling).
            var staleTransactions = transactions.Where(t => IsStale(t.Currency, t.ExchangeRateToReportingCurrency, company.Currency)).ToList();
            var staleInvoices = invoices.Where(i => IsStale(i.Currency, i.ExchangeRateToReportingCurrency, company.Currency)).ToList();

            int restatedTransactions = 0, restatedInvoices = 0, incomeRecorded = 0;
            string? problem = null;
            if (staleTransactions.Count + staleInvoices.Count > 0)
            {
                try
                {
                    var summary = await converter.RestateAsync(staleTransactions, staleInvoices, company.Currency, null, cancellationToken);
                    restatedTransactions = summary.TransactionsRestated;
                    restatedInvoices = summary.InvoicesRestated;
                }
                catch (DomainException ex)
                {
                    problem = ex.Message;
                }
            }

            if (command.BackfillPaidInvoiceIncome)
            {
                var invoicedIncome = transactions.Where(t => t.InvoiceId is not null).Select(t => t.InvoiceId!.Value).ToHashSet();
                foreach (var invoice in invoices.Where(i => i.Status == InvoiceStatus.Paid && !invoicedIncome.Contains(i.Id)))
                {
                    // When it was paid isn't recorded; the invoice's last change is the closest available date.
                    var paidAt = invoice.LastModifiedAtUtc ?? invoice.DueDateUtc;
                    if (!command.DryRun)
                    {
                        await transactionRepository.AddAsync(Transaction.ForPaidInvoice(invoice, paidAt), cancellationToken);
                    }

                    incomeRecorded++;
                }
            }

            if (restatedTransactions + restatedInvoices + incomeRecorded > 0 || problem is not null)
            {
                rows.Add(new CompanyRepairRow(company.Id, company.Name, company.Currency, restatedTransactions, restatedInvoices, incomeRecorded, problem));
            }
        }

        // A dry run changed tracked entities in memory only; without SaveChanges nothing reaches the database.
        if (!command.DryRun)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new RepairCurrencyDataResult(
            command.DryRun,
            rows,
            rows.Sum(r => r.TransactionsRestated),
            rows.Sum(r => r.InvoicesRestated),
            rows.Sum(r => r.IncomeRecorded));
    }

    private static bool IsStale(string recordCurrency, decimal rate, string reportingCurrency) =>
        !string.Equals(recordCurrency, reportingCurrency, StringComparison.OrdinalIgnoreCase) && rate == 1m;
}
