using FlowIQ.Domain.BankTransactions;

namespace FlowIQ.Application.AiCategorisation;

public record CategorySuggestion(TransactionCategory Category, double Confidence, IReadOnlyCollection<string> MatchedKeywords);

/// <summary>
/// Keyword-scoring heuristic classifier — not a call to an external AI/ML service.
/// Each category has a set of trigger words; the description is scored against every
/// category and the highest-scoring one wins (ties go to the category listed first).
/// Falls back to Other when nothing matches. When the direction is known (money in or out),
/// only categories that fit it are considered, so an expense is never suggested as Sales.
/// </summary>
public static class TransactionCategorySuggester
{
    // Order matters for ties: the owner categories come first so "rent for home" is a drawing, not business rent.
    private static readonly Dictionary<TransactionCategory, string[]> Keywords = new()
    {
        [TransactionCategory.OwnerDrawings] =
            ["school fees", "school fee", "tuition", "personal", "household", "home", "house rent", "family", "drawings",
                "owner draw", "for self"],
        [TransactionCategory.OwnerContribution] =
            ["owner contribution", "capital", "from owner", "own funds", "own money", "personal funds", "injection"],
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

    private static readonly HashSet<TransactionCategory> IncomeCategories =
        [TransactionCategory.Sales, TransactionCategory.OwnerContribution, TransactionCategory.Other];

    /// <param name="isIncome">True for money in, false for money out, null when unknown.</param>
    public static CategorySuggestion Suggest(string description, bool? isIncome = null)
    {
        var normalized = (description ?? string.Empty).ToLowerInvariant();

        var best = TransactionCategory.Other;
        var bestMatches = new List<string>();

        foreach (var (category, keywords) in Keywords)
        {
            if (isIncome is { } income && IncomeCategories.Contains(category) != income)
            {
                continue;
            }

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
