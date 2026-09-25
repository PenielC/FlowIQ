using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;
using FluentValidation;

namespace FlowIQ.Application.BankTransactions.TransactionImport.Commands.ConfirmTransactionImport;

public class ConfirmTransactionImportCommandValidator : AbstractValidator<ConfirmTransactionImportCommand>
{
    private const int MaxRows = 1000;

    public ConfirmTransactionImportCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Rows).NotEmpty().WithMessage("At least one row is required.");
        RuleFor(x => x.Rows).Must(r => r.Count <= MaxRows).WithMessage($"Cannot import more than {MaxRows} rows at once.");
        RuleForEach(x => x.Rows).ChildRules(row =>
        {
            row.RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            row.RuleFor(x => x.Amount).NotEqual(0m);
            row.RuleFor(x => x.Category).IsInEnum();
        });
        RuleFor(x => x.Currency)
            .Must(c => UpdateCompanyCurrencyCommandValidator.SupportedCurrencies.Contains(c))
            .WithMessage("Unsupported currency code.");
        RuleFor(x => x.ExchangeRate).GreaterThan(0m);
    }
}
