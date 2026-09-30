using AwesomeAssertions;
using FlowIQ.Domain.CashFlowForecasting;
using FlowIQ.Domain.Exceptions;
using Xunit;

namespace FlowIQ.Tests.Unit.CashFlowForecasting;

public class PlannedOwnerDrawTests
{
    private static DateTime D(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static PlannedOwnerDraw Draw(DrawFrequency frequency, DateTime next, params int[] months) =>
        new(Guid.NewGuid(), "School fees", 100m, "UGX", 100m, 1m, frequency, next, months);

    [Fact]
    public void SelectedMonths_FallsOnTheAnchorDay_OfEachChosenMonth()
    {
        var fees = Draw(DrawFrequency.SelectedMonths, D(2026, 9, 5), 2, 5, 9);

        fees.OccurrencesBetween(D(2026, 9, 1), D(2027, 9, 30)).Should().Equal(
            D(2026, 9, 5), D(2027, 2, 5), D(2027, 5, 5), D(2027, 9, 5));
    }

    [Fact]
    public void Monthly_OnThe31st_UsesTheLastDayOfShorterMonths()
    {
        var rent = Draw(DrawFrequency.Monthly, D(2027, 1, 31));

        rent.OccurrencesBetween(D(2027, 1, 1), D(2027, 4, 30)).Should().Equal(
            D(2027, 1, 31), D(2027, 2, 28), D(2027, 3, 31), D(2027, 4, 30));
    }

    [Fact]
    public void Weekly_RepeatsEverySevenDays_FromTheAnchor()
    {
        var household = Draw(DrawFrequency.Weekly, D(2026, 10, 2));

        household.OccurrencesBetween(D(2026, 10, 5), D(2026, 10, 20)).Should().Equal(D(2026, 10, 9), D(2026, 10, 16));
    }

    [Fact]
    public void Nothing_IsScheduledBeforeTheNextDate()
    {
        var once = Draw(DrawFrequency.Once, D(2026, 12, 1));

        once.OccurrencesBetween(D(2026, 10, 1), D(2026, 11, 30)).Should().BeEmpty();
        once.OccurrencesBetween(D(2026, 10, 1), D(2026, 12, 31)).Should().Equal(D(2026, 12, 1));
    }

    [Fact]
    public void SelectedMonths_WithoutMonths_IsRejected()
    {
        var act = () => Draw(DrawFrequency.SelectedMonths, D(2026, 9, 5));

        act.Should().Throw<DomainException>().WithMessage("Choose at least one month.");
    }

    [Fact]
    public void Restating_ConvertsFromTheOriginalAmount()
    {
        var fees = new PlannedOwnerDraw(Guid.NewGuid(), "School fees", 1_000_000m, "UGX", 270m, 0.00027m, DrawFrequency.Once, D(2026, 10, 1), null);

        fees.RestateInReportingCurrency(0.0005m);

        fees.AmountInReportingCurrency.Should().Be(500m);
        fees.Amount.Should().Be(1_000_000m);
    }
}
