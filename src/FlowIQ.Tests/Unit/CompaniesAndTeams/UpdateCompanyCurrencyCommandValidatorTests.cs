using AwesomeAssertions;
using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateCompanyCurrency;
using Xunit;

namespace FlowIQ.Tests.Unit.CompaniesAndTeams;

public class UpdateCompanyCurrencyCommandValidatorTests
{
    private readonly UpdateCompanyCurrencyCommandValidator _validator = new();

    [Theory]
    [InlineData("ZWG")] // Zimbabwe Gold, the currency since April 2024
    [InlineData("zwg")]
    [InlineData("ZAR")]
    public void AcceptsSupportedCurrencies(string currency)
    {
        _validator.Validate(new UpdateCompanyCurrencyCommand(Guid.NewGuid(), currency)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ZWL")] // the old Zimbabwe dollar, replaced by ZWG
    [InlineData("XYZ")]
    public void RejectsUnsupportedCurrencies(string currency)
    {
        _validator.Validate(new UpdateCompanyCurrencyCommand(Guid.NewGuid(), currency)).IsValid.Should().BeFalse();
    }
}
