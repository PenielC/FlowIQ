using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Application.BankTransactions.Commands.CreateTransaction;

public class CreateTransactionCommandHandler(
    ITransactionRepository transactionRepository,
    IRepository<Company> companyRepository,
    IExchangeRateProvider exchangeRateProvider,
    IUnitOfWork unitOfWork,
    ILogger<CreateTransactionCommandHandler> logger) : ICommandHandler<CreateTransactionCommand, TransactionResult>
{
    public async ValueTask<TransactionResult> Handle(CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        var rate = await CurrencyConversion.ResolveRateAsync(
            exchangeRateProvider, logger, command.Currency, company.Currency, command.ExchangeRate, cancellationToken);
        var amountInReportingCurrency = command.Amount * rate;

        var transaction = new Transaction(
            command.CompanyId,
            command.Description,
            command.Category,
            command.Amount,
            command.TransactionDateUtc,
            command.Status,
            command.Currency,
            amountInReportingCurrency,
            rate);

        await transactionRepository.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionResult(
            transaction.Id,
            transaction.Description,
            transaction.Category,
            transaction.Amount,
            transaction.TransactionDateUtc,
            transaction.Status,
            transaction.Currency,
            transaction.AmountInReportingCurrency);
    }
}
