using FlowIQ.Application.Common.Interfaces;

namespace FlowIQ.Infrastructure.Common;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
