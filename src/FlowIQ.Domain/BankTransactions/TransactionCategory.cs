namespace FlowIQ.Domain.BankTransactions;

public enum TransactionCategory
{
    Sales = 0,
    OperatingExpense = 1,
    RentAndLease = 2,
    Payroll = 3,
    Utilities = 4,
    Other = 5,

    /// <summary>Money the owner takes out for personal or household costs (school fees, home rent, family).</summary>
    OwnerDrawings = 6,

    /// <summary>Money the owner puts into the business from their own pocket.</summary>
    OwnerContribution = 7,
}

public static class TransactionCategoryExtensions
{
    /// <summary>
    /// Owner drawings and contributions move cash but are not business income or expenses, so they are left out
    /// of revenue, expense totals and category reports (they still count towards the cash balance).
    /// </summary>
    public static bool IsOwnerEquity(this TransactionCategory category) =>
        category is TransactionCategory.OwnerDrawings or TransactionCategory.OwnerContribution;
}
