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

    /// <summary>Where the owner came from when they signed up, e.g. "blog:owner-drawings" (null when unknown).</summary>
    public string? SignupSource { get; private set; }

    public void RecordSignupSource(string? source)
    {
        var clean = source?.Trim();
        SignupSource = string.IsNullOrEmpty(clean) ? null : clean.Length > 100 ? clean[..100] : clean;
    }

    /// <summary>
    /// When the owner answered the forecast setup (planned personal withdrawals, or "none"). Null until then, which
    /// is what makes the dashboard keep asking.
    /// </summary>
    public DateTime? ForecastSetupCompletedAtUtc { get; private set; }

    public void CompleteForecastSetup(DateTime completedAtUtc) => ForecastSetupCompletedAtUtc ??= completedAtUtc;

    /// <summary>Whether unpaid invoices are followed up by email. Off until an owner or admin turns it on.</summary>
    public bool ReminderEnabled { get; private set; }

    /// <summary>Days after the due date on which a reminder goes out, e.g. 1, 7 and 14.</summary>
    public List<int> ReminderDays { get; private set; } = [1, 7, 14];

    public void SetReminderSettings(bool enabled, IReadOnlyCollection<int> days)
    {
        var cleaned = (days ?? []).Distinct().Order().ToList();
        if (cleaned.Count is 0 or > 5)
        {
            throw new DomainException("Choose between 1 and 5 reminder days.");
        }

        if (cleaned.Any(d => d is < 1 or > 365))
        {
            throw new DomainException("Reminder days must be between 1 and 365 days after the due date.");
        }

        ReminderEnabled = enabled;
        ReminderDays = cleaned;
    }

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
