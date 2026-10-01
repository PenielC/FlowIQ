using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Customers;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.Invoicing;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Application.Invoicing.Commands.CreateInvoice;

public class CreateInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
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

        // The email to send the invoice and its reminders to: as given, else the saved customer's.
        var email = command.CustomerEmail;
        if (string.IsNullOrWhiteSpace(email))
        {
            email = (await customerRepository.FindByNameAsync(command.CompanyId, command.CustomerName, cancellationToken))?.Email;
        }

        invoice.SetCustomerEmail(email);

        await invoiceRepository.AddAsync(invoice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return InvoiceResult.From(invoice);
    }
}
