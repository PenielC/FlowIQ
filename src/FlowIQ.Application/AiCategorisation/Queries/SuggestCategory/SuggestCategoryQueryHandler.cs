using Mediator;

namespace FlowIQ.Application.AiCategorisation.Queries.SuggestCategory;

public class SuggestCategoryQueryHandler : IQueryHandler<SuggestCategoryQuery, SuggestCategoryResult>
{
    public ValueTask<SuggestCategoryResult> Handle(SuggestCategoryQuery query, CancellationToken cancellationToken)
    {
        var suggestion = TransactionCategorySuggester.Suggest(query.Description);
        return ValueTask.FromResult(new SuggestCategoryResult(suggestion.Category, suggestion.Confidence));
    }
}
