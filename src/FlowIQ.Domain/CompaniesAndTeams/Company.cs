using FlowIQ.Domain.Common;
using FlowIQ.Domain.Exceptions;

namespace FlowIQ.Domain.CompaniesAndTeams;

public class Company : BaseAuditableEntity, IAggregateRoot
{
    private Company() { }

    public Company(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public string Name { get; private set; } = string.Empty;
    public byte[]? LogoData { get; private set; }
    public string? LogoContentType { get; private set; }
    public string Currency { get; private set; } = "USD";
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// When the owner answered the forecast setup (planned personal withdrawals, or "none"). Null until then, which
    /// is what makes the dashboard keep asking.
    /// </summary>
    public DateTime? ForecastSetupCompletedAtUtc { get; private set; }

    public void CompleteForecastSetup(DateTime completedAtUtc) => ForecastSetupCompletedAtUtc ??= completedAtUtc;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Company name is required.");
        }

        Name = name;
    }

    public void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter code.");
        }

        Currency = currency.ToUpperInvariant();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetLogo(byte[] data, string contentType)
    {
        if (data.Length == 0)
        {
            throw new DomainException("Logo file is empty.");
        }

        LogoData = data;
        LogoContentType = contentType;
    }

    public void RemoveLogo()
    {
        LogoData = null;
        LogoContentType = null;
    }
}
