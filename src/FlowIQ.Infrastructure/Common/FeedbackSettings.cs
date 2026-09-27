using FlowIQ.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowIQ.Infrastructure.Common;

public class FeedbackSettings(IConfiguration configuration) : IFeedbackSettings
{
    public string RecipientEmail => configuration["Feedback:RecipientEmail"] ?? string.Empty;
}
