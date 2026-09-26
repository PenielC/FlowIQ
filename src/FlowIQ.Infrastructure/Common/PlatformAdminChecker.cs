using FlowIQ.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowIQ.Infrastructure.Common;

public class PlatformAdminChecker(IConfiguration configuration) : IPlatformAdminChecker
{
    public bool IsPlatformAdmin(string email)
    {
        var allowedEmails = configuration.GetSection("Admin:AllowedEmails").Get<string[]>() ?? [];
        return allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
    }
}
