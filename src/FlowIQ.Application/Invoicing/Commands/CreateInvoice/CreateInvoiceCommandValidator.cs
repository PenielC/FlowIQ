using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;
using FluentValidation;

namespace FlowIQ.Application.Invoicing.Commands.CreateInvoice;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            li.RuleFor(x => x.Amount).GreaterThan(0);
        });
        RuleFor(x => x.DueDateUtc).GreaterThanOrEqualTo(x => x.IssueDateUtc)
            .WithMessage("Due date cannot be before the issue date.");
        RuleFor(x => x.Currency)
            .Must(c => UpdateCompanyCurrencyCommandValidator.SupportedCurrencies.Contains(c))
            .WithMessage("Unsupported currency code.");
        RuleFor(x => x.ExchangeRate).GreaterThan(0m).When(x => x.ExchangeRate.HasValue);
    }
}
