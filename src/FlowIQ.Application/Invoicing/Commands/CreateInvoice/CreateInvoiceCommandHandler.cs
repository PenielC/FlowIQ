using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.Invoicing;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Application.Invoicing.Commands.CreateInvoice;

public class CreateInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IRepository<Company> companyRepository,
    IExchangeRateProvider exchangeRateProvider,
    IUnitOfWork unitOfWork,
    ILogger<CreateInvoiceCommandHandler> logger) : ICommandHandler<CreateInvoiceCommand, InvoiceResult>
{
    public async ValueTask<InvoiceResult> Handle(CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        var rate = await CurrencyConversion.ResolveRateAsync(
            exchangeRateProvider, logger, command.Currency, company.Currency, command.ExchangeRate, cancellationToken);

        var invoice = new Invoice(
            command.CompanyId,
            command.CustomerName,
            command.LineItems.Select(li => (li.Description, li.Amount)).ToList(),
            command.IssueDateUtc,
            command.DueDateUtc,
            InvoiceStatus.Sent,
            command.Currency,
            rate,
            command.Notes);

        await invoiceRepository.AddAsync(invoice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InvoiceResult(
            invoice.Id, invoice.CustomerName, invoice.Amount, invoice.IssueDateUtc, invoice.DueDateUtc, invoice.Status,
            invoice.Currency, invoice.AmountInReportingCurrency,
            invoice.LineItems.Select(li => new InvoiceLineItemResult(li.Id, li.Description, li.Amount)).ToList(),
            invoice.Notes);
    }
}
