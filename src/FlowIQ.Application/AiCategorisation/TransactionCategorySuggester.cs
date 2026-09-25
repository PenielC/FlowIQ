using FlowIQ.Domain.BankTransactions;

namespace FlowIQ.Application.AiCategorisation;

public record CategorySuggestion(TransactionCategory Category, double Confidence, IReadOnlyCollection<string> MatchedKeywords);

/// <summary>
/// Keyword-scoring heuristic classifier — not a call to an external AI/ML service.
/// Each category has a set of trigger words; the description is scored against every
/// category and the highest-scoring one wins. Falls back to Other when nothing matches.
/// </summary>
public static class TransactionCategorySuggester
{
    private static readonly Dictionary<TransactionCategory, string[]> Keywords = new()
    {
        [TransactionCategory.Sales] =
            ["sale", "sales", "invoice", "payment received", "customer", "client", "revenue", "order", "deposit"],
        [TransactionCategory.Payroll] =
            ["salary", "salaries", "payroll", "wage", "wages", "staff pay", "employee", "bonus", "commission"],
        [TransactionCategory.RentAndLease] =
            ["rent", "lease", "landlord", "office space", "warehouse"],
        [TransactionCategory.Utilities] =
            ["electricity", "water bill", "internet", "phone bill", "utility", "utilities", "gas bill", "power"],
        [TransactionCategory.OperatingExpense] =
            ["supplies", "software", "subscription", "marketing", "advertising", "ads", "equipment", "maintenance",
                "insurance", "fuel", "transport", "delivery", "stationery", "repair", "consulting", "legal fees"],
    };

    public static CategorySuggestion Suggest(string description)
    {
        var normalized = (description ?? string.Empty).ToLowerInvariant();

        var best = TransactionCategory.Other;
        var bestMatches = new List<string>();

        foreach (var (category, keywords) in Keywords)
        {
            var matches = keywords.Where(k => normalized.Contains(k)).ToList();
            if (matches.Count > bestMatches.Count)
            {
                best = category;
                bestMatches = matches;
            }
        }

        if (bestMatches.Count == 0)
        {
            return new CategorySuggestion(TransactionCategory.Other, 0.0, []);
        }

        // Longer, more specific keyword matches are more confident; cap at 0.95 since this is a heuristic, not certainty.
        var confidence = Math.Min(0.4 + (bestMatches.Count * 0.2), 0.95);

        return new CategorySuggestion(best, confidence, bestMatches);
    }
}
