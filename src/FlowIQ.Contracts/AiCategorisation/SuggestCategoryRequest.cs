namespace FlowIQ.Contracts.AiCategorisation;

/// <param name="IsIncome">True for money in, false for money out; narrows the suggestion when known.</param>
public record SuggestCategoryRequest(string Description, bool? IsIncome = null);
