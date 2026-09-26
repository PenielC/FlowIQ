namespace FlowIQ.Application.Common.Interfaces;

/// <summary>
/// Platform admin access is a config-driven email allowlist, not a database flag — it's about
/// operating the platform, not a per-company team role (see UserRole for that).
/// </summary>
public interface IPlatformAdminChecker
{
    bool IsPlatformAdmin(string email);
}
