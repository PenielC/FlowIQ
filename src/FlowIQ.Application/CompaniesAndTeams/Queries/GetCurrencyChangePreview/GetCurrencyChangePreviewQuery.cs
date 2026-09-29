using FlowIQ.Application.Common;
using Mediator;

namespace FlowIQ.Application.CompaniesAndTeams.Queries.GetCurrencyChangePreview;

public record GetCurrencyChangePreviewQuery(Guid CompanyId, string Currency) : IQuery<CurrencyChangePreview>;
