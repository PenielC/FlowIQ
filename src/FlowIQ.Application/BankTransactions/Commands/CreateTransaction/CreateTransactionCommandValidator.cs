using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;
using FluentValidation;

namespace FlowIQ.Application.BankTransactions.Commands.CreateTransaction;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Amount).NotEqual(0m);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Currency)
            .Must(c => UpdateCompanyCurrencyCommandValidator.SupportedCurrencies.Contains(c))
            .WithMessage("Unsupported currency code.");
        RuleFor(x => x.ExchangeRate).GreaterThan(0m).When(x => x.ExchangeRate.HasValue);
    }
}
