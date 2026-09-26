using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;
using FluentValidation;

namespace FlowIQ.Application.Admin.Commands.UpdateCompany;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Currency)
            .Must(c => UpdateCompanyCurrencyCommandValidator.SupportedCurrencies.Contains(c))
            .WithMessage("Unsupported currency code.");
    }
}
