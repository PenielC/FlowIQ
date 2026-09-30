using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;
using FlowIQ.Domain.CashFlowForecasting;
using FluentValidation;

namespace FlowIQ.Application.CashFlowForecasting.OwnerDraws;

public class CreateOwnerDrawCommandValidator : AbstractValidator<CreateOwnerDrawCommand>
{
    public CreateOwnerDrawCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency)
            .Must(c => UpdateCompanyCurrencyCommandValidator.SupportedCurrencies.Contains(c ?? string.Empty))
            .WithMessage("Unsupported currency code.");
        RuleFor(x => x.ExchangeRate).GreaterThan(0).When(x => x.ExchangeRate.HasValue);
        RuleFor(x => x.Frequency).IsInEnum();
        RuleFor(x => x.NextDateUtc).NotEmpty();
        RuleFor(x => x.Months)
            .Must(OwnerDrawMonths.AreValid)
            .When(x => x.Frequency == DrawFrequency.SelectedMonths)
            .WithMessage("Choose at least one month.");
    }
}

public class UpdateOwnerDrawCommandValidator : AbstractValidator<UpdateOwnerDrawCommand>
{
    public UpdateOwnerDrawCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.DrawId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency)
            .Must(c => UpdateCompanyCurrencyCommandValidator.SupportedCurrencies.Contains(c ?? string.Empty))
            .WithMessage("Unsupported currency code.");
        RuleFor(x => x.ExchangeRate).GreaterThan(0).When(x => x.ExchangeRate.HasValue);
        RuleFor(x => x.Frequency).IsInEnum();
        RuleFor(x => x.NextDateUtc).NotEmpty();
        RuleFor(x => x.Months)
            .Must(OwnerDrawMonths.AreValid)
            .When(x => x.Frequency == DrawFrequency.SelectedMonths)
            .WithMessage("Choose at least one month.");
    }
}

internal static class OwnerDrawMonths
{
    public static bool AreValid(IReadOnlyList<int>? months) => months is { Count: > 0 } && months.All(m => m is >= 1 and <= 12);
}
