using AwesomeAssertions;
using FlowIQ.Application.AiCategorisation;
using FlowIQ.Domain.BankTransactions;
using Xunit;

namespace FlowIQ.Tests.Unit.AiCategorisation;

public class TransactionCategorySuggesterTests
{
    [Theory]
    [InlineData("Monthly staff salaries", TransactionCategory.Payroll)]
    [InlineData("Office rent for March", TransactionCategory.RentAndLease)]
    [InlineData("Electricity bill payment", TransactionCategory.Utilities)]
    [InlineData("Invoice payment received from client", TransactionCategory.Sales)]
    [InlineData("Adobe software subscription", TransactionCategory.OperatingExpense)]
    public void Suggest_MatchesExpectedCategory_ForKeywordedDescriptions(string description, TransactionCategory expected)
    {
        var result = TransactionCategorySuggester.Suggest(description);

        result.Category.Should().Be(expected);
        result.Confidence.Should().BeGreaterThan(0);
        result.MatchedKeywords.Should().NotBeEmpty();
    }

    [Fact]
    public void Suggest_FallsBackToOther_WhenNoKeywordsMatch()
    {
        var result = TransactionCategorySuggester.Suggest("xyz random text 123");

        result.Category.Should().Be(TransactionCategory.Other);
        result.Confidence.Should().Be(0.0);
        result.MatchedKeywords.Should().BeEmpty();
    }

    [Fact]
    public void Suggest_HandlesEmptyDescription()
    {
        var result = TransactionCategorySuggester.Suggest(string.Empty);

        result.Category.Should().Be(TransactionCategory.Other);
        result.Confidence.Should().Be(0.0);
    }

    [Fact]
    public void Suggest_IsCaseInsensitive()
    {
        var result = TransactionCategorySuggester.Suggest("MONTHLY PAYROLL RUN");

        result.Category.Should().Be(TransactionCategory.Payroll);
    }

    [Fact]
    public void Suggest_MoreKeywordMatches_ProducesHigherConfidence()
    {
        var weak = TransactionCategorySuggester.Suggest("rent");
        var strong = TransactionCategorySuggester.Suggest("office space rent for the warehouse landlord");

        strong.Confidence.Should().BeGreaterThan(weak.Confidence);
        strong.Confidence.Should().BeLessThanOrEqualTo(0.95);
    }

    [Theory]
    [InlineData("School fees for Tendai, term 1")]
    [InlineData("House rent")]
    [InlineData("Rent for home")]
    [InlineData("Household groceries - family")]
    public void Suggest_PersonalCosts_AreOwnerDrawings(string description)
    {
        TransactionCategorySuggester.Suggest(description, isIncome: false).Category.Should().Be(TransactionCategory.OwnerDrawings);
    }

    [Fact]
    public void Suggest_OfficeRent_IsStillBusinessRent()
    {
        TransactionCategorySuggester.Suggest("Office rent - October", isIncome: false).Category.Should().Be(TransactionCategory.RentAndLease);
    }

    [Fact]
    public void Suggest_OwnerPuttingMoneyIn_IsAContribution()
    {
        TransactionCategorySuggester.Suggest("Own money deposit to cover stock", isIncome: true).Category.Should().Be(TransactionCategory.OwnerContribution);
    }

    [Fact]
    public void Suggest_KnownDirection_NeverSuggestsACategoryOfTheOtherDirection()
    {
        // "deposit" is a Sales keyword, but money going out can't be a sale.
        TransactionCategorySuggester.Suggest("Deposit paid for new fridge", isIncome: false).Category.Should().NotBe(TransactionCategory.Sales);
        // "internet" is a Utilities keyword, but money coming in isn't a utility bill.
        TransactionCategorySuggester.Suggest("Refund from internet provider", isIncome: true).Category.Should().NotBe(TransactionCategory.Utilities);
    }
}
