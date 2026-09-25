using FluentValidation;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;

public class UpdateCompanyCurrencyCommandValidator : AbstractValidator<UpdateCompanyCurrencyCommand>
{
    public static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP",
        "KES", "NGN", "GHS", "ZAR", "UGX", "TZS", "EGP",
        "XOF", "XAF", "RWF", "ETB", "MAD", "ZMW", "BWP", "MWK",
    };

    public UpdateCompanyCurrencyCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Currency)
            .Must(c => SupportedCurrencies.Contains(c))
            .WithMessage("Unsupported currency code.");
    }
}
