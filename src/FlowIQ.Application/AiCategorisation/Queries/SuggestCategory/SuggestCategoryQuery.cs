using FlowIQ.Domain.BankTransactions;
using Mediator;

namespace FlowIQ.Application.AiCategorisation.Queries.SuggestCategory;

public record SuggestCategoryQuery(string Description, bool? IsIncome = null) : IQuery<SuggestCategoryResult>;

public record SuggestCategoryResult(TransactionCategory Category, double Confidence);
