using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;

/// <param name="ManualRates">Rates the user entered for currencies no source covers, keyed by currency code.</param>
public record UpdateCompanyCurrencyCommand(Guid CompanyId, string Currency, IReadOnlyDictionary<string, decimal>? ManualRates = null) : ICommand;
